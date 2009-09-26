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
using System.Collections.Generic;
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
		private static readonly PackFile[] NoPacks = { };
		private readonly DirectoryInfo _objects;
		private readonly DirectoryInfo _infoDirectory;
		private readonly DirectoryInfo _packDirectory;
		private readonly FileInfo _alternatesFile;
		private readonly AtomicReference<PackFile[]> _packList;

		private long _packDirectoryLastModified;

		/// <summary>
		/// Initialize a reference to an on-disk object directory.
		/// </summary>
		/// <param name="dir">the location of the <code>objects</code> directory.</param>
		public ObjectDirectory(DirectoryInfo dir)
		{
			_objects = dir;
			_infoDirectory = new DirectoryInfo(_objects.FullName + "/info");
			_packDirectory = new DirectoryInfo(_objects.FullName + "/pack");
			_alternatesFile = new FileInfo(_infoDirectory + "/alternates");
			_packList = new AtomicReference<PackFile[]>();
		}

		/// <summary>
		/// Gets the location of the <code>objects</code> directory.
		/// </summary>
		public DirectoryInfo getDirectory()
		{
			return _objects;
		}

		public override bool exists()
		{
			return _objects.Exists;
		}

		public override void create()
		{
			_objects.Create();
			_infoDirectory.Create();
			_packDirectory.Create();
		}

		public override void closeSelf()
		{
			PackFile[] packs = _packList.get();
			if (packs == null) return;

			_packList.set(null);
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
		public FileInfo fileFor(AnyObjectId objectId)
		{
			return fileFor(objectId.ToString());
		}

		private FileInfo fileFor(string objectName)
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
		public void openPack(FileInfo pack, FileInfo idx)
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
			return "ObjectDirectory[" + getDirectory() + "]";
		}

		public override bool hasObject1(AnyObjectId objectId)
		{
			foreach (PackFile p in Packs())
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
					// The hasObject call should have only touched the index,
					// so any failure here indicates the index is unreadable
					// by this process, and the pack is likewise not readable.
					//
					RemovePack(p);
					continue;
				}
			}

			return false;
		}

		public override ObjectLoader openObject1(WindowCursor curs, AnyObjectId objectId)
		{
			PackFile[] pList = Packs();

			while (true)
			{
                SEARCH:
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
						goto SEARCH;
					}
					catch (IOException)
					{
						// Assume the pack is corrupted.
						//
						RemovePack(p);
					}
				}

				return null;
			}
		}

		public override void OpenObjectInAllPacksImplementation(ICollection<PackedObjectLoader> @out, WindowCursor windowCursor, AnyObjectId objectId)
		{
			PackFile[] pList = Packs();
			while (true)
			{
                SEARCH:
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
						goto SEARCH;
					}
					catch (IOException)
					{
						// Assume the pack is corrupted.
						//
						RemovePack(p);
					}
				}

				break;
			}
		}

		public override bool hasObject2(string objectName)
		{
			return fileFor(objectName).Exists;
		}

		public override ObjectLoader openObject2(WindowCursor curs, string objectName, AnyObjectId objectId)
		{
			try
			{
				return new UnpackedObjectLoader(fileFor(objectName), objectId);
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

		public override bool tryAgain1()
		{
			PackFile[] old = _packList.get();
            _packDirectory.Refresh();
            if (_packDirectoryLastModified < _packDirectory.LastAccessTime.Ticks)
			{
				ScanPacks(old);
				return true;
			}

			return false;
		}

		private void InsertPack(PackFile pf)
		{
			PackFile[] o, n;
			do
			{
				o = Packs();
				n = new PackFile[1 + o.Length];
				n[0] = pf;
				Array.Copy(o, 0, n, 1, o.Length);
			} while (!_packList.compareAndSet(o, n));
		}

		private void RemovePack(PackFile deadPack)
		{
			PackFile[] o, n;
			do
			{
				o = _packList.get();
				if (o == null || !InList(o, deadPack))
				{
					break;
				}

				if (o.Length == 1)
				{
					n = NoPacks;
				}
				else
				{
					n = new PackFile[o.Length - 1];
					int j = 0;
					foreach (PackFile p in o)
					{
						if (p != deadPack)
						{
							n[j++] = p;
						}
					}
				}
			} while (!_packList.compareAndSet(o, n));
			deadPack.Close();
		}

		private static bool InList(IEnumerable<PackFile> list, PackFile pack)
		{
			return list != null && list.Contains(pack);
		}

		private PackFile[] Packs()
		{
			PackFile[] packFiles = _packList.get() ?? ScanPacks(null);
			return packFiles;
		}

		private PackFile[] ScanPacks(PackFile[] original)
		{
			lock (_packList)
			{
				PackFile[] o, n;
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
					n = ScanPacksImpl(o ?? NoPacks);

				} while (!_packList.compareAndSet(o, n));

				return n;
			}
		}

		private PackFile[] ScanPacksImpl(IEnumerable<PackFile> old)
		{
			Dictionary<string, PackFile> forReuse = ReuseMap(old);
			string[] idxList = ListPackIdx();
			var list = new List<PackFile>(idxList.Length);
			foreach (string indexName in idxList)
			{
				string @base = indexName.Slice(0, indexName.Length - 4);
				string packName = IndexPack.GetPackFileName(@base);

				PackFile oldPack;
				forReuse.TryGetValue(packName, out oldPack);
				forReuse.Remove(packName);
				if (oldPack != null)
				{
					list.Add(oldPack);
					continue;
				}

				var packFile = new FileInfo(_packDirectory.FullName + "/" + packName);
				if (!packFile.Exists)
				{
					// Sometimes C Git's HTTP fetch transport leaves a
					// .idx file behind and does not download the .pack.
					// We have to skip over such useless indexes.
					//
					continue;
				}

				var idxFile = new FileInfo(_packDirectory + "/" + indexName);
				list.Add(new PackFile(idxFile, packFile));
			}

			foreach (PackFile p in forReuse.Values)
			{
				p.Close();
			}

			if (list.Count == 0)
			{
				return NoPacks;
			}

			PackFile[] r = list.ToArray();
			Array.Sort(r, PackFile.PackFileSortComparison);
			return r;
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

		private string[] ListPackIdx()
		{
            _packDirectoryLastModified = _packDirectory.LastAccessTime.Ticks;
			// Must match "pack-[0-9a-f]{40}.idx" to be an index.

			string[] idxList = _packDirectory.GetFiles()
				.Select(file => file.Name)
				.Where(n => n.Length == 49 && n.EndsWith(IndexPack.IndexSuffix) && n.StartsWith("pack-"))
				.ToArray();

			return idxList;  // idxList != null ? idxList : "";
		}

		public override ObjectDatabase[] loadAlternates()
		{
			StreamReader br = Open(_alternatesFile);
			var l = new List<ObjectDirectory>(4);
			try
			{
				string line;
				while ((line = br.ReadLine()) != null)
				{
					l.Add(new ObjectDirectory((DirectoryInfo)FS.resolve(_objects, line)));
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
	}
}