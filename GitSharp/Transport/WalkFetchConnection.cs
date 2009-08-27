/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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

using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Exceptions;
using GitSharp.RevWalk;
using GitSharp.Util;

namespace GitSharp.Transport
{

    public class WalkFetchConnection : BaseFetchConnection
    {
        private readonly Repository local;
        private readonly ObjectChecker objCheck;
        private readonly List<WalkRemoteObjectDatabase> remotes;
        private int lastRemoteIdx;
        private readonly RevWalk.RevWalk revWalk;
        private readonly TreeWalk.TreeWalk treeWalk;

        private readonly RevFlag COMPLETE;
        private readonly RevFlag IN_WORK_QUEUE;
        private readonly RevFlag LOCALLY_SEEN;

        private readonly DateRevQueue localCommitQueue;
        private LinkedList<ObjectId> workQueue;
        private readonly LinkedList<WalkRemoteObjectDatabase> noPacksYet;
        private readonly LinkedList<WalkRemoteObjectDatabase> noAlternatesYet;
        private readonly LinkedList<RemotePack> unfetchedPacks;
        private readonly List<string> packsConsidered;
        private readonly MutableObjectId idBuffer = new MutableObjectId();
        private readonly MessageDigest objectDigest = Constants.newMessageDigest();

        private readonly Dictionary<ObjectId, List<Exception>> fetchErrors;
        private string lockMessage;
        private readonly List<PackLock> packLocks;

        public WalkFetchConnection(IWalkTransport t, WalkRemoteObjectDatabase w)
        {
            Transport wt = (Transport) t;
            local = wt.Local;
            objCheck = wt.CheckFetchedObjects ? new ObjectChecker() : null;

            remotes = new List<WalkRemoteObjectDatabase> {w};

            unfetchedPacks = new LinkedList<RemotePack>();
            packsConsidered = new List<string>();

            noPacksYet = new LinkedList<WalkRemoteObjectDatabase>();
            noPacksYet.AddFirst(w);

            noAlternatesYet = new LinkedList<WalkRemoteObjectDatabase>();
            noAlternatesYet.AddFirst(w);

            fetchErrors = new Dictionary<ObjectId, List<Exception>>();
            packLocks = new List<PackLock>(4);

            revWalk = new RevWalk.RevWalk(local);
            treeWalk = new TreeWalk.TreeWalk(local);
            COMPLETE = revWalk.newFlag("COMPLETE");
            IN_WORK_QUEUE = revWalk.newFlag("IN_WORK_QUEUE");
            LOCALLY_SEEN = revWalk.newFlag("LOCALLY_SEEN");

            localCommitQueue = new DateRevQueue();
            workQueue = new LinkedList<ObjectId>();
        }

        public override bool DidFetchTestConnectivity
        {
            get
            {
                return true;
            }
        }

        public override List<PackLock> PackLocks
        {
            get { return packLocks; }
        }

        public override void SetPackLockMessage(string message)
        {
            lockMessage = message;
        }

        public override void Close()
        {
            foreach (RemotePack p in unfetchedPacks)
                p.tmpIdx.Delete();
            foreach (WalkRemoteObjectDatabase r in remotes)
                r.close();
        }

        protected override void doFetch(IProgressMonitor monitor, List<Ref> want, List<ObjectId> have)
        {
            markLocalRefsComplete(have);
            queueWants(want);

            while (!monitor.IsCancelled && workQueue.Count > 0)
            {
                ObjectId id = workQueue.First.Value;
                workQueue.RemoveFirst();
                if (!(id is RevObject) || !((RevObject)id).has(COMPLETE))
                    downloadObject(monitor, id);
                process(id);
            }
        }

        private void queueWants(IEnumerable<Ref> want)
        {
            List<ObjectId> inWorkQueue = new List<ObjectId>();
            foreach (Ref r in want)
            {
                ObjectId id = r.ObjectId;
                try
                {
                    RevObject obj = revWalk.parseAny(id);
                    if (obj.has(COMPLETE))
                        continue;
                    inWorkQueue.Add(id);
                    obj.add(IN_WORK_QUEUE);
                    workQueue.AddLast(obj);
                }
                catch (MissingObjectException)
                {
                    inWorkQueue.Add(id);
                    workQueue.AddLast(id);
                }
                catch (IOException e)
                {
                    throw new TransportException("Cannot read " + id.Name, e);
                }
            }
        }

        private void process(ObjectId id)
        {
            RevObject obj;
            try
            {
                if (id is RevObject)
                {
                    obj = (RevObject) id;
                    if (obj.has(COMPLETE))
                        return;
                    revWalk.parse(obj);
                }
                else
                {
                    obj = revWalk.parseAny(id);
                    if (obj.has(COMPLETE))
                        return;
                }
            }
            catch (IOException e)
            {
                throw new TransportException("Cannot read " + id.Name, e);
            }

            obj.dispose();

            switch (obj.getType())
            {
                case Constants.OBJ_BLOB:
                    processBlob(obj);
                    break;

                case Constants.OBJ_TREE:
                    processTree(obj);
                    break;
                    
                case Constants.OBJ_COMMIT:
                    processCommit(obj);
                    break;

                case Constants.OBJ_TAG:
                    processTag(obj);
                    break;

                default:
                    throw new TransportException("Unknown object type " + id.Name + " (" + obj.getType() + ")");
            }

            fetchErrors.Remove(id.Copy());
        }

        private void processBlob(RevObject obj)
        {
            if (!local.HasObject(obj))
                throw new TransportException("Cannot read blob " + obj.Name, new MissingObjectException(obj, Constants.TYPE_BLOB));
            obj.add(COMPLETE);
        }

        private void processTree(RevObject obj)
        {
            try
            {
                treeWalk.reset(obj);
                while (treeWalk.next())
                {
                    FileMode mode = treeWalk.getFileMode(0);
                    int sType = mode.Bits;

                    switch (sType)
                    {
                        case Constants.OBJ_BLOB:
                        case Constants.OBJ_TREE:
                            treeWalk.getObjectId(idBuffer, 0);
                            needs(revWalk.lookupAny(idBuffer, sType));
                            continue;

                        default:
                            if (FileMode.GitLink.Equals(sType))
                                continue;
                            treeWalk.getObjectId(idBuffer, 0);
                            throw new CorruptObjectException("Invalid mode " + mode + " for " + idBuffer.Name + " " +
                                                             treeWalk.getPathString() + " in " + obj.getId().Name + ".");
                    }
                }
            }
            catch (IOException ioe)
            {
                throw new TransportException("Cannot read tree " + obj.Name, ioe);
            }
            obj.add(COMPLETE);
        }

        private void processCommit(RevObject obj)
        {
            RevCommit commit = (RevCommit) obj;
            markLocalCommitsComplete(commit.getCommitTime());
            needs(commit.getTree());
            foreach (RevCommit p in commit.getParents())
                needs(p);
            obj.add(COMPLETE);
        }

        private void processTag(RevObject obj)
        {
            RevTag tag = (RevTag) obj;
            needs(tag.getObject());
            obj.add(COMPLETE);
        }

        private void needs(RevObject obj)
        {
            if (obj.has(COMPLETE))
                return;
            if (!obj.has(IN_WORK_QUEUE))
            {
                obj.add(IN_WORK_QUEUE);
                workQueue.AddLast(obj);
            }
        }

        private void downloadObject(IProgressMonitor pm, AnyObjectId id)
        {
            if (local.HasObject(id))
                return;

            for (;;)
            {
                if (downloadPackedObject(pm, id))
                    return;

                string idStr = id.Name;
                string subdir = idStr.Slice(0, 2);
                string file = idStr.Substring(2);
                string looseName = subdir + "/" + file;

                for (int i = lastRemoteIdx; i < remotes.Count; i++)
                {
                    if (downloadLooseObject(id, looseName, remotes[i]))
                    {
                        lastRemoteIdx = i;
                        return;
                    }
                }

                for (int i = 0; i < lastRemoteIdx; i++)
                {
                    if (downloadLooseObject(id, looseName, remotes[i]))
                    {
                        lastRemoteIdx = i;
                        return;
                    }
                }

                while (noPacksYet.Count > 0)
                {
                    WalkRemoteObjectDatabase wrr = noPacksYet.First.Value;
                    noPacksYet.RemoveFirst();
                    List<string> packNameList;
                    try
                    {
                        pm.BeginTask("Listing packs", -1);
                        packNameList = wrr.getPackNames();
                    }
                    catch (IOException e)
                    {
                        recordError(id, e);
                        continue;
                    }
                    finally
                    {
                        pm.EndTask();
                    }

                    if (packNameList == null || packNameList.Count == 0)
                        continue;
                    foreach (string packName in packNameList)
                    {
                        if (!packsConsidered.Contains(packName))
                        {
                            packsConsidered.Add(packName);
                            unfetchedPacks.AddLast(new RemotePack(lockMessage, packLocks, objCheck, local, wrr, packName));
                        }
                    }
                    if (downloadPackedObject(pm, id))
                        return;
                }

                List<WalkRemoteObjectDatabase> al = expandOneAlternate(id, pm);
                if (al != null && al.Count > 0)
                {
                    foreach (WalkRemoteObjectDatabase alt in al)
                    {
                        remotes.Add(alt);
                        noPacksYet.AddLast(alt);
                        noAlternatesYet.AddLast(alt);
                    }
                    continue;
                }

                List<Exception> failures = null;
                if (fetchErrors.ContainsKey(id.Copy()))
                {
                    failures = fetchErrors[id.Copy()];
                }

                TransportException te = null;
                if (failures != null && failures.Count > 0)
                {
                    if (failures.Count == 1)
                        te = new TransportException("Cannot get " + id.Name + ".", failures[0]);
                    // TODO: no CompoundException
                    //else
                    //    te =  = new TransportException("Cannot get " + id.Name + ".", new CompoundException());  
                }
                if (te == null) te = new TransportException("Cannot get " + id.Name + ".");
                throw te;
            }
        }

        private bool downloadPackedObject(IProgressMonitor monitor, AnyObjectId id)
        {
            IEnumerator<RemotePack> packItr = unfetchedPacks.GetEnumerator();
            while (packItr.MoveNext() && !monitor.IsCancelled)
            {
                RemotePack pack = packItr.Current;
                try
                {
                    pack.openIndex(monitor);
                }
                catch (IOException err)
                {
                    recordError(id, err);
                    unfetchedPacks.Remove(pack);
                    continue;
                }

                if (monitor.IsCancelled)
                    return false;

                if (!pack.index.HasObject(id))
                    continue;

                try
                {
                    pack.downloadPack(monitor);
                }
                catch (IOException err)
                {
                    recordError(id, err);
                    continue;
                }
                finally
                {
                    pack.tmpIdx.Delete();
                    unfetchedPacks.Remove(pack);
                }

                if (!local.HasObject(id))
                {
                    recordError(id,
                                new FileNotFoundException("Object " + id.Name + " not found in " + pack.packName + "."));
                    continue;
                }

                IEnumerator<ObjectId> pending = swapFetchQueue();
                while (pending.MoveNext())
                {
                    ObjectId p = pending.Current;
                    if (pack.index.HasObject(p))
                    {
                        workQueue.Remove(p);
                        process(p);
                    }
                    else
                        workQueue.AddLast(p);
                }
                return true;
            }
            return false;
        }

        private IEnumerator<ObjectId> swapFetchQueue()
        {
            IEnumerator<ObjectId> r = workQueue.GetEnumerator();
            workQueue = new LinkedList<ObjectId>();
            return r;
        }

        private bool downloadLooseObject(AnyObjectId id, string looseName, WalkRemoteObjectDatabase remote)
        {
            try
            {
                byte[] compressed = remote.open(looseName).toArray();
                verifyLooseObject(id, compressed);
                saveLooseObject(id, compressed);
                return true;
            }
            catch (FileNotFoundException e)
            {
                recordError(id, e);
                return false;
            }
            catch (IOException e)
            {
                throw new TransportException("Cannot download " + id.Name, e);
            }
        }

        private void verifyLooseObject(AnyObjectId id, byte[] compressed)
        {
            UnpackedObjectLoader uol;
            try
            {
                uol = new UnpackedObjectLoader(compressed);
            }
            catch (CorruptObjectException parsingError)
            {
                FileNotFoundException e = new FileNotFoundException(id.Name, parsingError);
                throw e;
            }

            objectDigest.Reset();
            objectDigest.Update(Constants.encodedTypeString(uol.getType()));
            objectDigest.Update((byte) ' ');
            objectDigest.Update(Constants.encodeASCII(uol.getSize()));
            objectDigest.Update(0);
            objectDigest.Update(uol.getCachedBytes());
            idBuffer.FromRaw(objectDigest.Digest(), 0);

            if (!id.Equals(idBuffer))
            {
                throw new TransportException("Incorrect hash for " + id.Name + "; computed " + idBuffer.Name + " as a " +
                                             Constants.typeString(uol.getType()) + " from " + compressed.Length +
                                             " bytes.");
            }
            if (objCheck != null)
            {
                try
                {
                    objCheck.check(uol.getType(), uol.getCachedBytes());
                }
                catch (CorruptObjectException e)
                {
                    throw new TransportException("Invalid " + Constants.typeString(uol.getType()) + " " + id.Name + ": " + e.Message);
                }
            }
        }

        private void saveLooseObject(AnyObjectId id, byte[] compressed)
        {
            FileInfo tmp = new FileInfo(Path.Combine(local.ObjectsDirectory.ToString(), Path.GetTempFileName()));
            try
            {
                FileStream stream = File.Create(tmp.ToString());
                try
                {
                    stream.Write(compressed, 0, compressed.Length);
                }
                finally
                {
                    stream.Close();
                }
                tmp.Attributes |= FileAttributes.ReadOnly;
            }
            catch (IOException e)
            {
                File.Delete(tmp.ToString());
                throw e;
            }

            FileInfo o = local.ToFile(id);
            if (tmp.RenameTo(o.ToString()))
                return;

            Directory.CreateDirectory(tmp.ToString());
            if (tmp.RenameTo(o.ToString()))
                return;

            tmp.Delete();
            if (local.HasObject(id))
                return;

            throw new ObjectWritingException("Unable to store " + id.Name + ".");
        }

        private List<WalkRemoteObjectDatabase> expandOneAlternate(AnyObjectId id, IProgressMonitor pm)
        {
            while (noAlternatesYet.Count > 0)
            {
                WalkRemoteObjectDatabase wrr = noAlternatesYet.First.Value;
                noAlternatesYet.RemoveFirst();
                try
                {
                    pm.BeginTask("Listing alternates", -1);
                    List<WalkRemoteObjectDatabase> altList = wrr.getAlternates();
                    if (altList != null && altList.Count > 0)
                        return altList;
                }
                catch (IOException e)
                {
                    recordError(id, e);
                }
                finally
                {
                    pm.EndTask();
                }
            }
            return null;
        }

        private void markLocalRefsComplete(IEnumerable<ObjectId> have)
        {
            foreach (Ref r in local.Refs.Values)
            {
                try
                {
                    markLocalObjComplete(revWalk.parseAny(r.ObjectId));
                }
                catch (IOException readError)
                {
                    throw new TransportException("Local ref " + r.Name + " is missing object(s).", readError);
                }
            }

            foreach (ObjectId id in have)
            {
                try
                {
                    markLocalObjComplete(revWalk.parseAny(id));
                }
                catch (IOException readError)
                {
                    throw new TransportException("Missing assumed " + id.Name, readError);
                }
            }
        }

        private void markLocalObjComplete(RevObject obj)
        {
            while (obj.getType() == Constants.OBJ_TAG)
            {
                obj.add(COMPLETE);
                obj.dispose();
                obj = ((RevTag) obj).getObject();
                revWalk.parse(obj);
            }

            switch (obj.getType())
            {
                case Constants.OBJ_BLOB:
                    obj.add(COMPLETE);
                    break;

                case Constants.OBJ_COMMIT:
                    pushLocalCommit((RevCommit) obj);
                    break;

                case Constants.OBJ_TREE:
                    markTreeComplete(obj);
                    break;
            }
        }

        private void markLocalCommitsComplete(int until)
        {
            try
            {
                for (;;)
                {
                    RevCommit c = localCommitQueue.peek();
                    if (c == null || c.getCommitTime() < until)
                        return;
                    localCommitQueue.next();

                    markTreeComplete(c.getTree());
                    foreach (RevCommit p in c.getParents())
                        pushLocalCommit(p);
                }
            }
            catch (IOException err)
            {
                throw new TransportException("Local objects incomplete.", err);
            }
        }

        private void pushLocalCommit(RevCommit p)
        {
            if (p.has(LOCALLY_SEEN))
                return;
            revWalk.parse(p);
            p.add(LOCALLY_SEEN);
            p.add(COMPLETE);
            p.carry(COMPLETE);
            p.dispose();
            localCommitQueue.add(p);
        }

        private void markTreeComplete(RevObject tree)
        {
            if (tree.has(COMPLETE))
                return;

            tree.add(COMPLETE);
            treeWalk.reset(tree);
            while (treeWalk.next())
            {
                FileMode mode = treeWalk.getFileMode(0);
                int sType = mode.Bits;

                switch (sType)
                {
                    case Constants.OBJ_BLOB:
                        treeWalk.getObjectId(idBuffer, 0);
                        revWalk.lookupAny(idBuffer, sType).add(COMPLETE);
                        continue;

                    case Constants.OBJ_TREE:
                        {
                            treeWalk.getObjectId(idBuffer, 0);
                            RevObject o = revWalk.lookupAny(idBuffer, sType);
                            if (!o.has(COMPLETE))
                            {
                                o.add(COMPLETE);
                                treeWalk.enterSubtree();
                            }
                            continue;
                        }

                    default:
                        if (FileMode.GitLink.Equals(sType))
                            continue;
                        treeWalk.getObjectId(idBuffer, 0);
                        throw new CorruptObjectException("Invalid mode " + mode + " for " + idBuffer.Name + " " +
                                                         treeWalk.getPathString() + " in " + tree.Name);
                }
            }
        }

        private void recordError(AnyObjectId id, Exception e)
        {
            ObjectId objId = id.Copy();
            if (fetchErrors.ContainsKey(objId))
            {
                fetchErrors[objId].Add(e);
            }
            else
            {
                fetchErrors.Add(objId, new List<Exception>(2){e});
            }
        }

        private class RemotePack
        {
            public readonly WalkRemoteObjectDatabase connection;
            public readonly string packName;
            public readonly string idxName;
            public readonly FileInfo tmpIdx;
            public PackIndex index;
            private readonly Repository local;
            private readonly ObjectChecker objCheck;
            private readonly string lockMessage;
            private readonly List<PackLock> packLocks;

            public RemotePack(string lM, List<PackLock> pL, ObjectChecker oC, Repository r, WalkRemoteObjectDatabase c, string pn)
            {
                lockMessage = lM;
                packLocks = pL;
                objCheck = oC;
                local = r;
                DirectoryInfo objdir = local.ObjectsDirectory;
                connection = c;
                packName = pn;
                idxName = packName.Slice(0, packName.Length - 5) + ".idx";

                string tn = idxName;
                if (tn.StartsWith("pack-"))
                    tn = tn.Substring(5);
                if (tn.EndsWith(".idx"))
                    tn = tn.Slice(0, tn.Length - 4);
                tmpIdx = new FileInfo(Path.Combine(objdir.ToString(), "walk-" + tn + ".walkidx"));
            }

            public void openIndex(IProgressMonitor pm)
            {
                if (index != null)
                    return;
                
                try
                {
                    index = PackIndex.Open(tmpIdx);
                    return;
                }
                catch(FileNotFoundException)
                {
                    
                }

                Stream s = connection.open("pack/" + idxName);
                pm.BeginTask("Get " + idxName.Slice(0, 12) + "..idx", s.Length < 0 ? -1 : (int)(s.Length / 1024));
                try
                {
                    FileStream fos = new FileStream(tmpIdx.ToString(), System.IO.FileMode.Open, FileAccess.ReadWrite);
                    try
                    {
                        byte[] buf = new byte[2048];
                        int cnt;
                        while (!pm.IsCancelled && (cnt = s.Read(buf, 0, buf.Length)) >= 0)
                        {
                            fos.Write(buf, 0, cnt);
                            pm.Update(cnt / 1024);
                        }
                    }
                    finally
                    {
                        fos.Close();
                    }
                }
                catch (IOException err)
                {
                    tmpIdx.Delete();
                    throw err;
                }
                finally
                {
                    s.Close();
                }
                pm.EndTask();

                if (pm.IsCancelled)
                {
                    tmpIdx.Delete();
                    return;
                }

                try
                {
                    index = PackIndex.Open(tmpIdx);
                }
                catch (IOException e)
                {
                    tmpIdx.Delete();
                    throw e;
                }
            }

            public void downloadPack(IProgressMonitor monitor)
            {
                Stream s = connection.open("pack/" + packName);
                IndexPack ip = IndexPack.create(local, s);
                ip.setFixThin(false);
                ip.setObjectChecker(objCheck);
                ip.index(monitor);
                PackLock keep = ip.renameAndOpenPack(lockMessage);
                if (keep != null)
                    packLocks.Add(keep);
            }
        }
    }

}