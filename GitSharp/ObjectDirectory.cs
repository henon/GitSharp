/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GitSharp.Exceptions;
using GitSharp.Transport;
using GitSharp.Util;

namespace GitSharp
{
	/// <summary>
	/// Traditional file system based <see cref="ObjectDatabase"/>.
	/// <para />
	/// This is the classical object database representation for a Git repository,
	/// where objects are stored loose by hashing them into directories by their
	/// <see cref="ObjectId"/>, or are stored in compressed containers known as
	/// <see cref="PackFile"/>s.
	/// </summary>
	public class ObjectDirectory : ObjectDatabase
	{
		private static readonly PackList NO_PACKS = new PackList(-1, -1, new PackFile[0]);
		private readonly DirectoryInfo _objects;
		private readonly DirectoryInfo _infoDirectory;
		private readonly DirectoryInfo _packDirectory;
		private readonly FileInfo _alternatesFile;
		private readonly AtomicReference<PackList> _packList;

		/// <summary>
		/// Initialize a reference to an on-disk object directory.
		/// </summary>
		/// <param name="dir">the location of the <code>objects</code> directory.</param>
		public ObjectDirectory(DirectoryInfo dir)
		{
			_objects = dir;
			_infoDirectory = new DirectoryInfo(Path.Combine(_objects.FullName, "info"));
			_packDirectory = new DirectoryInfo(Path.Combine(_objects.FullName, "pack"));
			_alternatesFile = new FileInfo(Path.Combine(_infoDirectory.FullName, "alternates"));
			_packList = new AtomicReference<PackList>();
		}

		/// <summary>
		/// Gets the location of the <code>objects</code> directory.
		/// </summary>
		public DirectoryInfo Directory()
		{
			return _objects;
		}

		public override bool Exists()
		{
			return _objects.Exists;
		}

		public override void Create()
		{
			_objects.Create();
			_infoDirectory.Create();
			_packDirectory.Create();
		}

		public override void CloseSelf()
		{
			PackList packs = _packList.get();

			_packList.set(NO_PACKS);

			if (packs == null) return;

			foreach (PackFile p in packs)
			{
				p.Close();
			}
		}

		/// <summary>
		/// Compute the location of a loose object file.
		/// </summary>
		/// <param name="objectId">Identity of the loose object to map to the directory.</param>
		/// <returns>Location of the object, if it were to exist as a loose object.</returns>
		public FileInfo FileFor(AnyObjectId objectId)
		{
			return FileFor(objectId.Name);
		}

		private FileInfo FileFor(string objectName)
		{
			string d = objectName.Slice(0, 2);
			string f = objectName.Substring(2);
			return new FileInfo(_objects.FullName + "/" + d + "/" + f);
		}

		/// <summary>
		/// Add a single existing pack to the list of available pack files.
		/// </summary>
		/// <param name="pack">Path of the pack file to open.</param>
		/// <param name="idx">Path of the corresponding index file.</param>
		///	<exception cref="IOException">
		/// Index file could not be opened, read, or is not recognized as
		/// a Git pack file index.
		/// </exception>
		public void OpenPack(FileInfo pack, FileInfo idx)
		{
			string p = pack.Name;
			string i = idx.Name;

			if (p.Length != 50 || !p.StartsWith("pack-") || !p.EndsWith(IndexPack.PackSuffix))
			{
				throw new IOException("Not a valid pack " + pack);
			}

			if (i.Length != 49 || !i.StartsWith("pack-") || !i.EndsWith(IndexPack.IndexSuffix))
			{
				throw new IOException("Not a valid pack " + idx);
			}

			if (!p.Slice(0, 45).Equals(i.Slice(0, 45)))
			{
				throw new IOException("Pack " + pack + "does not match index");
			}

			InsertPack(new PackFile(idx, pack));
		}

		public override string ToString()
		{
			return "ObjectDirectory[" + Directory() + "]";
		}

		protected internal override bool HasObject1(AnyObjectId objectId)
		{
			foreach (PackFile p in _packList.get())
			{
				try
				{
					if (p.HasObject(objectId))
					{
						return true;
					}
				}
				catch (IOException)
				{
					// The HasObject call should have only touched the index,
					// so any failure here indicates the index is unreadable
					// by this process, and the pack is likewise not readable.
					//
					RemovePack(p);
					continue;
				}
			}

			return false;
		}

		protected internal override ObjectLoader OpenObject1(WindowCursor curs, AnyObjectId objectId)
		{
			PackList pList = _packList.get();

			while (true)
			{
				bool breakLoop = false;

				if (pList == null) break;

				foreach (PackFile p in pList)
				{
					try
					{
						PackedObjectLoader ldr = p.Get(curs, objectId);
						if (ldr != null)
						{
							ldr.Materialize(curs);
							return ldr;
						}
					}
					catch (PackMismatchException)
					{
						// Pack was modified; refresh the entire pack list.
						//
						pList = ScanPacks(pList);
						breakLoop = true;
					}
					catch (IOException)
					{
						// Assume the pack is corrupted.
						//
						RemovePack(p);
					}

					if (breakLoop) break;
				}

				if (breakLoop) continue;

				return null;
			}

			return null;
		}

		public override void OpenObjectInAllPacks1(ICollection<PackedObjectLoader> @out, WindowCursor windowCursor, AnyObjectId objectId)
		{
			PackList pList = _packList.get();

			while (true)
			{
				bool breakLoop = false;

				foreach (PackFile p in pList)
				{
					try
					{
						PackedObjectLoader ldr = p.Get(windowCursor, objectId);
						if (ldr != null)
						{
							@out.Add(ldr);
						}
					}
					catch (PackMismatchException)
					{
						// Pack was modified; refresh the entire pack list.
						//
						pList = ScanPacks(pList);
						breakLoop = true;
					}
					catch (IOException)
					{
						// Assume the pack is corrupted.
						//
						RemovePack(p);
					}

					if (breakLoop) break;
				}
			}
		}

		protected internal override bool HasObject2(string objectName)
		{
			return FileFor(objectName).Exists;
		}

		protected internal override ObjectLoader OpenObject2(WindowCursor curs, string objectName, AnyObjectId objectId)
		{
			try
			{
				return new UnpackedObjectLoader(FileFor(objectName), objectId);
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			catch (DirectoryNotFoundException)
			{
				return null;
			}
		}

		protected internal override bool TryAgain1()
		{
			PackList old = _packList.get();

			if (/*old == null && */old.TryAgain(_packDirectory.LastWriteTime.Ticks))
			{
				return old != ScanPacks(old);
			}

			return false;
		}

		private void InsertPack(PackFile pf)
		{
			PackList o, n;
			do
			{
				o = _packList.get();
				PackFile[] oldList = o.Packs;
				var newList = new PackFile[1 + oldList.Length];
				newList[0] = pf;
				Array.Copy(oldList, 0, newList, 1, oldList.Length);
				n = new PackList(o.LastRead, o.LastModified, newList);
			} while (!_packList.compareAndSet(o, n));
		}

		private void RemovePack(PackFile deadPack)
		{
			PackList o, n;
			do
			{
				o = _packList.get();

				var oldList = o.Packs;
				int j = IndexOf(oldList, deadPack);
				if (j < 0) break;

				var newList = new PackFile[oldList.Length - 1];
				Array.Copy(oldList, 0, newList, 0, j);
				Array.Copy(oldList, j + 1, newList, j, newList.Length - j);
				n = new PackList(o.LastRead, o.LastModified, newList);
			} while (!_packList.compareAndSet(o, n));

			deadPack.Close();
		}

		private static int IndexOf(IEnumerable<PackFile> list, PackFile pack)
		{
			if (list == null) return -1;
			return list.ToList().IndexOf(pack);
		}

		private PackList ScanPacks(PackList original)
		{
			lock (_packList)
			{
				PackList o, n;
				do
				{
					o = _packList.get();
					if (o != original)
					{
						// Another thread did the scan for us, while we
						// were blocked on the monitor above.
						//
						return o;
					}
					n = ScanPacksImpl(o);
					if (n == o)
					{
						return n;
					}
				} while (!_packList.compareAndSet(o, n));

				return n;
			}
		}

		private PackList ScanPacksImpl(PackList old)
		{
			IDictionary<string, PackFile> forReuse = ReuseMap(old);
			var names = ListPackDirectory();
			var list = new List<PackFile>(names.Length >> 2);

			long lastRead = DateTime.Now.Ticks;
			long lastModified = _packDirectory.LastWriteTime.Ticks;

			bool foundNew = false;

			foreach (string indexName in names)
			{
				// Must match "pack-[0-9a-f]{40}.idx" to be an index.
				//
				if (indexName.Length != 49 || !indexName.EndsWith(".idx"))
				{
					continue;
				}

				string basePackName = indexName.Slice(0, indexName.Length - 4);
				string packName = basePackName + IndexPack.PackSuffix;

				if (!names.Contains(packName))
				{
					// Sometimes C Git's HTTP fetch transport leaves a
					// .idx file behind and does not download the .pack.
					// We have to skip over such useless indexes.
					//
					continue;
				}

				PackFile oldPack;
				if (forReuse.TryGetValue(packName, out oldPack))
				{
					list.Add(oldPack);
					continue;
				}

				var packFile = new FileInfo(Path.Combine(_packDirectory.FullName, packName));
				var idxFile = new FileInfo(Path.Combine(_packDirectory.FullName, indexName));
				list.Add(new PackFile(idxFile, packFile));
				foundNew = true;
			}

			// If we did not discover any new files, the modification time was not
			// changed, and we did not remove any files, then the set of files is
			// the same as the set we were given. Instead of building a new object
			// return the same collection.
			//
			if (!foundNew && lastModified == old.LastModified && forReuse.isEmpty())
			{
				return old.UpdateLastRead(lastRead);
			}

			foreach (PackFile p in forReuse.Values)
			{
				p.Close();
			}

			if (list.Count == 0)
			{
				return new PackList(lastRead, lastModified, NO_PACKS.Packs);
			}

			PackFile[] r = list.ToArray();
			Array.Sort(r, PackFile.PackFileSortComparison);
			return new PackList(lastRead, lastModified, r);
		}

		private static Dictionary<string, PackFile> ReuseMap(IEnumerable<PackFile> old)
		{
			var forReuse = new Dictionary<string, PackFile>();
			foreach (PackFile p in old)
			{
				if (p.IsInvalid)
				{
					// The pack instance is corrupted, and cannot be safely used
					// again. Do not include it in our reuse map.
					//
					p.Close();
					continue;
				}

				PackFile prior = forReuse[p.File.Name] = p;
				if (prior != null)
				{
					// This should never occur. It should be impossible for us
					// to have two pack files with the same name, as all of them
					// came out of the same directory. If it does, we promised to
					// close any PackFiles we did not reuse, so close the one we
					// just evicted out of the reuse map.
					//
					prior.Close();
				}
			}

			return forReuse;
		}

		private string[] ListPackDirectory()
		{
			string[] nameList = _packDirectory.GetFiles()
				.Select(file => file.Name)
				.Where(n => n.Length == 49 && n.EndsWith(IndexPack.IndexSuffix) && n.StartsWith("pack-"))
				.ToArray();

			return nameList;
		}

		protected override ObjectDatabase[] LoadAlternates()
		{
			StreamReader br = Open(_alternatesFile);
			var l = new List<ObjectDatabase>(4);
			try
			{
				string line;
				while ((line = br.ReadLine()) != null)
				{
					l.Add(OpenAlternate(line));
				}
			}
			finally
			{
				br.Close();
			}

			return l.Count == 0 ? NoAlternates : l.ToArray();
		}

		private static StreamReader Open(FileSystemInfo f)
		{
			return new StreamReader(new FileStream(f.FullName, System.IO.FileMode.Open));
		}

		private ObjectDatabase OpenAlternate(string location)
		{
			var objdir = FS.resolve(_objects, location);
			DirectoryInfo parent = objdir.Parent;

			if (RepositoryCache.FileKey.isGitRepository(parent))
			{
				Repository db = RepositoryCache.open(RepositoryCache.FileKey.exact(parent));
				return new AlternateRepositoryDatabase(db);
			}

			return new ObjectDirectory(objdir);
		}

		#region Nested Types

		private class PackList : IEnumerable<PackFile>
		{
			// Last wall-clock time the directory was read.
			private long _lastRead;

			// Last modification time of ObjectDirectory.PackDirectory
			private readonly long _lastModified;

			// All known packs, sorted by PackFile.SORT.
			private readonly PackFile[] _packs;

			private bool _cannotBeRacilyClean;

			public PackList(long lastRead, long lastModified, PackFile[] packs)
			{
				_lastRead = lastRead;
				_lastModified = lastModified;
				_packs = packs;
				_cannotBeRacilyClean = NotRacyClean(lastRead);
			}

			private bool NotRacyClean(long read)
			{
				return read - _lastModified > 2 * 60 * 1000L;
			}

			public PackList UpdateLastRead(long now)
			{
				if (NotRacyClean(now))
				{
					_cannotBeRacilyClean = true;
				}

				_lastRead = now;
				return this;
			}

			public bool TryAgain(long currLastModified)
			{
				// Any difference indicates the directory was modified.
				//
				if (_lastModified != currLastModified) return true;

				// We have already determined the last read was far enough
				// after the last modification that any new modifications
				// are certain to change the last modified time.
				//
				if (_cannotBeRacilyClean) return false;

				if (NotRacyClean(_lastRead))
				{
					// Our last read should have marked cannotBeRacilyClean,
					// but this thread may not have seen the change. The read
					// of the volatile field lastRead should have fixed that.
					//
					return false;
				}

				// We last read this directory too close to its last observed
				// modification time. We may have missed a modification. Scan
				// the directory again, to ensure we still see the same state.
				//
				return true;
			}

			public PackFile[] Packs
			{
				[DebuggerStepThrough]
				get { return _packs; }
			}

			public long LastRead
			{
				[DebuggerStepThrough]
				get { return _lastRead; }
			}

			public long LastModified
			{
				[DebuggerStepThrough]
				get { return _lastModified; }
			}

			#region Implementation of IEnumerable

			/// <summary>
			/// Returns an enumerator that iterates through the collection.
			/// </summary>
			/// <returns>
			/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
			/// </returns>
			/// <filterpriority>1</filterpriority>
			public IEnumerator<PackFile> GetEnumerator()
			{
				return _packs.AsEnumerable().GetEnumerator();
			}

			/// <summary>
			/// Returns an enumerator that iterates through a collection.
			/// </summary>
			/// <returns>
			/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
			/// </returns>
			/// <filterpriority>2</filterpriority>
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			#endregion
		}

		#endregion
	}
}