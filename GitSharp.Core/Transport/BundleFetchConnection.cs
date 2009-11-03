/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Google Inc.
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    public class BundleFetchConnection : BaseFetchConnection
    {
        public const string V2_BUNDLE_SIGNATURE = "# v2 git bundle";

        private readonly Transport transport;
        private Stream bin;
        private readonly List<ObjectId> prereqs = new List<ObjectId>();
        private string lockMessage;
        private PackLock packLock;

        public BundleFetchConnection(Transport transportBundle, Stream src)
        {
            transport = transportBundle;
            bin = new BufferedStream(src, IndexPack.BUFFER_SIZE);
            try
            {
                switch (readSignature())
                {
                    case 2:
                        readBundleV2();
                        break;

                    default:
                        throw new TransportException(transport.Uri, "not a bundle");
                }
            }
            catch (TransportException)
            {
                Close();
                throw;
            }
            catch (IOException err)
            {
                Close();
                throw new TransportException(transport.Uri, err.Message, err);
            }
        }

        private int readSignature()
        {
            string rev = readLine(new byte[1024]);
            if (V2_BUNDLE_SIGNATURE.Equals(rev))
                return 2;
            throw new TransportException(transport.Uri, "not a bundle");
        }

        private void readBundleV2()
        {
            byte[] hdrbuf = new byte[1024];
            Dictionary<string, Ref> avail = new Dictionary<string, Ref>();
            for (;;)
            {
                string line = readLine(hdrbuf);
                if (line.Length == 0)
                    break;

                if (line[0] == '-')
                {
                    prereqs.Add(ObjectId.FromString(line.Slice(1, 41)));
                    continue;
                }

                string name = line.Slice(41, line.Length);
                ObjectId id = ObjectId.FromString(line.Slice(0, 40));
                Ref prior = new Ref(Ref.Storage.Network, name, id);
                if (avail.ContainsKey(name))
                {
                    throw duplicateAdvertisement(name);
                }
                avail.Add(name, prior);
            }
            available(avail);
        }

        private PackProtocolException duplicateAdvertisement(string name)
        {
            return new PackProtocolException(transport.Uri, "duplicate advertisement of " + name);
        }

        private string readLine(byte[] hdrbuf)
        {
            long mark = bin.Position;
            int cnt = bin.Read(hdrbuf, 0, hdrbuf.Length);
            int lf = 0;
            while (lf < cnt && hdrbuf[lf] != '\n')
                lf++;
            bin.Position = mark;
            IO.skipFully(bin, lf);
            if (lf < cnt && hdrbuf[lf] == '\n')
                IO.skipFully(bin, 1);

            return RawParseUtils.decode(Constants.CHARSET, hdrbuf, 0, lf);
        }

        public override bool DidFetchTestConnectivity
        {
            get { return false; }
        }

        protected override void doFetch(ProgressMonitor monitor, List<Ref> want, List<ObjectId> have)
        {
            verifyPrerequisites();
            try
            {
                IndexPack ip = newIndexPack();
                ip.index(monitor);
                packLock = ip.renameAndOpenPack(lockMessage);
            }
            catch (IOException err)
            {
                Close();
                throw new TransportException(transport.Uri, err.Message, err);
            }
        }

        public override void SetPackLockMessage(string message)
        {
            lockMessage = message;
        }

        public override List<PackLock> PackLocks
        {
            get { return new List<PackLock> { packLock }; }
        }

        private IndexPack newIndexPack()
        {
            IndexPack ip = IndexPack.Create(transport.Local, bin);
            ip.setFixThin(true);
            ip.setObjectChecking(transport.CheckFetchedObjects);
            return ip;
        }

        private void verifyPrerequisites()
        {
            if (prereqs.isEmpty())
                return;

            using(RevWalk.RevWalk rw = new RevWalk.RevWalk(transport.Local))
            {
	            RevFlag PREREQ = rw.newFlag("PREREQ");
	            RevFlag SEEN = rw.newFlag("SEEN");
	
	            List<ObjectId> missing = new List<ObjectId>();
	            List<RevObject> commits = new List<RevObject>();
	            foreach (ObjectId p in prereqs)
	            {
	                try
	                {
	                    RevCommit c = rw.parseCommit(p);
	                    if (!c.has(PREREQ))
	                    {
	                        c.add(PREREQ);
	                        commits.Add(c);
	                    }
	                }
	                catch (MissingObjectException)
	                {
	                    missing.Add(p);
	                }
	                catch (IOException err)
	                {
	                    throw new TransportException(transport.Uri, "Cannot Read commit " + p.Name, err);
	                }
	            }
	
	            if (!missing.isEmpty())
	                throw new MissingBundlePrerequisiteException(transport.Uri, missing);
	
	            foreach (Ref r in transport.Local.getAllRefs().Values)
	            {
	                try
	                {
	                    rw.markStart(rw.parseCommit(r.ObjectId));
	                }
	                catch (IOException)
	                {
	                }
	            }
	
	            int remaining = commits.Count;
	            try
	            {
	                RevCommit c;
	                while ((c = rw.next()) != null)
	                {
	                    if (c.has(PREREQ))
	                    {
	                        c.add(SEEN);
	                        if (--remaining == 0)
	                            break;
	                    }
	                }
	            }
	            catch (IOException err)
	            {
	                throw new TransportException(transport.Uri, "Cannot Read object", err);
	            }
	
	            if (remaining > 0)
	            {
	                foreach (RevObject o in commits)
	                {
	                    if (!o.has(SEEN))
	                        missing.Add(o);
	                }
	                throw new MissingBundlePrerequisiteException(transport.Uri, missing);
	            }
            }
        }

        public override void Close()
        {
            if (bin != null)
            {
                try
                {
                    bin.Close();
                }
                catch (IOException)
                {

                }
                finally
                {
                    bin = null;
                }
            }
        }
    }

}