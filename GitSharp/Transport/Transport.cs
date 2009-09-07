/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp.Transport
{
    /**
 * Connects two Git repositories together and copies objects between them.
 * <p>
 * A transport can be used for either fetching (copying objects into the
 * caller's repository from the remote repository) or pushing (copying objects
 * into the remote repository from the caller's repository). Each transport
 * implementation is responsible for the details associated with establishing
 * the network connection(s) necessary for the copy, as well as actually
 * shuffling data back and forth.
 * </p>
 * Transport instances and the connections they Create are not thread-safe.
 * Callers must ensure a transport is accessed by only one thread at a time.
 */
    public abstract class Transport
    {
        public enum Operation
        {
            FETCH,
            PUSH
        }

        public static Transport Open(Repository local, string remote)
        {
            RemoteConfig cfg = new RemoteConfig(local.Config, remote);
            List<URIish> uris = cfg.URIs;
            if (uris.Count == 0)
                return Open(local, new URIish(remote));
            return Open(local, cfg);
        }

        public static List<Transport> openAll(Repository local, string remote)
        {
            RemoteConfig cfg = new RemoteConfig(local.Config, remote);
            List<URIish> uris = cfg.URIs;
            if (uris.isEmpty())
            {
                List<Transport> transports = new List<Transport>(1) {Open(local, new URIish(remote))};
                return transports;
            }

            return openAll(local, cfg);
        }

        public static Transport Open(Repository local, RemoteConfig cfg)
        {
            if (cfg.URIs.Count == 0)
                throw new ArgumentException("Remote config \"" + cfg.Name + "\" has no URIs associated");

            Transport tn = Open(local, cfg.URIs[0]);
            tn.ApplyConfig(cfg);
            return tn;
        }

        public static List<Transport> openAll(Repository local, RemoteConfig cfg)
        {
            List<URIish> uris = cfg.URIs;
            List<Transport> tranports = new List<Transport>(uris.Count);
            foreach (URIish uri in uris)
            {
                Transport tn = Open(local, uri);
                tn.ApplyConfig(cfg);
                tranports.Add(tn);
            }
            return tranports;
        }

        private static List<URIish> getURIs(RemoteConfig cfg, Operation op)
        {
            switch (op)
            {
                case Operation.FETCH:
                    return cfg.URIs;

                case Operation.PUSH:
                    List<URIish> uris = cfg.PushURIs;
                    if (uris.Count == 0)
                        uris = cfg.URIs;
                    return uris;

                default:
                    throw new ArgumentException(op.ToString());
            }
        }

        private static bool doesNotExist(RemoteConfig cfg)
        {
            return cfg.URIs.Count == 0 && cfg.PushURIs.Count == 0;
        }

        /**
         * Support for Transport over HTTP and Git (Anon+SSH)
         */
        public static Transport Open(Repository local, URIish remote)
        {
            if (TransportHttp.canHandle(remote))
                return new TransportHttp(local, remote);

            if (TransportGitAnon.canHandle(remote))
                return new TransportGitAnon(local, remote);

            if (TransportGitSsh.canHandle(remote))
                return new TransportGitSsh(local, remote);

            if (TransportSftp.canHandle(remote))
                return new TransportSftp(local, remote);

            throw new NotSupportedException("URI not supported: " + remote);
        }

        private static List<RefSpec> expandPushWildcardsFor(Repository db, IEnumerable<RefSpec> specs)
        {
            Dictionary<string, Ref> localRefs = db.getAllRefs();
            List<RefSpec> procRefs = new List<RefSpec>();

            foreach (RefSpec spec in specs)
            {
                if (spec.Wildcard)
                {
                    foreach (Ref localRef in localRefs.Values)
                    {
                        if (spec.MatchSource(localRef))
                            procRefs.Add(spec.ExpandFromSource(localRef));
                    }
                }
                else
                {
                    procRefs.Add(spec);
                }
            }
            return procRefs;
        }

        private static string findTrackingRefName(string remoteName, IEnumerable<RefSpec> fetchSpecs)
        {
            foreach (RefSpec fetchSpec in fetchSpecs)
            {
                if (fetchSpec.MatchSource(remoteName))
                {
                    if (fetchSpec.Wildcard)
                        return fetchSpec.ExpandFromSource(remoteName).Destination;

                    return fetchSpec.Destination;
                }
            }
            return null;
        }

        public const bool DEFAULT_FETCH_THIN = true;
        public const bool DEFAULT_PUSH_THIN = true;
        public static readonly RefSpec REFSPEC_TAGS = new RefSpec("refs/tags/*:refs/tags/*");
        public static readonly RefSpec REFSPEC_PUSH_ALL = new RefSpec("refs/heads/*:refs/heads/*");

        protected Repository local;
        protected URIish uri;

        private int timeout;

        public Repository Local { get { return local; } }
        public URIish Uri { get { return uri; } }

        private string _optionUploadPack = RemoteConfig.DEFAULT_UPLOAD_PACK;
        public string OptionUploadPack
        {
            get
            {
                return _optionUploadPack;
            }
            set {
                _optionUploadPack = string.IsNullOrEmpty(value) ? RemoteConfig.DEFAULT_UPLOAD_PACK : value;
            }
        }

        private string _optionReceivePack = RemoteConfig.DEFAULT_RECEIVE_PACK;
        public string OptionReceivePack
        {
            get
            {
                return _optionReceivePack;
            }
            set {
                _optionReceivePack = string.IsNullOrEmpty(value) ? RemoteConfig.DEFAULT_RECEIVE_PACK : value;
            }
        }

        private TagOpt _tagopt = TagOpt.NO_TAGS;
        public TagOpt TagOpt
        {
            get
            {
                return _tagopt;
            }
            set
            {
                _tagopt = value ?? TagOpt.AUTO_FOLLOW;
            }
        }

        private bool _fetchThin = DEFAULT_FETCH_THIN;
        public bool FetchThin
        {
            get
            {
                return _fetchThin;
            }
            set
            {
                _fetchThin = value;
            }
        }

        private bool _pushThin = DEFAULT_PUSH_THIN;
        public bool PushThin
        {
            get
            {
                return _pushThin;
            }
            set
            {
                _pushThin = value;
            }
        }

        public bool CheckFetchedObjects
        {
            get;
            set;
        }

        public bool DryRun
        {
            get;
            set;
        }

        public bool RemoveDeletedRefs
        {
            get;
            set;
        }

        private List<RefSpec> fetchSpecs = new List<RefSpec>();
        private List<RefSpec> pushSpecs = new List<RefSpec>();

        protected Transport(Repository local, URIish uri)
        {
            //final TransferConfig tc = local.getConfig().getTransfer();
            this.local = local;
            this.uri = uri;
            //this.checkFetchedObjects = tc.FsckObjects;
        }

        public void ApplyConfig(RemoteConfig cfg)
        {
            OptionUploadPack = cfg.UploadPack;
            fetchSpecs = cfg.Fetch;
            TagOpt = cfg.TagOpt;
            OptionReceivePack = cfg.ReceivePack;
            pushSpecs = cfg.Push;
        }

        public abstract IFetchConnection openFetch();
        public abstract IPushConnection openPush();
        public abstract void close();

        public FetchResult fetch(IProgressMonitor monitor, List<RefSpec> toFetch)
        {
            if (toFetch == null || toFetch.Count == 0)
            {
                if (fetchSpecs.Count == 0)
                    throw new TransportException("Nothing to fetch.");
                toFetch = fetchSpecs;
            }
            else if (fetchSpecs.Count != 0)
            {
                List<RefSpec> tmp = new List<RefSpec>(toFetch);
                foreach (RefSpec requested in toFetch)
                {
                    string reqSrc = requested.Source;
                    foreach (RefSpec configured in fetchSpecs)
                    {
                        string cfgSrc = configured.Source;
                        string cfgDst = configured.Destination;
                        if (cfgSrc.Equals(reqSrc) && cfgDst != null)
                        {
                            tmp.Add(configured);
                            break;
                        }
                    }
                }
                toFetch = tmp;
            }

            FetchResult result = new FetchResult();
            new FetchProcess(this, toFetch).execute(monitor, result);
            return result;
        }

        public PushResult push(IProgressMonitor monitor, List<RemoteRefUpdate> toPush)
        {
            if (toPush == null || toPush.Count == 0)
            {
                try
                {
                    toPush = findRemoteRefUpdatesFor(pushSpecs);
                }
                catch (IOException e)
                {
                    throw new TransportException("Problem with resolving push ref specs locally: " + e.Message, e);
                }

                if (toPush.Count == 0)
                    throw new TransportException("Nothing to push");
            }
            PushProcess pushProcess = new PushProcess(this, toPush);
            return pushProcess.execute(monitor);
        }

        public List<RemoteRefUpdate> findRemoteRefUpdatesFor(List<RefSpec> specs)
        {
            return findRemoteRefUpdatesFor(local, specs, fetchSpecs);
        }

        public static List<RemoteRefUpdate> findRemoteRefUpdatesFor(Repository db, List<RefSpec> specs, List<RefSpec> fetchSpecs)
        {
            if (fetchSpecs == null)
                fetchSpecs = new List<RefSpec>();
            List<RemoteRefUpdate> result = new List<RemoteRefUpdate>();
            List<RefSpec> procRefs = expandPushWildcardsFor(db, specs);

            foreach (RefSpec spec in procRefs)
            {
                string srcSpec = spec.Source;
                Ref srcRef = db.getRef(srcSpec);
                if (srcRef != null)
                    srcSpec = srcRef.Name;

                string destSpec = spec.Destination ?? srcSpec;

                if (srcRef != null && !destSpec.StartsWith(Constants.R_REFS))
                {
                    string n = srcRef.Name;
                    int kindEnd = n.IndexOf('/', Constants.R_REFS.Length);
                    destSpec = n.Slice(0, kindEnd + 1) + destSpec;
                }

                bool forceUpdate = spec.Force;
                string localName = findTrackingRefName(destSpec, fetchSpecs);
                RemoteRefUpdate rru = new RemoteRefUpdate(db, srcSpec, destSpec, forceUpdate, localName, null);
                result.Add(rru);
            }
            return result;
        }

        public int getTimeout()
        {
            return timeout;
        }

        public void setTimeout(int seconds)
        {
            timeout = seconds;
        }

    }
}