/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Threading;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp
{
	/// <summary>
	/// Represents a Git repository. A repository holds all objects and refs used for
	/// managing source code (could by any type of file, but source code is what
	/// SCM's are typically used for).
	/// <para />
	/// In Git terms all data is stored in GIT_DIR, typically a directory called
	/// .git. A work tree is maintained unless the repository is a bare repository.
	/// Typically the .git directory is located at the root of the work dir.
	/// <ul>
	/// <li>GIT_DIR
	/// 	<ul>
	/// 		<li>objects/ - objects</li>
	/// 		<li>refs/ - tags and heads</li>
	/// 		<li>config - configuration</li>
	/// 		<li>info/ - more configurations</li>
	/// 	</ul>
	/// </li>
	/// </ul>
	/// <para />
	/// This class is thread-safe.
	/// <para />
	/// This implementation only handles a subtly undocumented subset of git features.
	/// </summary>
	public class Repository
	{
		private readonly RefDatabase _refDb;
		private readonly List<DirectoryInfo> _objectsDirs;
		private readonly ObjectDirectory _objectDatabase;

		private int _useCnt;
		private GitIndex _index;

		/// <summary>
		/// Initializes a new instance of the <see cref="Repository"/> class.
		/// Assumes parent directory is the working directory.
		/// </summary>
		/// <param name="gitDirectory">The git directory.</param>
		public Repository(DirectoryInfo gitDirectory)
			: this(gitDirectory, gitDirectory.Parent)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Repository"/> class.
		/// </summary>
		/// <param name="gitDirectory">The git directory.</param>
		/// <param name="workingDirectory">The working directory.</param>
		private Repository(DirectoryInfo gitDirectory, DirectoryInfo workingDirectory)
		{
			_useCnt = 1;
			_objectsDirs = new List<DirectoryInfo>();

			Directory = gitDirectory;
			WorkingDirectory = workingDirectory;
			_objectDatabase = new ObjectDirectory(FS.resolve(gitDirectory, "objects"));
			_objectsDirs = new List<DirectoryInfo>();
			_objectsDirs = ReadObjectsDirs(Path.Combine(gitDirectory.FullName, "objects"), ref _objectsDirs);

			Config = new RepositoryConfig(this);
			_refDb = new RefDatabase(this);

			bool isExisting = _objectsDirs[0].Exists;
			if (isExisting)
			{
				try
				{
					Config.load();
				}
				catch (ConfigInvalidException e1)
				{
					throw new IOException("Unknown repository format", e1);
				}

				string repositoryFormatVersion = Config.getString("core", null, "repositoryFormatVersion");

				if (!Constants.RepositoryFormatVersion.Equals(repositoryFormatVersion))
				{
					throw new IOException(
						string.Format("Unknown repository format \"{0}\"; expected \"0\".", repositoryFormatVersion));
				}
			}
			else
			{
				Create();
			}
		}

		public event EventHandler<RefsChangedEventArgs> RefsChanged;
		public event EventHandler<IndexChangedEventArgs> IndexChanged;

		internal void OnRefsChanged()
		{
			var handler = RefsChanged;
			if (handler != null)
			{
				handler(this, new RefsChangedEventArgs(this));
			}
		}

		internal void OnIndexChanged()
		{
			var handler = IndexChanged;
			if (handler != null)
			{
				handler(this, new IndexChangedEventArgs(this));
			}
		}

		/// <summary>
		/// Create a new Git repository initializing the necessary files and
		/// directories.
		/// </summary>
		public void Create()
		{
			Create(false);
		}

		/// <summary>
		/// Create a new Git repository initializing the necessary files and
		/// directories.
		/// </summary>
		/// <param name="bare">if true, a bare repository is created.</param>
		public void Create(bool bare)
		{
			if (Directory.Exists)
			{
				throw new GitException("Unable to create repository. Directory already exists.");
			}

			Directory.Create();
			_refDb.Create();

			_objectsDirs[0].Create();
			new DirectoryInfo(Path.Combine(_objectsDirs[0].FullName, "pack")).Create();
			new DirectoryInfo(Path.Combine(_objectsDirs[0].FullName, "info")).Create();
			new DirectoryInfo(Path.Combine(Directory.FullName, "branches")).Create();
			new DirectoryInfo(Path.Combine(Directory.FullName, "remote")).Create();

			const string master = Constants.RefsHeads + Constants.Master;

			_refDb.Link(Constants.HEAD, master);

			Config.setInt("core", null, "repositoryformatversion", 0);
			Config.setBoolean("core", null, "filemode", true);

			if (bare)
			{
				Config.setBoolean("core", null, "bare", true);
			}

			Config.save();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="objectId"></param>
		/// <returns>
		/// true if the specified object is stored in this repo or any of the
		/// known shared repositories.
		/// </returns>
		public bool HasObject(AnyObjectId objectId)
		{
			return _objectDatabase.HasObject(objectId);
		}

		private static List<DirectoryInfo> ReadObjectsDirs(string objectsDir, ref List<DirectoryInfo> ret)
		{
			ret.Add(new DirectoryInfo(objectsDir));
			var altFile = new FileInfo(Path.Combine(Path.Combine(objectsDir, "info"), "alternates"));
			if (altFile.Exists)
			{
				using (StreamReader reader = altFile.OpenText())
				{
					for (string alt = reader.ReadLine(); alt != null; alt = reader.ReadLine())
					{
						ReadObjectsDirs(Path.Combine(objectsDir, alt), ref ret);
					}
				}
			}

			return ret;
		}

		public DirectoryInfo ObjectsDirectory
		{
			get { return _objectsDirs[0]; }
		}

		public DirectoryInfo Directory { get; private set; }
		public DirectoryInfo WorkingDirectory { get; private set; }
		public RepositoryConfig Config { get; private set; }

		/// <summary>
		/// Construct a filename where the loose object having a specified SHA-1
		/// should be stored. If the object is stored in a shared repository the path
		/// to the alternative repo will be returned. If the object is not yet store
		/// a usable path in this repo will be returned. It is assumed that callers
		/// will look for objects in a pack first.
		/// </summary>
		/// <param name="objectId"></param>
		/// <returns>Suggested file name</returns>
		public FileInfo ToFile(AnyObjectId objectId)
		{
			string n = objectId.ToString();
			string d = n.Slice(0, 2);
			string f = n.Substring(2);
			for (int i = 0; i < _objectsDirs.Count; ++i)
			{
				var ret = new FileInfo(PathUtil.Combine(_objectsDirs[i].FullName, d, f));
				if (ret.Exists)
				{
					return ret;
				}
			}

			return new FileInfo(PathUtil.Combine(_objectsDirs[0].FullName, d, f));
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="windowCursor">
		/// Temporary working space associated with the calling thread.
		/// </param>
		/// <param name="id">SHA-1 of an object.</param>
		/// <returns>
		/// A <see cref="ObjectLoader"/> for accessing the data of the named
		/// object, or null if the object does not exist.
		/// </returns>
		public ObjectLoader OpenObject(WindowCursor windowCursor, AnyObjectId id)
		{
			return _objectDatabase.OpenObject(windowCursor, id);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id">SHA-1 of an object.</param>
		/// <returns>
		/// A <see cref="ObjectLoader"/> for accessing the data of the named
		/// object, or null if the object does not exist.
		/// </returns>
		public ObjectLoader OpenObject(AnyObjectId id)
		{
			var wc = new WindowCursor();
			try
			{
				return OpenObject(wc, id);
			}
			finally
			{
				wc.Release();
			}
		}

		/// <summary>
		/// Open object in all packs containing specified object.
		/// </summary>
		/// <param name="objectId">id of object to search for</param>
		/// <param name="windowCursor">
		/// Temporary working space associated with the calling thread.
		/// </param>
		/// <returns>
		/// Collection of loaders for this object, from all packs containing
		/// this object
		/// </returns>
		public IEnumerable<PackedObjectLoader> OpenObjectInAllPacks(AnyObjectId objectId, WindowCursor windowCursor)
		{
			var result = new List<PackedObjectLoader>();
			OpenObjectInAllPacks(objectId, result, windowCursor);
			return result;
		}

		/// <summary>
		/// Open object in all packs containing specified object.
		/// </summary>
		/// <param name="objectId"><see cref="ObjectId"/> of object to search for</param>
		/// <param name="resultLoaders">
		/// Result collection of loaders for this object, filled with
		/// loaders from all packs containing specified object
		/// </param>
		/// <param name="windowCursor">
		/// Temporary working space associated with the calling thread.
		/// </param>
		public void OpenObjectInAllPacks(AnyObjectId objectId, ICollection<PackedObjectLoader> resultLoaders,
										 WindowCursor windowCursor)
		{
			_objectDatabase.OpenObjectInAllPacks(resultLoaders, windowCursor, objectId);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id">SHA'1 of a blob</param>
		/// <returns>
		/// An <see cref="ObjectLoader"/> for accessing the data of a named blob
		/// </returns>
		public ObjectLoader OpenBlob(ObjectId id)
		{
			return OpenObject(id);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="id">SHA'1 of a tree</param>
		/// <returns>
		/// An <see cref="ObjectLoader"/> for accessing the data of a named tree
		/// </returns>
		public ObjectLoader OpenTree(ObjectId id)
		{
			return OpenObject(id);
		}

		/// <summary>
		/// Access a Commit object using a symbolic reference. This reference may
		/// be a SHA-1 or ref in combination with a number of symbols translating
		/// from one ref or SHA1-1 to another, such as HEAD^ etc.
		/// </summary>
		/// <param name="resolveString">a reference to a git commit object</param>
		/// <returns>A <see cref="Commit"/> named by the specified string</returns>
		public Commit MapCommit(string resolveString)
		{
			ObjectId id = Resolve(resolveString);
			return id != null ? MapCommit(id) : null;
		}

		/// <summary>
		/// Access a Commit by SHA'1 id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns>Commit or null</returns>
		public Commit MapCommit(ObjectId id)
		{
			ObjectLoader or = OpenObject(id);
			if (or == null) return null;

			byte[] raw = or.Bytes;
			if (ObjectType.Commit == or.Type)
			{
				return new Commit(this, id, raw);
			}

			throw new IncorrectObjectTypeException(id, ObjectType.Commit);
		}

		/// <summary>
		/// Access any type of Git object by id and
		/// </summary>
		/// <param name="id">SHA-1 of object to read</param>
		/// <param name="refName">optional, only relevant for simple tags</param>
		/// <returns>The Git object if found or null</returns>
		public object MapObject(ObjectId id, string refName)
		{
			ObjectLoader or = OpenObject(id);

			if (or == null)
			{
				return null;
			}

			byte[] raw = or.Bytes;
			switch (or.Type)
			{
				case ObjectType.Tree:
					return MakeTree(id, raw);

				case ObjectType.Commit:
					return MakeCommit(id, raw);

				case ObjectType.Tag:
					return MakeTag(id, refName, raw);

				case ObjectType.Blob:
					return raw;

				default:
					throw new IncorrectObjectTypeException(id,
						"COMMIT nor TREE nor BLOB nor TAG");
			}
		}

		private object MakeCommit(ObjectId id, byte[] raw)
		{
			return new Commit(this, id, raw);
		}

		/// <summary>
		/// Access a Tree object using a symbolic reference. This reference may
		/// be a SHA-1 or ref in combination with a number of symbols translating
		/// from one ref or SHA1-1 to another, such as HEAD^{tree} etc.
		/// </summary>
		/// <param name="revstr">a reference to a git commit object</param>
		/// <returns>a Tree named by the specified string</returns>
		public Tree MapTree(string revstr)
		{
			ObjectId id = Resolve(revstr);
			return (id != null) ? MapTree(id) : null;
		}

		/// <summary>
		/// Access a Tree by SHA'1 id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns>Tree or null</returns>
		public Tree MapTree(ObjectId id)
		{
			ObjectLoader or = OpenObject(id);
			if (or == null) return null;

			byte[] raw = or.Bytes;
			switch (or.Type)
			{
				case ObjectType.Tree:
					return new Tree(this, id, raw);

				case ObjectType.Commit:
					return MapTree(ObjectId.FromString(raw, 5));
			}

			throw new IncorrectObjectTypeException(id, ObjectType.Tree);
		}

		private Tag MakeTag(ObjectId id, string refName, byte[] raw)
		{
			return new Tag(this, id, refName, raw);
		}

		private Tree MakeTree(ObjectId id, byte[] raw)
		{
			return new Tree(this, id, raw);
		}

		/// <summary>
		/// Access a tag by symbolic name.
		/// </summary>
		/// <param name="revstr"></param>
		/// <returns>Tag or null</returns>
		public Tag MapTag(string revstr)
		{
			ObjectId id = Resolve(revstr);
			return id != null ? MapTag(revstr, id) : null;
		}

		/// <summary>
		/// Access a Tag by SHA'1 id
		/// </summary>
		/// <param name="refName"></param>
		/// <param name="id"></param>
		/// <returns>Commit or null</returns>
		public Tag MapTag(string refName, ObjectId id)
		{
			ObjectLoader or = OpenObject(id);
			if (or == null) return null;

			byte[] raw = or.Bytes;
			
			if (ObjectType.Tag == or.Type)
			{
				return new Tag(this, id, refName, raw);
			}

			return new Tag(this, id, refName, null);
		}

		/// <summary>
		/// Create a command to update (or create) a ref in this repository.
		/// </summary>
		/// <param name="refName">
		/// name of the ref the caller wants to modify.
		/// </param>
		/// <returns>
		/// An update command. The caller must finish populating this command
		/// and then invoke one of the update methods to actually make a
		/// change.
		/// </returns>
		public RefUpdate UpdateRef(string refName)
		{
			return _refDb.NewUpdate(refName);
		}

		///	<summary>
		/// Parse a git revision string and return an object id.
		///	<para />
		///	Currently supported is combinations of these.
		///	<ul>
		///	 <li>SHA-1 - a SHA-1</li>
		///	 <li>refs/... - a ref name</li>
		///	 <li>ref^n - nth parent reference</li>
		///	 <li>ref~n - distance via parent reference</li>
		///	 <li>ref@{n} - nth version of ref</li>
		///	 <li>ref^{tree} - tree references by ref</li>
		///	 <li>ref^{commit} - commit references by ref</li>
		///	</ul>
		///	<para />
		///	Not supported is
		///	<ul>
		///	 <li>timestamps in reflogs, ref@{full or relative timestamp}</li>
		///	 <li>abbreviated SHA-1's</li>
		///	</ul>
		///	</summary>
		///	<param name="revision">A git object references expression.</param>
		///	<returns>
		/// An <see cref="ObjectId"/> or null if revstr can't be resolved to any <see cref="ObjectId"/>.
		/// </returns>
		///	<exception cref="IOException">On serious errors.</exception>
		public ObjectId Resolve(string revision)
		{
			object oref = null;
			ObjectId refId = null;

			for (int i = 0; i < revision.Length; ++i)
			{
				switch (revision[i])
				{
					case '^':
						if (refId == null)
						{
							var refstr = new string(revision.ToCharArray(0, i));
							refId = ResolveSimple(refstr);
							if (refId == null) return null;
						}

						if (i + 1 < revision.Length)
						{
							switch (revision[i + 1])
							{
								case '0':
								case '1':
								case '2':
								case '3':
								case '4':
								case '5':
								case '6':
								case '7':
								case '8':
								case '9':

									int j;
									oref = MapObject(refId, null);

									while (oref is Tag)
									{
										var tag = (Tag)oref;
										refId = tag.Id;
										oref = MapObject(refId, null);
									}

									if (!(oref is Commit))
									{
										throw new IncorrectObjectTypeException(refId, ObjectType.Commit);
									}

									for (j = i + 1; j < revision.Length; ++j)
									{
										if (!Char.IsDigit(revision[j])) break;
									}

									var parentnum = new string(revision.ToCharArray(i + 1, j - i - 1));

									int pnum;
									if (int.TryParse(parentnum, out pnum) && pnum != 0)
									{
										ObjectId[] parents = ((Commit)oref).ParentIds;
										refId = pnum > parents.Length ? null : parents[pnum - 1];
									}

									i = j - 1;
									break;

								case '{':
									int k;
									string item = null;
									for (k = i + 2; k < revision.Length; ++k)
									{
										if (revision[k] != '}') continue;
										item = new string(revision.ToCharArray(i + 2, k - i - 2));
										break;
									}

									i = k;
									if (item != null)
									{
										if (item.Equals("tree"))
										{
											oref = MapObject(refId, null);
											while (oref is Tag)
											{
												var t = (Tag)oref;
												refId = t.Id;
												oref = MapObject(refId, null);
											}
											if (oref is Treeish)
											{
												refId = ((Treeish)oref).TreeId;
											}
											else
											{
												throw new IncorrectObjectTypeException(refId, ObjectType.Tree);
											}
										}
										else if (item.Equals("commit"))
										{
											oref = MapObject(refId, null);
											while (oref is Tag)
											{
												var t = (Tag)oref;
												refId = t.Id;
												oref = MapObject(refId, null);
											}
											if (!(oref is Commit))
											{
												throw new IncorrectObjectTypeException(refId, ObjectType.Commit);
											}
										}
										else if (item.Equals("blob"))
										{
											oref = MapObject(refId, null);
											while (oref is Tag)
											{
												var t = (Tag)oref;
												refId = t.Id;
												oref = MapObject(refId, null);
											}
											if (!(oref is byte[]))
											{
												throw new IncorrectObjectTypeException(refId, ObjectType.Commit);
											}
										}
										else if (string.Empty.Equals(item))
										{
											oref = MapObject(refId, null);
											while (oref is Tag)
											{
												var t = (Tag)oref;
												refId = t.Id;
												oref = MapObject(refId, null);
											}
										}
										else
										{
											throw new RevisionSyntaxException(revision);
										}
									}
									else
									{
										throw new RevisionSyntaxException(revision);
									}
									break;

								default:
									oref = MapObject(refId, null);
									if (oref is Commit)
									{
										ObjectId[] parents = ((Commit)oref).ParentIds;
										refId = parents.Length == 0 ? null : parents[0];
									}
									else
									{
										throw new IncorrectObjectTypeException(refId, ObjectType.Commit);
									}
									break;
							}
						}
						else
						{
							oref = MapObject(refId, null);
							while (oref is Tag)
							{
								var tag = (Tag)oref;
								refId = tag.Id;
								oref = MapObject(refId, null);
							}

							if (oref is Commit)
							{
								ObjectId[] parents = ((Commit)oref).ParentIds;
								refId = parents.Length == 0 ? null : parents[0];
							}
							else
							{
								throw new IncorrectObjectTypeException(refId, Constants.TYPE_COMMIT);
							}
						}
						break;

					case '~':
						if (oref == null)
						{
							var refstr = new string(revision.ToCharArray(0, i));
							refId = ResolveSimple(refstr);
							if (refId == null) return null;
							oref = MapObject(refId, null);
						}

						while (oref is Tag)
						{
							var tag = (Tag)oref;
							refId = tag.Id;
							oref = MapObject(refId, null);
						}

						if (!(oref is Commit))
						{
							throw new IncorrectObjectTypeException(refId, Constants.TYPE_COMMIT);
						}

						int l;
						for (l = i + 1; l < revision.Length; ++l)
						{
							if (!Char.IsDigit(revision[l]))
								break;
						}

						var distnum = new string(revision.ToCharArray(i + 1, l - i - 1));
						int dist;

						try
						{
							dist = Convert.ToInt32(distnum);
						}
						catch (FormatException)
						{
							throw new RevisionSyntaxException("Invalid ancestry length", revision);
						}
						while (dist > 0)
						{

							ObjectId[] parents = ((Commit)oref).ParentIds;
							if (parents.Length == 0)
							{
								refId = null;
								break;
							}
							refId = parents[0];
							oref = MapCommit(refId);
							--dist;
						}
						i = l - 1;
						break;

					case '@':
						int m;
						string time = null;
						for (m = i + 2; m < revision.Length; ++m)
						{
							if (revision[m] != '}') continue;
							time = new string(revision.ToCharArray(i + 2, m - i - 2));
							break;
						}

						if (time != null)
						{
							throw new RevisionSyntaxException("reflogs not yet supported by revision parser yet", revision);
						}
						i = m - 1;
						break;

					default:
						if (refId != null)
						{
							throw new RevisionSyntaxException(revision);
						}
						break;
				}
			}

			if (refId == null)
			{
				refId = ResolveSimple(revision);
			}

			return refId;
		}

		private ObjectId ResolveSimple(string revstr)
		{
			if (ObjectId.IsId(revstr))
			{
				return ObjectId.FromString(revstr);
			}
			Ref r = _refDb.ReadRef(revstr);
			return r != null ? r.ObjectId : null;
		}

		public void IncrementOpen()
		{
			Interlocked.Increment(ref _useCnt);
		}

		/// <summary>
		/// Close all resources used by this repository
		/// </summary>
		public void Close()
		{
			int usageCount = Interlocked.Decrement(ref _useCnt);
			if (usageCount == 0)
			{
				_objectDatabase.Close();
			}
		}

		public void OpenPack(FileInfo pack, FileInfo idx)
		{
			_objectDatabase.OpenPack(pack, idx);
		}

		public ObjectDirectory ObjectDatabase
		{
			get { return _objectDatabase; }
		}

		/// <summary>
		/// Writes a symref (e.g. HEAD) to disk
		/// </summary>
		/// <param name="name">symref name</param>
		/// <param name="target">pointed to ref</param>
		public void WriteSymref(string name, string target)
		{
			_refDb.Link(name, target);
		}

		/// <summary>
		/// Gets a representation of the index associated with this repo
		/// </summary>
		public GitIndex Index
		{
			get
			{
				if (_index == null)
				{
					_index = new GitIndex(this);
					_index.Read();
				}
				else
				{
					_index.RereadIfNecessary();
				}

				return _index;
			}
		}

		/// <summary>
		/// Clean up stale caches.
		/// </summary>
		public void RefreshFromDisk()
		{
			_refDb.ClearCache();
		}

		/// <summary>
		/// Replaces any windows director separators (backslash) with /
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		internal static byte[] GitInternalSlash(byte[] bytes)
		{
			if (Path.DirectorySeparatorChar == '/') // [henon] DirectorySeparatorChar == \
			{
				return bytes;
			}

			for (int i = 0; i < bytes.Length; ++i)
			{
				if (bytes[i] == Path.DirectorySeparatorChar)
				{
					bytes[i] = (byte)'/';
				}
			}

			return bytes;
		}

		/// <summary>
		/// Strip work dir and return normalized repository path
		/// </summary>
		/// <param name="wd">Work directory</param>
		/// <param name="f">File whose path shall be stripp off it's workdir</param>
		/// <returns>Normalized repository relative path</returns>
		public static string StripWorkDir(FileSystemInfo wd, FileSystemInfo f)
		{
			string relName = f.FullName.Substring(wd.FullName.Length + 1);
			relName = relName.Replace(Path.DirectorySeparatorChar, '/');
			return relName;
		}

		/// <summary>
		/// Gets the <see cref="Repository"/> state
		/// </summary>
		public RepositoryState RespositoryState
		{
			get
			{
				// Pre Git-1.6 logic
				if (WorkingDirectory.GetFiles(".dotest").Length > 0)
				{
					return RepositoryState.Rebasing;
				}

				if (WorkingDirectory.GetFiles(".dotest-merge").Length > 0)
				{
					return RepositoryState.RebasingInteractive;
				}

				// From 1.6 onwards
				if (WorkingDirectory.GetFiles("rebase-apply/rebasing").Length > 0)
				{
					return RepositoryState.RebasingRebasing;
				}

				if (WorkingDirectory.GetFiles("rebase-apply/applying").Length > 0)
				{
					return RepositoryState.Apply;
				}

				if (WorkingDirectory.GetFiles("rebase-apply").Length > 0)
				{
					return RepositoryState.Rebasing;
				}

				if (WorkingDirectory.GetFiles("rebase-merge/interactive").Length > 0)
				{
					return RepositoryState.RebasingInteractive;
				}

				if (WorkingDirectory.GetFiles("rebase-merge").Length > 0)
				{
					return RepositoryState.RebasingMerge;
				}

				// Both versions
				if (WorkingDirectory.GetFiles("MERGE_HEAD").Length > 0)
				{
					return RepositoryState.Merging;
				}

				if (WorkingDirectory.GetFiles("BISECT_LOG").Length > 0)
				{
					return RepositoryState.Bisecting;
				}

				return RepositoryState.Safe;
			}
		}

		public Dictionary<string, Ref> getAllRefs()
		{
			return _refDb.GetAllRefs();
		}

		public Ref getRef(string name)
		{
			return _refDb.ReadRef(name);
		}

		public Dictionary<string, Ref> getTags()
		{
			return _refDb.GetTags();
		}

		public Ref Head
		{
			get { return getRef("HEAD"); }
		}

		public void Link(string name, string target)
		{
			_refDb.Link(name, target);
		}

		public Ref Peel(Ref pRef)
		{
			return _refDb.Peel(pRef);
		}

		public static Repository Open(string directory)
		{
			return Open(new DirectoryInfo(directory));
		}

		public static Repository Open(DirectoryInfo directory)
		{
			var name = directory.FullName;
			if (name.EndsWith(".git"))
			{
				return new Repository(directory);
			}

			var subDirectories = directory.GetDirectories(".git");
			if (subDirectories.Length > 0)
			{
				return new Repository(subDirectories[0]);
			}

			if (directory.Parent == null) return null;

			return Open(directory.Parent);
		}

		/// <summary>
		/// Check validity of a ref name. It must not contain character that has
		/// a special meaning in a Git object reference expression. Some other
		/// dangerous characters are also excluded.
		/// </summary>
		/// <param name="refName"></param>
		/// <returns>
		/// Returns true if <paramref name="refName"/> is a valid ref name.
		/// </returns>
		public static bool IsValidRefName(string refName)
		{
			int len = refName.Length;

			if (len == 0) return false;

			if (refName.EndsWith(".lock")) return false;

			int components = 1;
			char p = '\0';
			for (int i = 0; i < len; i++)
			{
				char c = refName[i];
				if (c <= ' ') return false;

				switch (c)
				{
					case '.':
						switch (p)
						{
							case '\0':
							case '/':
							case '.':
								return false;
						}

						if (i == len - 1) return false;
						break;

					case '/':
						if (i == 0 || i == len - 1) return false;
						components++;
						break;

					case '{':
						if (p == '@') return false;
						break;

					case '~':
					case '^':
					case ':':
					case '?':
					case '[':
					case '*':
					case '\\':
						return false;
				}
				p = c;
			}

			return components > 1;
		}

		public Commit OpenCommit(ObjectId id)
		{
			return MapCommit(id);
		}

		public override string ToString()
		{
			return "Repository[" + Directory + "]";
		}

		public string FullBranch
		{
			get
			{
				var ptr = new FileInfo(Path.Combine(Directory.FullName, Constants.HEAD));
				var sr = new StreamReader(ptr.FullName);
				string reference;
				try
				{
					reference = sr.ReadLine();
				}
				finally
				{
					sr.Close();
				}

				if (reference.StartsWith("ref: "))
				{
					reference = reference.Substring(5);
				}

				return reference;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="refName"></param>
		/// <returns>A more user friendly ref name</returns>
		public static string ShortenRefName(string refName)
		{
			if (refName.StartsWith(Constants.R_HEADS))
			{
				return refName.Substring(Constants.R_HEADS.Length);
			}

			if (refName.StartsWith(Constants.R_TAGS))
			{
				return refName.Substring(Constants.R_TAGS.Length);
			}

			if (refName.StartsWith(Constants.R_REMOTES))
			{
				return refName.Substring(Constants.R_REMOTES.Length);
			}

			return refName;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="refName"></param>
		/// <returns>
		/// A <see cref="ReflogReader"/> for the supplied <paramref name="refName"/>,
		/// or null if the /// named ref does not exist.
		/// </returns>
		public ReflogReader ReflogReader(string refName)
		{
			Ref @ref = getRef(refName);
			if (@ref != null)
			{
				return new ReflogReader(this, @ref.OriginalName);
			}

			return null;
		}
	}
}