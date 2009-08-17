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
using System.Collections.ObjectModel;
using System.IO;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp.Transport
{
    /// <summary>
    /// Connects two Git repositories together and copies objects between them.
    ///
    /// A transport can be used for either fetching (copying objects into the
    /// caller's repository from the remote repository) or pushing (copying objects
    /// into the remote repository from the caller's repository). Each transport
    /// implementation is responsible for the details associated with establishing
    /// the network connection(s) necessary for the copy, as well as actually
    /// shuffling data back and forth.
    ///
    /// Transport instances and the connections they create are not thread-safe.
    /// Callers must ensure a transport is accessed by only one thread at a time.
    /// </summary>
    public abstract class Transport
    {
        /// <summary>
        ///  Type of operation a Transport is being opened for.  
        /// </summary>
        public enum Operation
        {
            /// <summary> Transport is to fetch objects locally. </summary>
            FETCH,
            /// <summary> Transport is to push objects remotely. </summary>
            PUSH
        }

        ///	<summary>
        /// Open a new transport instance to connect two repositories.
        ///	
        ///	This method assumes <seealso cref="Operation.FETCH"/>.
        ///	</summary>
        ///	<param name="local">existing local repository.</param>
        ///	<param name="remote">
        /// location of the remote repository - may be URI or remote
        /// configuration name.
        /// </param>
        ///	<returns>
        /// the new transport instance. Never null. In case of multiple URIs
        /// in remote configuration, only the first is chosen.
        /// </returns>
        ///	<exception cref="URISyntaxException">
        /// the location is not a remote defined in the configuration
        /// file and is not a well-formed URL.
        /// </exception>
        ///	<exception cref="NotSupportedException">
        /// the protocol specified is not supported.
        /// </exception>
        public static Transport open(Repository local, string remote)
        {
            return open(local, remote, Operation.FETCH);
        }

        ///    
        ///	 <summary> * Open a new transport instance to connect two repositories.
        ///	 * </summary>
        ///	 * <param name="local">
        ///	 *            existing local repository. </param>
        ///	 * <param name="remote">
        ///	 *            location of the remote repository - may be URI or remote
        ///	 *            configuration name. </param>
        ///	 * <param name="op">
        ///	 *            planned use of the returned Transport; the URI may differ
        ///	 *            based on the type of connection desired. </param>
        ///	 * <returns> the new transport instance. Never null. In case of multiple URIs
        ///	 *         in remote configuration, only the first is chosen. </returns>
        ///	 * <exception cref="URISyntaxException">
        ///	 *             the location is not a remote defined in the configuration
        ///	 *             file and is not a well-formed URL. </exception>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             the protocol specified is not supported. </exception>
        public static Transport open(Repository local, string remote, Operation op)
        {
            RemoteConfig cfg = new RemoteConfig(local.Config, remote);
            if (doesNotExist(cfg))
                return open(local, new URIish(remote));
            return open(local, cfg, op);
        }

        ///    
        ///	 <summary> * Open new transport instances to connect two repositories.
        ///	 * <p>
        ///	 * This method assumes <seealso cref="Operation#FETCH"/>.
        ///	 * </summary>
        ///	 * <param name="local">
        ///	 *            existing local repository. </param>
        ///	 * <param name="remote">
        ///	 *            location of the remote repository - may be URI or remote
        ///	 *            configuration name. </param>
        ///	 * <returns> the list of new transport instances for every URI in remote
        ///	 *         configuration. </returns>
        ///	 * <exception cref="URISyntaxException">
        ///	 *             the location is not a remote defined in the configuration
        ///	 *             file and is not a well-formed URL. </exception>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             the protocol specified is not supported. </exception>
        ///	 
        public static IList<Transport> openAll(Repository local, string remote)
        {
            return openAll(local, remote, Operation.FETCH);
        }

        ///    
        ///	 <summary> * Open new transport instances to connect two repositories.
        ///	 * </summary>
        ///	 * <param name="local">
        ///	 *            existing local repository. </param>
        ///	 * <param name="remote">
        ///	 *            location of the remote repository - may be URI or remote
        ///	 *            configuration name. </param>
        ///	 * <param name="op">
        ///	 *            planned use of the returned Transport; the URI may differ
        ///	 *            based on the type of connection desired. </param>
        ///	 * <returns> the list of new transport instances for every URI in remote
        ///	 *         configuration. </returns>
        ///	 * <exception cref="URISyntaxException">
        ///	 *             the location is not a remote defined in the configuration
        ///	 *             file and is not a well-formed URL. </exception>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             the protocol specified is not supported. </exception>
        ///	 
        public static IList<Transport> openAll(Repository local, string remote, Operation op)
        {
            RemoteConfig cfg = new RemoteConfig(local.Config, remote);
            if (doesNotExist(cfg))
            {
                List<Transport> transports = new List<Transport>(1);
                transports.Add(open(local, new URIish(remote)));
                return transports;
            }
            return openAll(local, cfg, op);
        }

        ///    
        ///	 <summary> * Open a new transport instance to connect two repositories.
        ///	 * <p>
        ///	 * This method assumes <seealso cref="Operation#FETCH"/>.
        ///	 *  </summary>
        ///	 * <param name="local">
        ///	 *            existing local repository. </param>
        ///	 * <param name="cfg">
        ///	 *            configuration describing how to connect to the remote
        ///	 *            repository. </param>
        ///	 * <returns> the new transport instance. Never null. In case of multiple URIs
        ///	 *         in remote configuration, only the first is chosen. </returns>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             the protocol specified is not supported. </exception>
        ///	 * <exception cref="IllegalArgumentException">
        ///	 *             if provided remote configuration doesn't have any URI
        ///	 *             associated. </exception>
        public static Transport open(Repository local, RemoteConfig cfg)
        {
            return open(local, cfg, Operation.FETCH);
        }

        ///    
        ///	 <summary> * Open a new transport instance to connect two repositories.
        ///	 * </summary>
        ///	 * <param name="local">
        ///	 *            existing local repository. </param>
        ///	 * <param name="cfg">
        ///	 *            configuration describing how to connect to the remote
        ///	 *            repository. </param>
        ///	 * <param name="op">
        ///	 *            planned use of the returned Transport; the URI may differ
        ///	 *            based on the type of connection desired. </param>
        ///	 * <returns> the new transport instance. Never null. In case of multiple URIs
        ///	 *         in remote configuration, only the first is chosen. </returns>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             the protocol specified is not supported. </exception>
        ///	 * <exception cref="IllegalArgumentException">
        ///	 *             if provided remote configuration doesn't have any URI
        ///	 *             associated. </exception>
        public static Transport open(Repository local, RemoteConfig cfg, Operation op)
        {
            IList<URIish> uris = getURIs(cfg, op);
            if (uris.isEmpty())
                throw new System.ArgumentException("Remote config \"" + cfg.Name + "\" has no URIs associated");

            Transport tn = open(local, uris[0]);
            tn.applyConfig(cfg);
            return tn;
        }

        ///	 <summary> * Open new transport instances to connect two repositories.
        ///	 * <p>
        ///	 * This method assumes <seealso cref="Operation#FETCH"/>.
        ///	 * </summary>
        ///	 * <param name="local">
        ///	 *            existing local repository. </param>
        ///	 * <param name="cfg">
        ///	 *            configuration describing how to connect to the remote
        ///	 *            repository. </param>
        ///	 * <returns> the list of new transport instances for every URI in remote
        ///	 *         configuration. </returns>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             the protocol specified is not supported. </exception>
        public static IList<Transport> openAll(Repository local, RemoteConfig cfg)
        {
            return openAll(local, cfg, Operation.FETCH);
        }

        ///	 <summary> * Open new transport instances to connect two repositories.
        ///	 * </summary>
        ///	 * <param name="local">
        ///	 *            existing local repository. </param>
        ///	 * <param name="cfg">
        ///	 *            configuration describing how to connect to the remote
        ///	 *            repository. </param>
        ///	 * <param name="op">
        ///	 *            planned use of the returned Transport; the URI may differ
        ///	 *            based on the type of connection desired. </param>
        ///	 * <returns> the list of new transport instances for every URI in remote
        ///	 *         configuration. </returns>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             the protocol specified is not supported. </exception>
        public static IList<Transport> openAll(Repository local, RemoteConfig cfg, Operation op)
        {
            IList<URIish> uris = getURIs(cfg, op);
            IList<Transport> transports = new List<Transport>(uris.Count);
            foreach (URIish uri in uris)
            {
                Transport tn = open(local, uri);
                tn.applyConfig(cfg);
                transports.Add(tn);
            }
            return transports;
        }

        private static IList<URIish> getURIs(RemoteConfig cfg, Operation op)
        {
            switch (op)
            {
                case Operation.FETCH:
                    return cfg.URIs;

                case Operation.PUSH:
                    {
                        IList<URIish> uris = cfg.PushURIs;
                        if (uris.isEmpty())
                            uris = cfg.URIs;
                        return uris;
                    }

                default:
                    throw new ArgumentException(op.ToString());
            }
        }

        private static bool doesNotExist(RemoteConfig cfg)
        {
            return cfg.URIs.isEmpty() && cfg.PushURIs.isEmpty();
        }

        ///	 <summary> * Open a new transport instance to connect two repositories.
        ///	 *  </summary>
        ///	 * <param name="local">
        ///	 *            existing local repository. </param>
        ///	 * <param name="remote">
        ///	 *            location of the remote repository. </param>
        ///	 * <returns> the new transport instance. Never null. </returns>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             the protocol specified is not supported. </exception>
        ///	 
        public static Transport open(Repository local, URIish remote)
        {
            if (TransportGitSsh.canHandle(remote))
                return new TransportGitSsh(local, remote);

            else if (TransportHttp.canHandle(remote))
                return new TransportHttp(local, remote);

            else if (TransportSftp.canHandle(remote))
                return new TransportSftp(local, remote);

            else if (TransportGitAnon.canHandle(remote))
                return new TransportGitAnon(local, remote);

            else if (TransportAmazonS3.canHandle(remote))
                return new TransportAmazonS3(local, remote);

            else if (TransportBundleFile.CanHandle(remote))
                return new TransportBundleFile(local, remote);

            else if (TransportLocal.canHandle(remote))
                return new TransportLocal(local, remote);

            throw new NotSupportedException("URI not supported: " + remote);
        }

        ///    
        ///	 <summary> * Convert push remote refs update specification from <seealso cref="RefSpec"/> form
        ///	 * to <seealso cref="RemoteRefUpdate"/>. Conversion expands wildcards by matching
        ///	 * source part to local refs. expectedOldObjectId in RemoteRefUpdate is
        ///	 * always set as null. Tracking branch is configured if RefSpec destination
        ///	 * matches source of any fetch ref spec for this transport remote
        ///	 * configuration.
        ///	 * </summary>
        ///	 * <param name="db">
        ///	 *            local database. </param>
        ///	 * <param name="specs">
        ///	 *            collection of RefSpec to convert. </param>
        ///	 * <param name="fetchSpecs">
        ///	 *            fetch specifications used for finding localtracking refs. May
        ///	 *            be null or empty collection. </param>
        ///	 * <returns> collection of set up <seealso cref="RemoteRefUpdate"/>. </returns>
        ///	 * <exception cref="IOException">
        ///	 *             when problem occurred during conversion or specification set
        ///	 *             up: most probably, missing objects or refs. </exception>
        ///	 
        public static ICollection<RemoteRefUpdate> findRemoteRefUpdatesFor(Repository db, ICollection<RefSpec> specs, ICollection<RefSpec> fetchSpecs)
        {
            if (fetchSpecs == null)
            {
                fetchSpecs = new Collection<RefSpec>();
            }

            List<RemoteRefUpdate> result = new List<RemoteRefUpdate>();
            ICollection<RefSpec> procRefs = expandPushWildcardsFor(db, specs);

            foreach (RefSpec spec in procRefs)
            {
                string srcSpec = spec.Source;
                Ref srcRef = db.Refs[srcSpec];
                if (srcRef != null)
                    srcSpec = srcRef.Name;

                string destSpec = spec.Destination;
                if (destSpec == null)
                {
                    // No destination (no-colon in ref-spec), DWIMery assumes src
                    //
                    destSpec = srcSpec;
                }

                if (srcRef != null && !destSpec.StartsWith(Constants.R_REFS))
                {
                    // Assume the same kind of ref at the destination, e.g.
                    // "refs/heads/foo:master", DWIMery assumes master is also
                    // under "refs/heads/".
                    //
                    string n = srcRef.Name;
                    int kindEnd = n.IndexOf('/', Constants.R_REFS.Length);
                    destSpec = n.Substring(0, kindEnd + 1) + destSpec;
                }

                bool forceUpdate = spec.Force;
                string localName = findTrackingRefName(destSpec, fetchSpecs);
                RemoteRefUpdate rru = new RemoteRefUpdate(db, srcSpec, destSpec, forceUpdate, localName, null);
                result.Add(rru);
            }

            return result;
        }

        private static ICollection<RefSpec> expandPushWildcardsFor(Repository db, IEnumerable<RefSpec> specs)
        {
            IDictionary<string, Ref> localRefs = db.Refs;
            ICollection<RefSpec> procRefs = new HashSet<RefSpec>();

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
            // try to find matching tracking refs
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

        ///	<summary>
        /// Default setting for <seealso cref="FetchThin"/> option. 
        /// </summary>
        public const bool DEFAULT_FETCH_THIN = true;

        ///	 <summary> * Default setting for <seealso cref="PushThin"/> option. </summary>
        public const bool DEFAULT_PUSH_THIN = false;

        ///	 <summary> * Specification for fetch or push operations, to fetch or push all tags.
        ///	 * Acts as --tags. </summary>
        public static readonly RefSpec REFSPEC_TAGS = new RefSpec("refs/tags/*:refs/tags/*");

        ///    
        ///	 <summary> * Specification for push operation, to push all refs under refs/heads. Acts
        ///	 * as --all. </summary>
        ///	 
        public static readonly RefSpec REFSPEC_PUSH_ALL = new RefSpec("refs/heads/*:refs/heads/*");

        /// <summary> The repository this transport fetches into, or pushes out of.  </summary>
        protected internal readonly Repository _local;

        /// <summary> The URI used to create this transport.  </summary>
        private readonly URIish _uri;

        /// <summary> Name of the upload pack program, if it must be executed.  </summary>
        private string optionUploadPack = RemoteConfig.DEFAULT_UPLOAD_PACK;

        /// <summary> Specifications to apply during fetch.  </summary>
        private IList<RefSpec> _fetch = new List<RefSpec>();

        ///    
        ///	 <summary> * How <seealso cref="#fetch(IProgressMonitor, Collection)"/> should handle tags.
        ///	 * <p>
        ///	 * We default to <seealso cref="TagOpt#NO_TAGS"/> so as to avoid fetching annotated
        ///	 * tags during one-shot fetches used for later merges. This prevents
        ///	 * dragging down tags from repositories that we do not have established
        ///	 * tracking branches for. If we do not track the source repository, we most
        ///	 * likely do not care about any tags it publishes. </summary>
        ///	 
        private TagOpt _tagopt = TagOpt.NO_TAGS;

        /// <summary> Should fetch request thin-pack if remote repository can produce it.  </summary>
        private bool _fetchThin = DEFAULT_FETCH_THIN;

        /// <summary> Name of the receive pack program, if it must be executed.  </summary>
        private string optionReceivePack = RemoteConfig.DEFAULT_RECEIVE_PACK;

        /// <summary> Specifications to apply during push.  </summary>
        private IList<RefSpec> _push = new List<RefSpec>();

        /// <summary> Should push produce thin-pack when sending objects to remote repository.  </summary>
        private bool _pushThin = DEFAULT_PUSH_THIN;

        /// <summary> Should push just check for operation result, not really push.  </summary>
        private bool _dryRun;

        /// <summary> Should an incoming (fetch) transfer validate objects?  </summary>
        private bool _checkFetchedObjects;

        /// <summary> Timeout in seconds to wait before aborting an IO read or write.  </summary>
        private int timeout;

        ///	 <summary> * Create a new transport instance.
        ///	 *  </summary>
        ///	 * <param name="local">
        ///	 *            the repository this instance will fetch into, or push out of.
        ///	 *            This must be the repository passed to
        ///	 *            <seealso cref="#open(Repository, URIish)"/>. </param>
        ///	 * <param name="uri">
        ///	 *            the URI used to access the remote repository. This must be the
        ///	 *            URI passed to <seealso cref="#open(Repository, URIish)"/>. </param>
        protected internal Transport(Repository local, URIish uri)
        {
            //TransferConfig tc = local.Config.getTransfer();
            _local = local;
            _uri = uri;
            //_checkFetchedObjects = tc.isFsckObjects();
        }

        ///	<summary>
        /// Get the URI this transport connects to.
        /// 
        /// Each transport instance connects to at most one URI at any point in time.
        /// </summary>
        ///	<returns> The URI describing the location of the remote repository. </returns>
        public virtual URIish Uri
        {
            get { return _uri; }
        }

        /// <summary>
        /// Gets/Sets the name of the remote executable providing upload-pack service.
        /// The default value is: <seealso cref="RemoteConfig.DEFAULT_RECEIVE_PACK" />
        /// </summary>
        /// <returns> typically "git-upload-pack". </returns>
        public string OptionUploadPack
        {
            get { return optionUploadPack; }
            set { optionUploadPack = !string.IsNullOrEmpty(value) ? value : RemoteConfig.DEFAULT_UPLOAD_PACK; }
        }

        /// <summary>
        /// Gets/Sets description of how annotated tags should be treated on fetch.
        /// </summary>
        public TagOpt TagOpt
        {
            get { return _tagopt; }
            set { _tagopt = value ?? TagOpt.AUTO_FOLLOW; }
        }

        ///	<summary>
        /// Gets/Sets the thin-pack preference for fetch operation.
        /// </summary>
        public bool FetchThin
        {
            get { return _fetchThin; }
            set { _fetchThin = value; }
        }

        ///	<summary>
        /// Gets/Sets vaidation checking of received objects.
        /// </summary>
        public bool CheckFetchedObjects
        {
            get { return _checkFetchedObjects; }
            set { _checkFetchedObjects = value; }
        }

        /// <summary>
        /// Gets/Sets remote executable providing receive-pack service for pack transports.
        /// The default value is: <seealso cref="RemoteConfig.DEFAULT_RECEIVE_PACK" />
        /// </summary>
        public string OptionReceivePack
        {
            get { return optionReceivePack; }
            set { optionReceivePack = !string.IsNullOrEmpty(value) ? value : RemoteConfig.DEFAULT_RECEIVE_PACK; }
        }

        ///	<summary>
        /// Gets/Sets the thin-pack preference for push operation.
        /// </summary>
        public bool PushThin
        {
            get { return _pushThin; }
            set { _pushThin = value; }
        }

        ///	<summary>
        /// Gets/Sets whether or not to remove refs which no longer exist in the source.
        /// 
        ///	If true, refs at the destination repository (local for fetch, remote for
        ///	push) are deleted if they no longer exist on the source side (remote for
        ///	fetch, local for push).
        ///
        /// False by default, as this may cause data to become unreachable, and
        ///	eventually be deleted on the next GC.
        /// </summary>
        public bool RemoveDeletedRefs
        {
            get;
            set;
        }

        public Repository Local
        {
            get { return _local; }
        }

        ///    
        ///	 <summary> * Apply provided remote configuration on this transport.
        ///	 * </summary>
        ///	 * <param name="cfg">
        ///	 *            configuration to apply on this transport. </param>
        ///	 
        public virtual void applyConfig(RemoteConfig cfg)
        {
            OptionUploadPack = cfg.UploadPack;
            OptionReceivePack = cfg.ReceivePack;
            TagOpt = cfg.TagOpt;
            _fetch = cfg.Fetch;
            _push = cfg.Push;
            timeout = cfg.Timeout;
        }

        ///    
        ///	 * <returns> true if push operation should just check for possible result and
        ///	 *         not really update remote refs, false otherwise - when push should
        ///	 *         act normally. </returns>
        ///	 
        public virtual bool isDryRun()
        {
            return _dryRun;
        }

        ///    
        ///	 <summary> * Set dry run option for push operation.
        ///	 * </summary>
        ///	 * <param name="dryRun">
        ///	 *            true if push operation should just check for possible result
        ///	 *            and not really update remote refs, false otherwise - when push
        ///	 *            should act normally. </param>
        ///	 
        public virtual void setDryRun(bool dryRun)
        {
            this._dryRun = dryRun;
        }

        /// <returns> timeout (in seconds) before aborting an IO operation.  </returns>
        public virtual int getTimeout()
        {
            return timeout;
        }

        ///    
        ///	 <summary> * Set the timeout before willing to abort an IO call.
        ///	 * </summary>
        ///	 * <param name="seconds">
        ///	 *            number of seconds to wait (with no data transfer occurring)
        ///	 *            before aborting an IO read or write operation with this
        ///	 *            remote. </param>
        ///	 
        public virtual void setTimeout(int seconds)
        {
            timeout = seconds;
        }

        ///    
        ///	 <summary> * Fetch objects and refs from the remote repository to the local one.
        ///	 * <p>
        ///	 * This is a utility function providing standard fetch behavior. Local
        ///	 * tracking refs associated with the remote repository are automatically
        ///	 * updated if this transport was created from a <seealso cref="RemoteConfig"/> with
        ///	 * fetch RefSpecs defined.
        ///	 *  </summary>
        ///	 * <param name="monitor">
        ///	 *            progress monitor to inform the user about our processing
        ///	 *            activity. Must not be null. Use <seealso cref="NullProgressMonitor"/> if
        ///	 *            progress updates are not interesting or necessary. </param>
        ///	 * <param name="toFetch">
        ///	 *            specification of refs to fetch locally. May be null or the
        ///	 *            empty collection to use the specifications from the
        ///	 *            RemoteConfig. Source for each RefSpec can't be null. </param>
        ///	 * <returns> information describing the tracking refs updated. </returns>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             this transport implementation does not support fetching
        ///	 *             objects. </exception>
        ///	 * <exception cref="TransportException">
        ///	 *             the remote connection could not be established or object
        ///	 *             copying (if necessary) failed or update specification was
        ///	 *             incorrect. </exception>
        ///	 
        public virtual FetchResult fetch(IProgressMonitor monitor, ICollection<RefSpec> toFetch)
        {
            if (toFetch == null || toFetch.Count == 0)
            {
                // If the caller did not ask for anything use the defaults.
                //
                if (_fetch.Count == 0)
                    throw new TransportException("Nothing to fetch.");
                toFetch = _fetch;
            }
            else if (_fetch.Count != 0)
            {
                // If the caller asked for something specific without giving
                // us the local tracking branch see if we can update any of
                // the local tracking branches without incurring additional
                // object transfer overheads.
                //
                ICollection<RefSpec> tmp = new List<RefSpec>(toFetch);
                foreach (RefSpec requested in toFetch)
                {
                    string reqSrc = requested.Source;
                    foreach (RefSpec configured in _fetch)
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

        ///    
        ///	 <summary> * Push objects and refs from the local repository to the remote one.
        ///	 * <p>
        ///	 * This is a utility function providing standard push behavior. It updates
        ///	 * remote refs and send there necessary objects according to remote ref
        ///	 * update specification. After successful remote ref update, associated
        ///	 * locally stored tracking branch is updated if set up accordingly. Detailed
        ///	 * operation result is provided after execution.
        ///	 * <p>
        ///	 * For setting up remote ref update specification from ref spec, see helper
        ///	 * method <seealso cref="#findRemoteRefUpdatesFor(Collection)"/>, predefined refspecs
        ///	 * (<seealso cref="#REFSPEC_TAGS"/>, <seealso cref="#REFSPEC_PUSH_ALL"/>) or consider using
        ///	 * directly <seealso cref="RemoteRefUpdate"/> for more possibilities.
        ///	 * <p>
        ///	 * When <seealso cref="#isDryRun()"/> is true, result of this operation is just
        ///	 * estimation of real operation result, no real action is performed.
        ///	 * </summary>
        ///	 * <seealso cref= RemoteRefUpdate
        ///	 * </seealso>
        ///	 * <param name="monitor">
        ///	 *            progress monitor to inform the user about our processing
        ///	 *            activity. Must not be null. Use <seealso cref="NullProgressMonitor"/> if
        ///	 *            progress updates are not interesting or necessary. </param>
        ///	 * <param name="toPush">
        ///	 *            specification of refs to push. May be null or the empty
        ///	 *            collection to use the specifications from the RemoteConfig
        ///	 *            converted by <seealso cref="#findRemoteRefUpdatesFor(Collection)"/>. No
        ///	 *            more than 1 RemoteRefUpdate with the same remoteName is
        ///	 *            allowed. These objects are modified during this call. </param>
        ///	 * <returns> information about results of remote refs updates, tracking refs
        ///	 *         updates and refs advertised by remote repository. </returns>
        ///	 * <exception cref="NotSupportedException">
        ///	 *             this transport implementation does not support pushing
        ///	 *             objects. </exception>
        ///	 * <exception cref="TransportException">
        ///	 *             the remote connection could not be established or object
        ///	 *             copying (if necessary) failed at I/O or protocol level or
        ///	 *             update specification was incorrect. </exception>
        ///	 
        public virtual PushResult push(IProgressMonitor monitor, ICollection<RemoteRefUpdate> toPush)
        {
            if (toPush == null || toPush.Count == 0)
            {
                // If the caller did not ask for anything use the defaults.
                try
                {
                    toPush = findRemoteRefUpdatesFor(_push);
                }
                catch (IOException e)
                {
                    throw new TransportException("Problem with resolving push ref specs locally: " + e.Message, e);
                }
                if (toPush.Count == 0)
                {
                    throw new TransportException("Nothing to push.");
                }
            }

            PushProcess pushProcess = new PushProcess(this, toPush);
            return pushProcess.execute(monitor);
        }

        ///    
        ///	 <summary> * Convert push remote refs update specification from <seealso cref="RefSpec"/> form
        ///	 * to <seealso cref="RemoteRefUpdate"/>. Conversion expands wildcards by matching
        ///	 * source part to local refs. expectedOldObjectId in RemoteRefUpdate is
        ///	 * always set as null. Tracking branch is configured if RefSpec destination
        ///	 * matches source of any fetch ref spec for this transport remote
        ///	 * configuration.
        ///	 * <p>
        ///	 * Conversion is performed for context of this transport (database, fetch
        ///	 * specifications).
        ///	 * </summary>
        ///	 * <param name="specs">
        ///	 *            collection of RefSpec to convert. </param>
        ///	 * <returns> collection of set up <seealso cref="RemoteRefUpdate"/>. </returns>
        ///	 * <exception cref="IOException">
        ///	 *             when problem occurred during conversion or specification set
        ///	 *             up: most probably, missing objects or refs. </exception>
        ///	 
        public virtual ICollection<RemoteRefUpdate> findRemoteRefUpdatesFor(ICollection<RefSpec> specs)
        {
            return findRemoteRefUpdatesFor(_local, specs, _fetch);
        }

        ///	<summary>
        /// Begins a new connection for fetching from the remote repository.
        ///	</summary>
        ///	<returns>a fresh connection to fetch from the remote repository.
        /// </returns>
        ///	<exception cref="NotSupportedException">
        ///	the implementation does not support fetching.
        /// </exception>
        ///	<exception cref="TransportException">
        ///	the remote connection could not be established.
        /// </exception>
        public abstract IFetchConnection openFetch();

        ///	<summary>
        /// Begins a new connection for pushing into the remote repository.
        /// </summary>
        ///	<returns>
        /// a fresh connection to push into the remote repository.
        /// </returns>
        ///	<exception cref="NotSupportedException">
        ///	the implementation does not support pushing.
        /// </exception>
        ///	<exception cref="TransportException">
        ///	the remote connection could not be established
        /// </exception>

        public abstract IPushConnection openPush();

        ///	<summary> 
        /// Close any resources used by this transport.
        ///	If the remote repository is contacted by a network socket this method
        ///	must close that network socket, disconnecting the two peers. If the
        /// remote repository is actually local (same system) this method must close
        /// any open file handles used to read the "remote" repository.
        /// </summary>
        public abstract void close();
    }
}