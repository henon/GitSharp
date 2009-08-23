/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp.Transport
{

    public class WalkPushConnection : BaseConnection, IPushConnection
    {
        private readonly Repository local;
        private readonly URIish uri;
        private readonly WalkRemoteObjectDatabase dest;

        private Dictionary<string, string> packNames;
        private Dictionary<string, Ref> newRefs;
        private List<RemoteRefUpdate> packedRefUpdates;

        public WalkPushConnection(IWalkTransport walkTransport, WalkRemoteObjectDatabase w)
        {
            Transport t = (Transport) walkTransport;
            local = t.Local;
            uri = t.Uri;
            dest = w;
        }

        private class PushRefWriter : RefWriter
        {
            private readonly WalkRemoteObjectDatabase dest;

            public PushRefWriter(IEnumerable<Ref> refs, WalkRemoteObjectDatabase dest) : base(refs)
            {
                this.dest = dest;
            }

            protected override void writeFile(string file, byte[] content)
            {
                dest.writeFile(WalkRemoteObjectDatabase.ROOT_DIR + file, content);
            }
        }

        public void Push(IProgressMonitor monitor, IDictionary<string, RemoteRefUpdate> refUpdates)
        {
            markStartedOperation();
            packNames = null;
            newRefs = new Dictionary<string, Ref>();
            packedRefUpdates = new List<RemoteRefUpdate>();

            List<RemoteRefUpdate> updates = new List<RemoteRefUpdate>();
            foreach (RemoteRefUpdate u in refUpdates.Values)
            {
                string n = u.RemoteName;
                if (!n.StartsWith("refs/") || !Repository.IsValidRefName(n))
                {
                    u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                    u.Message = "funny refname";
                    continue;
                }

                if (ObjectId.ZeroId.Equals(u.NewObjectId))
                    deleteCommand(u);
                else
                    updates.Add(u);
            }

            if (updates.Count > 0)
                sendpack(updates, monitor);
            foreach (RemoteRefUpdate u in updates)
                updateCommand(u);

            if (updates.Count > 0 && isNewRepository())
                createNewRepository(updates);

            RefWriter refWriter = new PushRefWriter(newRefs.Values, dest);
            if (packedRefUpdates.Count > 0)
            {
                try
                {
                    refWriter.writePackedRefs();
                    foreach (RemoteRefUpdate u in packedRefUpdates)
                        u.Status = RemoteRefUpdate.UpdateStatus.OK;
                }
                catch (IOException e)
                {
                    foreach (RemoteRefUpdate u in packedRefUpdates)
                    {
                        u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                        u.Message = e.Message;
                    }
                    throw new TransportException(uri, "failed updating refs", e);
                }
            }

            try
            {
                refWriter.writeInfoRefs();
            }
            catch (IOException err)
            {
                throw new TransportException(uri, "failed updating refs", err);
            }
        }

        public override void Close()
        {
            dest.close();
        }

        private void sendpack(IEnumerable<RemoteRefUpdate> updates, IProgressMonitor monitor)
        {
            string pathPack = null;
            string pathIdx = null;
            
            try
            {
                PackWriter pw = new PackWriter(local, monitor);
                List<ObjectId> need = new List<ObjectId>();
                List<ObjectId> have = new List<ObjectId>();

                foreach (RemoteRefUpdate r in updates)
                    need.Add(r.NewObjectId);
                foreach (Ref r in Refs)
                {
                    have.Add(r.ObjectId);
                    if (r.PeeledObjectId != null)
                        have.Add(r.PeeledObjectId);
                }
                pw.preparePack(need, have);

                if (pw.getObjectsNumber() == 0)
                    return;

                packNames = new Dictionary<string, string>();
                foreach (string n in dest.getPackNames())
                    packNames.Add(n, n);

                string b = "pack-" + pw.computeName().Name;
                string packName = b + ".pack";
                pathPack = "pack/" + packName;
                pathIdx = "pack/" + b + ".idx";

                if (packNames.Remove(packName))
                {
                    dest.writeInfoPacks(new List<string>(packNames.Keys));
                    dest.deleteFile(pathIdx);
                }

                string wt = "Put " + b.Slice(0, 12);
                Stream os = dest.writeFile(pathPack, monitor, wt + "..pack");
                try
                {
                    pw.writePack(os);
                }
                finally
                {
                    os.Close();
                }

                os = dest.writeFile(pathIdx, monitor, wt + "..idx");
                try
                {
                    pw.writeIndex(os);
                }
                finally
                {
                    os.Close();
                }

                List<string> infoPacks = new List<string> {packName};
                infoPacks.AddRange(packNames.Keys);
                dest.writeInfoPacks(infoPacks);
            }
            catch (IOException err)
            {
                safeDelete(pathIdx);
                safeDelete(pathPack);

                throw new TransportException(uri, "cannot store objects", err);
            }
        }

        private void safeDelete(string path)
        {
            if (path != null)
            {
                try
                {
                    dest.deleteFile(path);
                }
                catch (IOException)
                {
                    // Ignore the deletion failure. We probably are
                    // already failing and were just trying to pick
                    // up after ourselves.
                }
            }
        }

        private void deleteCommand(RemoteRefUpdate u)
        {
            Ref r = null;
            foreach (string n in newRefs.Keys)
            {
                if (n == u.RemoteName)
                {
                    r = newRefs[n];
                    newRefs.Remove(n);
                }
            }
            
            if (r == null)
            {
                u.Status = RemoteRefUpdate.UpdateStatus.OK;
                return;
            }

            if (r.StorageFormat.IsPacked)
            {
                packedRefUpdates.Add(u);
            }

            if (r.StorageFormat.IsLoose)
            {
                try
                {
                    dest.deleteRef(u.RemoteName);
                    u.Status = RemoteRefUpdate.UpdateStatus.OK;
                }
                catch (IOException e)
                {
                    u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                    u.Message = e.Message;
                }
            }

            try
            {
                dest.deleteRefLog(u.RemoteName);
            }
            catch (IOException e)
            {
                u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                u.Message = e.Message;
            }
        }

        private void updateCommand(RemoteRefUpdate u)
        {
            try
            {
                dest.writeRef(u.RemoteName, u.NewObjectId);
                newRefs.Add(u.RemoteName, new Ref(Ref.Storage.Loose, u.RemoteName, u.NewObjectId));
                u.Status = RemoteRefUpdate.UpdateStatus.OK;
            }
            catch (IOException e)
            {
                u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                u.Message = e.Message;
            }
        }

        private bool isNewRepository()
        {
            return RefsMap.Count == 0 && packNames != null && packNames.Count == 0;
        }

        private void createNewRepository(IList<RemoteRefUpdate> updates)
        {
            try
            {
                string @ref = "ref: " + pickHEAD(updates) + "\n";
                byte[] bytes = Constants.encode(@ref);
                dest.writeFile(WalkRemoteObjectDatabase.ROOT_DIR + Constants.HEAD, bytes);
            }
            catch (IOException e)
            {
                throw new TransportException(uri, "Cannot create HEAD", e);
            }

            try
            {
                const string config = "[core]\n\trepositoryformatversion = 0\n";
                byte[] bytes = Constants.encode(config);
                dest.writeFile(WalkRemoteObjectDatabase.ROOT_DIR + "config", bytes);
            }
            catch (IOException e)
            {
                throw new TransportException(uri, "Cannot create config", e);
            }
        }

        private static string pickHEAD(IList<RemoteRefUpdate> updates)
        {
            foreach (RemoteRefUpdate u in updates)
            {
                string n = u.RemoteName;
                if (n.Equals(Constants.R_HEADS + Constants.MASTER))
                    return n;
            }

            foreach (RemoteRefUpdate u in updates)
            {
                string n = u.RemoteName;
                if (n.StartsWith(Constants.R_HEADS))
                    return n;
            }

            return updates[0].RemoteName;
        }
    }

}