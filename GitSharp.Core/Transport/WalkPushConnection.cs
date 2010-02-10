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

using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    public class WalkPushConnection : BaseConnection, IPushConnection
    {
        private readonly Repository _local;
        private readonly URIish _uri;
        private readonly WalkRemoteObjectDatabase _dest;

        private Dictionary<string, string> _packNames;
        private Dictionary<string, Ref> _newRefs;
        private List<RemoteRefUpdate> _packedRefUpdates;

        public WalkPushConnection(IWalkTransport walkTransport, WalkRemoteObjectDatabase w)
        {
            var t = (Transport) walkTransport;
            _local = t.Local;
            _uri = t.Uri;
            _dest = w;
        }

        public void Push(ProgressMonitor monitor, IDictionary<string, RemoteRefUpdate> refUpdates)
        {
			if (refUpdates == null)
				throw new ArgumentNullException ("refUpdates");
			
            markStartedOperation();
            _packNames = null;
            _newRefs = new Dictionary<string, Ref>();
            _packedRefUpdates = new List<RemoteRefUpdate>();

            var updates = new List<RemoteRefUpdate>();
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
                {
                	DeleteCommand(u);
                }
                else
                {
                	updates.Add(u);
                }
            }

            if (updates.Count > 0)
            {
            	Sendpack(updates, monitor);
            }
            foreach (RemoteRefUpdate u in updates)
            {
            	UpdateCommand(u);
            }

            if (updates.Count > 0 && IsNewRepository)
            {
            	CreateNewRepository(updates);
            }

            RefWriter refWriter = new PushRefWriter(_newRefs.Values, _dest);
            if (_packedRefUpdates.Count > 0)
            {
                try
                {
                    refWriter.writePackedRefs();
                    foreach (RemoteRefUpdate u in _packedRefUpdates)
                    {
                    	u.Status = RemoteRefUpdate.UpdateStatus.OK;
                    }
                }
                catch (IOException e)
                {
                    foreach (RemoteRefUpdate u in _packedRefUpdates)
                    {
                        u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                        u.Message = e.Message;
                    }
                    throw new TransportException(_uri, "failed updating refs", e);
                }
            }

            try
            {
                refWriter.writeInfoRefs();
            }
            catch (IOException err)
            {
                throw new TransportException(_uri, "failed updating refs", err);
            }
        }

        public override void Close()
        {
            _dest.Dispose();
#if DEBUG
            GC.SuppressFinalize(this); // Disarm lock-release checker
#endif
		}

#if DEBUG
        // A debug mode warning if the type has not been disposed properly
        ~WalkPushConnection()
        {
            Console.Error.WriteLine(GetType().Name + " has not been properly disposed: " + this._uri);
        }
#endif


        private void Sendpack(IEnumerable<RemoteRefUpdate> updates, ProgressMonitor monitor)
        {
            string pathPack = null;
            string pathIdx = null;
            
            try
            {
                var pw = new PackWriter(_local, monitor);
                var need = new List<ObjectId>();
                var have = new List<ObjectId>();

                foreach (RemoteRefUpdate r in updates)
                {
                	need.Add(r.NewObjectId);
                }

                foreach (Ref r in Refs)
                {
                    have.Add(r.ObjectId);
                    if (r.PeeledObjectId != null)
                    {
                    	have.Add(r.PeeledObjectId);
                    }
                }
                pw.preparePack(need, have);

                if (pw.getObjectsNumber() == 0) return;

                _packNames = new Dictionary<string, string>();
                foreach (string n in _dest.getPackNames())
                {
                	_packNames.Add(n, n);
                }

                string b = "pack-" + pw.computeName().Name;
                string packName = b + IndexPack.PackSuffix;
                pathPack = "pack/" + packName;
                pathIdx = "pack/" + b + IndexPack.IndexSuffix;

                if (_packNames.Remove(packName))
                {
                    _dest.writeInfoPacks(new List<string>(_packNames.Keys));
                    _dest.deleteFile(pathIdx);
                }

                string wt = "Put " + b.Slice(0, 12);
                using (Stream os = _dest.writeFile(pathPack, monitor, wt + "." + IndexPack.PackSuffix))
                {
                    pw.writePack(os);
                }

                using (Stream os = _dest.writeFile(pathIdx, monitor, wt + "." + IndexPack.IndexSuffix))
                {
                    pw.writeIndex(os);
                }

                var infoPacks = new List<string> {packName};
                infoPacks.AddRange(_packNames.Keys);
                _dest.writeInfoPacks(infoPacks);
            }
            catch (IOException err)
            {
                SafeDelete(pathIdx);
                SafeDelete(pathPack);

                throw new TransportException(_uri, "cannot store objects", err);
            }
        }

        private void SafeDelete(string path)
        {
            if (path != null)
            {
                try
                {
                    _dest.deleteFile(path);
                }
                catch (IOException)
                {
                    // Ignore the deletion failure. We probably are
                    // already failing and were just trying to pick
                    // up After ourselves.
                }
            }
        }

        private void DeleteCommand(RemoteRefUpdate u)
        {
            Ref r = null;
            foreach (string n in _newRefs.Keys)
            {
                if (n == u.RemoteName)
                {
                    r = _newRefs[n];
                    _newRefs.Remove(n);
                }
            }
            
            if (r == null)
            {
                u.Status = RemoteRefUpdate.UpdateStatus.OK;
                return;
            }

            if (r.StorageFormat.IsPacked)
            {
                _packedRefUpdates.Add(u);
            }

            if (r.StorageFormat.IsLoose)
            {
                try
                {
                    _dest.deleteRef(u.RemoteName);
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
                _dest.deleteRefLog(u.RemoteName);
            }
            catch (IOException e)
            {
                u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                u.Message = e.Message;
            }
        }

        private void UpdateCommand(RemoteRefUpdate u)
        {
            try
            {
                _dest.writeRef(u.RemoteName, u.NewObjectId);
                _newRefs.Add(u.RemoteName, new Ref(Ref.Storage.Loose, u.RemoteName, u.NewObjectId));
                u.Status = RemoteRefUpdate.UpdateStatus.OK;
            }
            catch (IOException e)
            {
                u.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                u.Message = e.Message;
            }
        }

    	private bool IsNewRepository
    	{
    		get { return RefsMap.Count == 0 && _packNames != null && _packNames.Count == 0; }
    	}

    	private void CreateNewRepository(IList<RemoteRefUpdate> updates)
        {
            try
            {
                string @ref = "ref: " + PickHead(updates) + "\n";
                byte[] bytes = Constants.encode(@ref);
                _dest.writeFile(WalkRemoteObjectDatabase.ROOT_DIR + Constants.HEAD, bytes);
            }
            catch (IOException e)
            {
                throw new TransportException(_uri, "Cannot Create HEAD", e);
            }

            try
            {
                const string config = "[core]\n\trepositoryformatversion = 0\n";
                byte[] bytes = Constants.encode(config);
                _dest.writeFile(WalkRemoteObjectDatabase.ROOT_DIR + "config", bytes);
            }
            catch (IOException e)
            {
                throw new TransportException(_uri, "Cannot Create config", e);
            }
        }

        private static string PickHead(IList<RemoteRefUpdate> updates)
        {
            foreach (RemoteRefUpdate u in updates)
            {
                string n = u.RemoteName;
                if (n.Equals(Constants.R_HEADS + Constants.MASTER))
                {
                	return n;
                }
            }

            foreach (RemoteRefUpdate u in updates)
            {
                string n = u.RemoteName;
                if (n.StartsWith(Constants.R_HEADS))
                {
                	return n;
                }
            }

            return updates[0].RemoteName;
		}

		#region Nested Types

		private class PushRefWriter : RefWriter
		{
			private readonly WalkRemoteObjectDatabase _dest;

			public PushRefWriter(IEnumerable<Ref> refs, WalkRemoteObjectDatabase dest)
				: base(refs)
			{
				_dest = dest;
			}

			protected override void writeFile(string file, byte[] content)
			{
				_dest.writeFile(WalkRemoteObjectDatabase.ROOT_DIR + file, content);
			}
		}

		#endregion
	}
}