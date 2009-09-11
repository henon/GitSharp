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
		private readonly RevFlag COMPLETE;
		private readonly RevFlag IN_WORK_QUEUE;
		private readonly RevFlag LOCALLY_SEEN;

		private readonly Repository _local;
		private readonly ObjectChecker _objCheck;
		private readonly List<WalkRemoteObjectDatabase> _remotes;
		private readonly RevWalk.RevWalk _revWalk;
		private readonly TreeWalk.TreeWalk _treeWalk;
		private readonly DateRevQueue _localCommitQueue;
		private readonly LinkedList<WalkRemoteObjectDatabase> _noPacksYet;
		private readonly LinkedList<WalkRemoteObjectDatabase> _noAlternatesYet;
		private readonly LinkedList<RemotePack> _unfetchedPacks;
		private readonly List<string> _packsConsidered;
		private readonly MutableObjectId _idBuffer;
		private readonly MessageDigest _objectDigest;
		private readonly Dictionary<ObjectId, List<Exception>> _fetchErrors;
		private readonly List<PackLock> _packLocks;

		private int _lastRemoteIdx;
		private string _lockMessage;
		private LinkedList<ObjectId> _workQueue;

		public WalkFetchConnection(IWalkTransport t, WalkRemoteObjectDatabase w)
		{
			_idBuffer = new MutableObjectId();
			_objectDigest = Constants.newMessageDigest();

			var wt = (Transport)t;
			_local = wt.Local;
			_objCheck = wt.CheckFetchedObjects ? new ObjectChecker() : null;

			_remotes = new List<WalkRemoteObjectDatabase> { w };

			_unfetchedPacks = new LinkedList<RemotePack>();
			_packsConsidered = new List<string>();

			_noPacksYet = new LinkedList<WalkRemoteObjectDatabase>();
			_noPacksYet.AddFirst(w);

			_noAlternatesYet = new LinkedList<WalkRemoteObjectDatabase>();
			_noAlternatesYet.AddFirst(w);

			_fetchErrors = new Dictionary<ObjectId, List<Exception>>();
			_packLocks = new List<PackLock>(4);

			_revWalk = new RevWalk.RevWalk(_local);
			_treeWalk = new TreeWalk.TreeWalk(_local);

			COMPLETE = _revWalk.newFlag("COMPLETE");
			IN_WORK_QUEUE = _revWalk.newFlag("IN_WORK_QUEUE");
			LOCALLY_SEEN = _revWalk.newFlag("LOCALLY_SEEN");

			_localCommitQueue = new DateRevQueue();
			_workQueue = new LinkedList<ObjectId>();
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
			get { return _packLocks; }
		}

		public override void SetPackLockMessage(string message)
		{
			_lockMessage = message;
		}

		public override void Close()
		{
			foreach (RemotePack p in _unfetchedPacks)
			{
				p.TmpIdx.Delete();
			}
			foreach (WalkRemoteObjectDatabase r in _remotes)
			{
				r.close();
			}
		}

		protected override void doFetch(IProgressMonitor monitor, List<Ref> want, List<ObjectId> have)
		{
			MarkLocalRefsComplete(have);
			QueueWants(want);

			while (!monitor.IsCancelled && _workQueue.Count > 0)
			{
				ObjectId id = _workQueue.First.Value;
				_workQueue.RemoveFirst();
				if (!(id is RevObject) || !((RevObject)id).has(COMPLETE))
				{
					DownloadObject(monitor, id);
				}
				Process(id);
			}
		}

		private void QueueWants(IEnumerable<Ref> want)
		{
			var inWorkQueue = new List<ObjectId>();
			foreach (Ref r in want)
			{
				ObjectId id = r.ObjectId;
				try
				{
					RevObject obj = _revWalk.parseAny(id);
					if (obj.has(COMPLETE))
						continue;
					inWorkQueue.Add(id);
					obj.add(IN_WORK_QUEUE);
					_workQueue.AddLast(obj);
				}
				catch (MissingObjectException)
				{
					inWorkQueue.Add(id);
					_workQueue.AddLast(id);
				}
				catch (IOException e)
				{
					throw new TransportException("Cannot Read " + id.Name, e);
				}
			}
		}

		private void Process(ObjectId id)
		{
			RevObject obj;
			try
			{
				if (id is RevObject)
				{
					obj = (RevObject)id;
					if (obj.has(COMPLETE))
						return;
					_revWalk.parseHeaders(obj);
				}
				else
				{
					obj = _revWalk.parseAny(id);
					if (obj.has(COMPLETE))
						return;
				}
			}
			catch (IOException e)
			{
				throw new TransportException("Cannot Read " + id.Name, e);
			}

			obj.DisposeBody();

			switch (obj.Type)
			{
				case Constants.OBJ_BLOB:
					ProcessBlob(obj);
					break;

				case Constants.OBJ_TREE:
					ProcessTree(obj);
					break;

				case Constants.OBJ_COMMIT:
					ProcessCommit(obj);
					break;

				case Constants.OBJ_TAG:
					ProcessTag(obj);
					break;

				default:
					throw new TransportException("Unknown object type " + id.Name + " (" + obj.Type + ")");
			}

			_fetchErrors.Remove(id.Copy());
		}

		private void ProcessBlob(RevObject obj)
		{
			if (!_local.HasObject(obj))
			{
				throw new TransportException("Cannot Read blob " + obj.Name, new MissingObjectException(obj, Constants.TYPE_BLOB));
			}
			obj.add(COMPLETE);
		}

		private void ProcessTree(RevObject obj)
		{
			try
			{
				_treeWalk.reset(obj);
				while (_treeWalk.next())
				{
					FileMode mode = _treeWalk.getFileMode(0);
					int sType = mode.Bits;

					switch (sType)
					{
						case Constants.OBJ_BLOB:
						case Constants.OBJ_TREE:
							_treeWalk.getObjectId(_idBuffer, 0);
							Needs(_revWalk.lookupAny(_idBuffer, sType));
							continue;

						default:
							if (FileMode.GitLink.Equals(sType))
								continue;
							_treeWalk.getObjectId(_idBuffer, 0);
							throw new CorruptObjectException("Invalid mode " + mode + " for " + _idBuffer.Name + " " +
															 _treeWalk.getPathString() + " in " + obj.getId().Name + ".");
					}
				}
			}
			catch (IOException ioe)
			{
				throw new TransportException("Cannot Read tree " + obj.Name, ioe);
			}
			obj.add(COMPLETE);
		}

		private void ProcessCommit(RevObject obj)
		{
			var commit = (RevCommit)obj;
			MarkLocalCommitsComplete(commit.CommitTime);
			Needs(commit.Tree);
			foreach (RevCommit p in commit.Parents)
			{
				Needs(p);
			}
			obj.add(COMPLETE);
		}

		private void ProcessTag(RevObject obj)
		{
			var tag = (RevTag)obj;
			Needs(tag.getObject());
			obj.add(COMPLETE);
		}

		private void Needs(RevObject obj)
		{
			if (obj.has(COMPLETE)) return;

			if (!obj.has(IN_WORK_QUEUE))
			{
				obj.add(IN_WORK_QUEUE);
				_workQueue.AddLast(obj);
			}
		}

		private void DownloadObject(IProgressMonitor pm, AnyObjectId id)
		{
			if (_local.HasObject(id)) return;

			while (true)
			{
				if (DownloadPackedObject(pm, id))
					return;

				string idStr = id.Name;
				string subdir = idStr.Slice(0, 2);
				string file = idStr.Substring(2);
				string looseName = subdir + "/" + file;

				for (int i = _lastRemoteIdx; i < _remotes.Count; i++)
				{
					if (DownloadLooseObject(id, looseName, _remotes[i]))
					{
						_lastRemoteIdx = i;
						return;
					}
				}

				for (int i = 0; i < _lastRemoteIdx; i++)
				{
					if (DownloadLooseObject(id, looseName, _remotes[i]))
					{
						_lastRemoteIdx = i;
						return;
					}
				}

				while (_noPacksYet.Count > 0)
				{
					WalkRemoteObjectDatabase wrr = _noPacksYet.First.Value;
					_noPacksYet.RemoveFirst();
					List<string> packNameList;
					try
					{
						pm.BeginTask("Listing packs", -1);
						packNameList = wrr.getPackNames();
					}
					catch (IOException e)
					{
						RecordError(id, e);
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
						if (!_packsConsidered.Contains(packName))
						{
							_packsConsidered.Add(packName);
							_unfetchedPacks.AddLast(new RemotePack(_lockMessage, _packLocks, _objCheck, _local, wrr, packName));
						}
					}
					if (DownloadPackedObject(pm, id))
						return;
				}

				List<WalkRemoteObjectDatabase> al = ExpandOneAlternate(id, pm);
				if (al != null && al.Count > 0)
				{
					foreach (WalkRemoteObjectDatabase alt in al)
					{
						_remotes.Add(alt);
						_noPacksYet.AddLast(alt);
						_noAlternatesYet.AddLast(alt);
					}
					continue;
				}

				List<Exception> failures = null;
				if (_fetchErrors.ContainsKey(id.Copy()))
				{
					failures = _fetchErrors[id.Copy()];
				}

				TransportException te = null;
				if (failures != null && failures.Count > 0)
				{
					te = failures.Count == 1 ? 
						new TransportException("Cannot get " + id.Name + ".", failures[0]) : 
						new TransportException("Cannot get " + id.Name + ".", new CompoundException(failures));
				}

				if (te == null)
				{
					te = new TransportException("Cannot get " + id.Name + ".");
				}

				throw te;
			}
		}

		private bool DownloadPackedObject(IProgressMonitor monitor, AnyObjectId id)
		{
			IEnumerator<RemotePack> packItr = _unfetchedPacks.GetEnumerator();
			while (packItr.MoveNext() && !monitor.IsCancelled)
			{
				RemotePack pack = packItr.Current;
				try
				{
					pack.OpenIndex(monitor);
				}
				catch (IOException err)
				{
					RecordError(id, err);
					_unfetchedPacks.Remove(pack);
					continue;
				}

				if (monitor.IsCancelled)
					return false;

				if (!pack.Index.HasObject(id))
					continue;

				try
				{
					pack.DownloadPack(monitor);
				}
				catch (IOException err)
				{
					RecordError(id, err);
					continue;
				}
				finally
				{
					pack.TmpIdx.Delete();
					_unfetchedPacks.Remove(pack);
				}

				if (!_local.HasObject(id))
				{
					RecordError(id,
								new FileNotFoundException("Object " + id.Name + " not found in " + pack.PackName + "."));
					continue;
				}

				IEnumerator<ObjectId> pending = SwapFetchQueue();
				while (pending.MoveNext())
				{
					ObjectId p = pending.Current;
					if (pack.Index.HasObject(p))
					{
						_workQueue.Remove(p);
						Process(p);
					}
					else
						_workQueue.AddLast(p);
				}
				return true;
			}
			return false;
		}

		private IEnumerator<ObjectId> SwapFetchQueue()
		{
			IEnumerator<ObjectId> r = _workQueue.GetEnumerator();
			_workQueue = new LinkedList<ObjectId>();
			return r;
		}

		private bool DownloadLooseObject(AnyObjectId id, string looseName, WalkRemoteObjectDatabase remote)
		{
			try
			{
				byte[] compressed = remote.open(looseName).toArray();
				VerifyLooseObject(id, compressed);
				SaveLooseObject(id, compressed);
				return true;
			}
			catch (FileNotFoundException e)
			{
				RecordError(id, e);
				return false;
			}
			catch (IOException e)
			{
				throw new TransportException("Cannot download " + id.Name, e);
			}
		}

		private void VerifyLooseObject(AnyObjectId id, byte[] compressed)
		{
			UnpackedObjectLoader uol;
			try
			{
				uol = new UnpackedObjectLoader(compressed);
			}
			catch (CorruptObjectException parsingError)
			{
				var e = new FileNotFoundException(id.Name, parsingError);
				throw e;
			}

			_objectDigest.Reset();
			_objectDigest.Update(Constants.encodedTypeString(uol.Type));
			_objectDigest.Update((byte)' ');
			_objectDigest.Update(Constants.encodeASCII(uol.Size));
			_objectDigest.Update(0);
			_objectDigest.Update(uol.CachedBytes);
			_idBuffer.FromRaw(_objectDigest.Digest(), 0);

			if (!id.Equals(_idBuffer))
			{
				throw new TransportException("Incorrect hash for " + id.Name + "; computed " + _idBuffer.Name + " as a " +
											 Constants.typeString(uol.Type) + " from " + compressed.Length +
											 " bytes.");
			}
			if (_objCheck != null)
			{
				try
				{
					_objCheck.check(uol.Type, uol.CachedBytes);
				}
				catch (CorruptObjectException e)
				{
					throw new TransportException("Invalid " + Constants.typeString(uol.Type) + " " + id.Name + ": " + e.Message);
				}
			}
		}

		private void SaveLooseObject(AnyObjectId id, byte[] compressed)
		{
			var tmp = new FileInfo(Path.Combine(_local.ObjectsDirectory.ToString(), Path.GetTempFileName()));
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
			catch (IOException)
			{
				File.Delete(tmp.ToString());
				throw;
			}

			FileInfo o = _local.ToFile(id);
			if (tmp.RenameTo(o.ToString()))
				return;

			Directory.CreateDirectory(tmp.ToString());
			if (tmp.RenameTo(o.ToString()))
				return;

			tmp.Delete();
			if (_local.HasObject(id))
				return;

			throw new ObjectWritingException("Unable to store " + id.Name + ".");
		}

		private List<WalkRemoteObjectDatabase> ExpandOneAlternate(AnyObjectId id, IProgressMonitor pm)
		{
			while (_noAlternatesYet.Count > 0)
			{
				WalkRemoteObjectDatabase wrr = _noAlternatesYet.First.Value;
				_noAlternatesYet.RemoveFirst();
				try
				{
					pm.BeginTask("Listing alternates", -1);
					List<WalkRemoteObjectDatabase> altList = wrr.getAlternates();
					if (altList != null && altList.Count > 0)
						return altList;
				}
				catch (IOException e)
				{
					RecordError(id, e);
				}
				finally
				{
					pm.EndTask();
				}
			}
			return null;
		}

		private void MarkLocalRefsComplete(IEnumerable<ObjectId> have)
		{
			foreach (Ref r in _local.getAllRefs().Values)
			{
				try
				{
					MarkLocalObjComplete(_revWalk.parseAny(r.ObjectId));
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
					MarkLocalObjComplete(_revWalk.parseAny(id));
				}
				catch (IOException readError)
				{
					throw new TransportException("Missing assumed " + id.Name, readError);
				}
			}
		}

		private void MarkLocalObjComplete(RevObject obj)
		{
			while (obj.Type == Constants.OBJ_TAG)
			{
				obj.add(COMPLETE);
				obj.DisposeBody();
				obj = ((RevTag)obj).getObject();
				_revWalk.parseHeaders(obj);
			}

			switch (obj.Type)
			{
				case Constants.OBJ_BLOB:
					obj.add(COMPLETE);
					break;

				case Constants.OBJ_COMMIT:
					PushLocalCommit((RevCommit)obj);
					break;

				case Constants.OBJ_TREE:
					MarkTreeComplete(obj);
					break;
			}
		}

		private void MarkLocalCommitsComplete(int until)
		{
			try
			{
				while (true)
				{
					RevCommit c = _localCommitQueue.peek();
					if (c == null || c.CommitTime < until) return;
					_localCommitQueue.next();

					MarkTreeComplete(c.Tree);
					foreach (RevCommit p in c.Parents)
					{
						PushLocalCommit(p);
					}
				}
			}
			catch (IOException err)
			{
				throw new TransportException("Local objects incomplete.", err);
			}
		}

		private void PushLocalCommit(RevCommit p)
		{
			if (p.has(LOCALLY_SEEN)) return;
			_revWalk.parseHeaders(p);
			p.add(LOCALLY_SEEN);
			p.add(COMPLETE);
			p.carry(COMPLETE);
			p.DisposeBody();
			_localCommitQueue.add(p);
		}

		private void MarkTreeComplete(RevObject tree)
		{
			if (tree.has(COMPLETE)) return;

			tree.add(COMPLETE);
			_treeWalk.reset(tree);
			while (_treeWalk.next())
			{
				FileMode mode = _treeWalk.getFileMode(0);
				int sType = mode.Bits;

				switch (sType)
				{
					case Constants.OBJ_BLOB:
						_treeWalk.getObjectId(_idBuffer, 0);
						_revWalk.lookupAny(_idBuffer, sType).add(COMPLETE);
						continue;

					case Constants.OBJ_TREE:
						{
							_treeWalk.getObjectId(_idBuffer, 0);
							RevObject o = _revWalk.lookupAny(_idBuffer, sType);
							if (!o.has(COMPLETE))
							{
								o.add(COMPLETE);
								_treeWalk.enterSubtree();
							}
							continue;
						}

					default:
						if (FileMode.GitLink.Equals(sType))
							continue;
						_treeWalk.getObjectId(_idBuffer, 0);
						throw new CorruptObjectException("Invalid mode " + mode + " for " + _idBuffer.Name + " " +
														 _treeWalk.getPathString() + " in " + tree.Name);
				}
			}
		}

		private void RecordError(AnyObjectId id, Exception e)
		{
			ObjectId objId = id.Copy();
			if (_fetchErrors.ContainsKey(objId))
			{
				_fetchErrors[objId].Add(e);
			}
			else
			{
				_fetchErrors.Add(objId, new List<Exception>(2) { e });
			}
		}

		#region Nested Types

		private class RemotePack
		{
			private readonly WalkRemoteObjectDatabase _connection;
			private readonly string _idxName;
			private readonly Repository _local;
			private readonly ObjectChecker _objCheck;
			private readonly string _lockMessage;
			private readonly List<PackLock> _packLocks;

			public string PackName { get; private set; }
			public FileInfo TmpIdx { get; private set; }
			public PackIndex Index { get; private set; }

			public RemotePack(string lockMessage, List<PackLock> packLocks, ObjectChecker oC, Repository r, WalkRemoteObjectDatabase c, string pn)
			{
				_lockMessage = lockMessage;
				_packLocks = packLocks;
				_objCheck = oC;
				_local = r;
				DirectoryInfo objdir = _local.ObjectsDirectory;
				_connection = c;
				PackName = pn;
				_idxName = IndexPack.GetIndexFileName(PackName.Slice(0, PackName.Length - 5));

				string tn = _idxName;
				if (tn.StartsWith("pack-"))
				{
					tn = tn.Substring(5);
				}

				if (tn.EndsWith(IndexPack.IndexSuffix))
				{
					tn = tn.Slice(0, tn.Length - 4);
				}

				TmpIdx = new FileInfo(Path.Combine(objdir.ToString(), "walk-" + tn + ".walkidx"));
			}

			public void OpenIndex(IProgressMonitor pm)
			{
				if (Index != null) return;

				try
				{
					Index = PackIndex.Open(TmpIdx);
					return;
				}
				catch (FileNotFoundException)
				{

				}

				Stream s = _connection.open("pack/" + _idxName);
				pm.BeginTask("Get " + _idxName.Slice(0, 12) + "..idx", s.Length < 0 ? -1 : (int)(s.Length / 1024));
				try
				{
					var fos = new FileStream(TmpIdx.ToString(), System.IO.FileMode.Open, FileAccess.ReadWrite);
					try
					{
						var buf = new byte[2048];
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
				catch (IOException)
				{
					TmpIdx.Delete();
					throw;
				}
				finally
				{
					s.Close();
				}
				pm.EndTask();

				if (pm.IsCancelled)
				{
					TmpIdx.Delete();
					return;
				}

				try
				{
					Index = PackIndex.Open(TmpIdx);
				}
				catch (IOException)
				{
					TmpIdx.Delete();
					throw;
				}
			}

			public void DownloadPack(IProgressMonitor monitor)
			{
				Stream s = _connection.open("pack/" + PackName);
				IndexPack ip = IndexPack.Create(_local, s);
				ip.setFixThin(false);
				ip.setObjectChecker(_objCheck);
				ip.index(monitor);
				PackLock keep = ip.renameAndOpenPack(_lockMessage);
				if (keep != null)
				{
					_packLocks.Add(keep);
				}
			}
		}

		#endregion
	}
}