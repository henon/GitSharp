/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using GitSharp.Core.Util;
using GitSharp.Core.Exceptions;
using System.Runtime.CompilerServices;

namespace GitSharp.Core
{
	public class RefDatabase
	{
		private readonly DirectoryInfo _gitDir;
		private readonly DirectoryInfo _refsDir;
		private readonly FileInfo _packedRefsFile;

		private Dictionary<string, Ref> _looseRefs;
		private Dictionary<string, DateTime> _looseRefsMTime;
		private Dictionary<string, Ref> _packedRefs;
		private Dictionary<string, string> _looseSymRefs;

		private DateTime _packedRefsLastModified;
		private long _packedRefsLength;
		private int _refModificationCounter;
		
		private Object locker = new Object();

		public RefDatabase(Repository repo)
		{
			Repository = repo;
			_gitDir = repo.Directory;
			_refsDir = PathUtil.CombineDirectoryPath(_gitDir, "refs");
			_packedRefsFile = PathUtil.CombineFilePath(_gitDir, "packed-refs");
			ClearCache();
		}

		public Repository Repository { get; private set; }
		public int LastRefModification { get; private set; }
		public int LastNotifiedRefModification { get; set; }

		public void ClearCache()
		{
			lock(locker)
			{
				_looseRefs = new Dictionary<string, Ref>();
				_looseRefsMTime = new Dictionary<string, DateTime>();
				_looseSymRefs = new Dictionary<string, string>();
				_packedRefs = new Dictionary<string, Ref>();
				_packedRefsLastModified = DateTime.MinValue;
				_packedRefsLength = 0;
			}
		}

		public void Create()
		{
			_refsDir.Create();
			PathUtil.CombineDirectoryPath(_refsDir, "heads").Create();
			PathUtil.CombineDirectoryPath(_refsDir, "tags").Create();
		}

		public ObjectId IdOf(string name)
		{
			RefreshPackedRefs();
			Ref r = ReadRefBasic(name, 0);
			return (r != null) ? r.ObjectId : null;
		}

		/// <summary>
		/// Create a command to update, create or delete a ref in this repository.
		/// </summary>
		/// <param name="name">
		/// name of the ref the caller wants to modify.
		/// </param>
		/// <returns>
		/// An update command. The caller must finish populating this command 
		/// and then invoke one of the update methods to actually make a change.
		/// </returns>
		/// <exception cref="IOException">
		/// A symbolic ref was passed in and could not be resolved back
		/// to the base ref, as the symbolic ref could not be read.
		/// </exception>
		public RefUpdate NewUpdate(string name)
		{
			RefreshPackedRefs();
			Ref r = ReadRefBasic(name, 0) ?? new Ref(Ref.Storage.New, name, null);
			return new RefUpdate(this, r, FileForRef(r.Name));
		}

		internal void Stored(string origName, string name, ObjectId id, DateTime time)
		{
			lock (locker)
			{
				_looseRefs[name] = new Ref(Ref.Storage.Loose, name, name, id);
				_looseRefsMTime[name] = time;
				SetModified();
			}
			Repository.fireRefsMaybeChanged();
		}

		///	<summary>
		/// An set of update operations for renaming a ref
		///	</summary>
		///	<param name="fromRef"> Old ref name </param>
		///	<param name="toRef"> New ref name </param>
		///	<returns> a RefUpdate operation to rename a ref </returns>
		///	<exception cref="IOException"> </exception>
		public RefRename NewRename(string fromRef, string toRef)
		{
			RefreshPackedRefs();
			Ref f = ReadRefBasic(fromRef, 0);
			var t = new Ref(Ref.Storage.New, toRef, null);
			var refUpdateFrom = new RefUpdate(this, f, FileForRef(f.Name));
			var refUpdateTo = new RefUpdate(this, t, FileForRef(t.Name));
			return new RefRename(refUpdateTo, refUpdateFrom);
		}

		/// <summary>
		/// Writes a symref (e.g. HEAD) to disk
		/// * @param name
		/// </summary>
		/// <param name="name">symref name</param>
		/// <param name="target">pointed to ref</param>
		public void Link(string name, string target)
		{
			lock(locker)
			{
				byte[] content = Constants.encode("ref: " + target + "\n");
				LockAndWriteFile(FileForRef(name), content);
				UncacheSymRef(name);
				Repository.fireRefsMaybeChanged();
			}
		}

		private void UncacheSymRef(string name)
		{
			lock (locker)
			{
				_looseSymRefs.Remove(name);
				SetModified();
			}
		}

		private void UncacheRef(string name)
		{
			_looseRefs.Remove(name);
			_looseRefsMTime.Remove(name);
			_packedRefs.Remove(name);
		}

		private void SetModified()
		{
			LastRefModification = _refModificationCounter++;
		}

		public Ref ReadRef(string partialName)
		{
			RefreshPackedRefs();
			foreach (var searchPath in Constants.RefSearchPaths)
			{
				Ref r = ReadRefBasic(searchPath + partialName, 0);
				if (r != null && r.ObjectId != null)
				{
					return r;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets all known refs (heads, tags, remotes).
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, Ref> GetAllRefs()
		{
			return ReadRefs();
		}

		/// <summary>
		/// Gets all tags; key is short tag name ("v1.0") and value of the entry
		/// contains the ref with the full tag name ("refs/tags/v1.0").
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, Ref> GetTags()
		{
			var tags = new Dictionary<string, Ref>();
			foreach (Ref r in ReadRefs().Values)
			{
				if (r.Name.StartsWith(Constants.RefsTags))
				{
					tags.Add(r.Name.Substring(Constants.RefsTags.Length), r);
				}
			}
			return tags;
		}

		private Dictionary<string, Ref> ReadRefs()
		{
			var avail = new Dictionary<string, Ref>();
			ReadPackedRefs(avail);
			ReadLooseRefs(avail, Constants.Refs, _refsDir);
			try
			{
				Ref r = ReadRefBasic(Constants.HEAD, 0);
				if (r != null && r.ObjectId != null)
				{
					avail[Constants.HEAD] = r;
				}
			}
			catch (IOException)
			{
				// ignore here
			}

			Repository.fireRefsMaybeChanged();
			return avail;
		}

		private void ReadPackedRefs(ICollection<KeyValuePair<string, Ref>> avail)
		{
			lock(locker)
			{
				RefreshPackedRefs();
				foreach (KeyValuePair<string, Ref> kv in _packedRefs)
				{
					avail.Add(kv);
				}
			}
		}

		private void ReadLooseRefs(IDictionary<string, Ref> avail, string prefix, DirectoryInfo dir)
		{
			var entries = dir.GetFileSystemInfos();
			if (entries.Length == 0) return;

			foreach (FileSystemInfo ent in entries)
			{
				string entName = ent.Name;
				if (".".Equals(entName) || "..".Equals(entName)) continue;

				if (ent.IsDirectory())
				{
					ReadLooseRefs(avail, prefix + entName + "/", ent as DirectoryInfo);
				}
				else
				{
					try
					{
						Ref reference = ReadRefBasic(prefix + entName, 0);
						if (reference != null)
						{
							avail[reference.OriginalName] = reference;
						}
					}
					catch (IOException)
					{
						continue;
					}
				}

			}

		}

		/// <summary>
		/// Returns the object that this object points to if this is a commit.
		/// </summary>
		/// <param name="dref">The ref.</param>
		/// <returns></returns>
		internal Ref Peel(Ref dref)
		{
			if (dref.Peeled) return dref;

			ObjectId peeled = null;
			try
			{
				Tag target = (Repository.MapObject(dref.ObjectId, dref.Name) as Tag);

				while (target != null)
				{
					peeled = target.Id;

					if (target.TagType == Constants.TYPE_TAG)
					{
						target = (Repository.MapObject(target.Id, dref.Name) as Tag);
					}
					else
					{
						break;
					}
				}
			}
			catch (IOException)
			{
				// Ignore a read error.  Callers will also get the same error
				// if they try to use the result of getPeeledObjectId.
			}
			return new Ref(dref.StorageFormat, dref.Name, dref.ObjectId, peeled, true);
		}

		private FileInfo FileForRef(string name)
		{
			string fileName = name.StartsWith(Constants.Refs) ?
				Path.Combine(_refsDir.FullName, name.Substring(Constants.Refs.Length)) :
				Path.Combine(_gitDir.FullName, name);

			return new FileInfo(fileName.Replace('/', Path.DirectorySeparatorChar));
		}

		private Ref ReadRefBasic(string name, int depth)
		{
			return ReadRefBasic(name, name, depth);
		}

		private Ref ReadRefBasic(String origName, string name, int depth)
		{
			// Prefer loose ref to packed ref as the loose
			// file can be more up-to-date than a packed one.
			//
			Ref @ref;
			_looseRefs.TryGetValue(name, out @ref);
			FileInfo loose = FileForRef(name);
			loose.Refresh();
			DateTime mtime = loose.Exists ? loose.LastWriteTime : DateTime.MinValue;	// [ammachado] If the file does not exists, LastWriteTimes returns '1600-12-31 22:00:00'

			if (@ref != null)
			{
				DateTime cachedLastModified;
				if (_looseRefsMTime.TryGetValue(name, out cachedLastModified) && cachedLastModified == mtime)
				{
					return _packedRefs.ContainsKey(origName) ?
						new Ref(Ref.Storage.LoosePacked, origName, @ref.ObjectId, @ref.PeeledObjectId, @ref.Peeled)
						: @ref;
				}

				_looseRefs.Remove(origName);
				_looseRefsMTime.Remove(origName);
			}

			if (!loose.Exists)
			{
				// File does not exist.
				// Try packed cache.
				//
				_packedRefs.TryGetValue(name, out @ref);
				if (@ref != null && !@ref.OriginalName.Equals(origName))
				{
					@ref = new Ref(Ref.Storage.LoosePacked, origName, name, @ref.ObjectId);
				}
				return @ref;
			}

			string line = null;
			try
			{
				DateTime cachedLastModified;
				if (_looseRefsMTime.TryGetValue(name, out cachedLastModified) && cachedLastModified == mtime)
				{
					_looseSymRefs.TryGetValue(name, out line);
				}

				if (string.IsNullOrEmpty(line))
				{
					line = ReadLine(loose);
					_looseRefsMTime[name] = mtime;
					_looseSymRefs[name] = line;
				}
			}
			catch (FileNotFoundException)
			{
				return _packedRefs[name];
			}

			if (string.IsNullOrEmpty(line))
			{
				_looseRefs.Remove(origName);
				_looseRefsMTime.Remove(origName);
				return new Ref(Ref.Storage.Loose, origName, name, null);
			}

			if (line.StartsWith("ref: "))
			{
				if (depth >= 5)
				{
					throw new IOException("Exceeded maximum ref depth of " + depth + " at " + name + ".  Circular reference?");
				}

				string target = line.Substring("ref: ".Length);
				Ref r = ReadRefBasic(target, depth + 1);
				DateTime cachedMtime;
				if (_looseRefsMTime.TryGetValue(name, out cachedMtime) && cachedMtime != mtime)
				{
					SetModified();
				}
				_looseRefsMTime[name] = mtime;

				if (r == null)
				{
					return new Ref(Ref.Storage.Loose, origName, target, null);
				}

				if (!origName.Equals(r.Name))
				{
					r = new Ref(Ref.Storage.LoosePacked, origName, r.Name, r.ObjectId, r.PeeledObjectId, true);
				}

				return r;
			}

			SetModified();

			ObjectId id;
			try
			{
				id = ObjectId.FromString(line);
			}
			catch (ArgumentException)
			{
				throw new IOException("Not a ref: " + name + ": " + line);
			}

			Ref.Storage storage = _packedRefs.ContainsKey(name) ? Ref.Storage.LoosePacked : Ref.Storage.Loose;
			@ref = new Ref(storage, name, id);
			_looseRefs[name] = @ref;
			_looseRefsMTime[name] = mtime;

			if (!origName.Equals(name))
			{
				@ref = new Ref(Ref.Storage.Loose, origName, name, id);
				_looseRefs[origName] = @ref;
			}

			return @ref;
		}

		private void RefreshPackedRefs()
		{
			lock(locker)
			{
				_packedRefsFile.Refresh();
				if (!_packedRefsFile.Exists) return;
	
				DateTime currTime = _packedRefsFile.LastWriteTime;
				long currLen = currTime == DateTime.MinValue ? 0 : _packedRefsFile.Length;
				if (currTime == _packedRefsLastModified && currLen == _packedRefsLength) return;
	
				if (currTime == DateTime.MinValue)
				{
					_packedRefsLastModified = DateTime.MinValue;
					_packedRefsLength = 0;
					_packedRefs = new Dictionary<string, Ref>();
					return;
				}
	
				var newPackedRefs = new Dictionary<string, Ref>();
				try
				{
					using (var b = OpenReader(_packedRefsFile))
					{
						string p;
						Ref last = null;
						while ((p = b.ReadLine()) != null)
						{
							if (p[0] == '#') continue;
	
							if (p[0] == '^')
							{
								if (last == null)
								{
									throw new IOException("Peeled line before ref.");
								}
	
								ObjectId id = ObjectId.FromString(p.Substring(1));
								last = new Ref(Ref.Storage.Packed, last.Name, last.Name, last.ObjectId, id, true);
								newPackedRefs.put(last.Name,  last); 
								continue;
							}
	
							int sp = p.IndexOf(' ');
							ObjectId id2 = ObjectId.FromString(p.Slice(0, sp));
							string name = p.Substring(sp + 1);
							last = new Ref(Ref.Storage.Packed, name, name, id2);
							newPackedRefs.Add(last.Name, last);
						}
					}
	
					_packedRefsLastModified = currTime;
					_packedRefsLength = currLen;
					_packedRefs = newPackedRefs;
					SetModified();
				}
				catch (FileNotFoundException)
				{
					// Ignore it and leave the new map empty.
					//
					_packedRefsLastModified = DateTime.MinValue;
					_packedRefsLength = 0;
					_packedRefs = newPackedRefs;
				}
				catch (IOException e)
				{
					throw new GitException("Cannot read packed refs", e);
				}
			}
		}

		private static void LockAndWriteFile(FileInfo file, byte[] content)
		{
			string name = file.Name;
			var lck = new LockFile(file);
			if (!lck.Lock())
			{
				throw new ObjectWritingException("Unable to lock " + name);
			}

			try
			{
				lck.Write(content);
			}
			catch (IOException ioe)
			{
				throw new ObjectWritingException("Unable to write " + name, ioe);
			}
			if (!lck.Commit())
			{
				throw new ObjectWritingException("Unable to write " + name);
			}
		}

		public void RemovePackedRef(String name)
		{
			lock(locker)
			{
				_packedRefs.Remove(name);
				WritePackedRefs();
			}
		}

		private void WritePackedRefs()
		{
			new ExtendedRefWriter(_packedRefs.Values, this).writePackedRefs();
		}

	    private static string ReadLine(FileInfo file)
	    {
	        byte[] buf = IO.ReadFully(file, 4096);
	        int n = buf.Length;
            
	        // remove trailing whitespaces
	        while (n > 0 && char.IsWhiteSpace((char)buf[n - 1]))
	            n--;

            if (n == 0)
                return null;

	        return RawParseUtils.decode(buf, 0, n);
	    }

	    private static StreamReader OpenReader(FileSystemInfo file)
		{
			return new StreamReader(file.FullName);
		}

		public Dictionary<string, Ref> GetBranches()
		{
			var branches = new Dictionary<string, Ref>();
			foreach (Ref r in ReadRefs().Values)
			{
				if (r.Name.StartsWith(Constants.RefsHeads))
				{
					branches[r.Name.Substring(Constants.RefsTags.Length)]=r; // [henon] it may happen, for some reason, that the same branch is added twice. In this case we better silently overwrite instead of throwing.
				}
			}
			return branches;
		}

		public Dictionary<string, Ref> GetRemotes()
		{
			var remotes = new Dictionary<string, Ref>();
			foreach (Ref r in ReadRefs().Values)
			{
				if (r.Name.StartsWith(Constants.RefsRemotes))
				{
					remotes[r.Name.Substring(Constants.RefsRemotes.Length)]=r; // [henon] same here.
				}
			}
			return remotes;
		}

		#region Nested Types

		private class ExtendedRefWriter : RefWriter
		{
			private readonly RefDatabase _refDb;

			public ExtendedRefWriter(IEnumerable<Ref> refs, RefDatabase db)
				: base(refs)
			{
				_refDb = db;
			}

			protected override void writeFile(string name, byte[] content)
			{
				LockAndWriteFile(new FileInfo(_refDb.Repository.Directory + "/" + name), content);
			}
		}

		private class CachedRef : Ref
		{
			public DateTime LastModified { get; private set; }

			public CachedRef(Storage st, string refName, ObjectId id, DateTime mtime)
				: base(st, refName, id)
			{
				LastModified = mtime;
			}
		}

		#endregion
	}
}