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
using System.Linq;
using System.Text;
using System.IO;
using GitSharp.Util;
using GitSharp.Exceptions;
using System.Runtime.CompilerServices;

namespace GitSharp
{
    public class RefDatabase
    {
        public Repository Repository { get; private set; }

        private DirectoryInfo _gitDir;
        private DirectoryInfo _refsDir;
        private FileInfo _packedRefsFile;

        private Dictionary<string, Ref> looseRefs;
        private Dictionary<string, DateTime> looseRefsMTime;
        private Dictionary<string, Ref> packedRefs;
        private Dictionary<string, string> looseSymRefs;

        private DateTime packedRefsLastModified;
        private long packedRefsLength;

        public int lastRefModification;

        public int lastNotifiedRefModification;

        private int refModificationCounter;

        public RefDatabase(Repository repo)
        {
            this.Repository = repo;
            _gitDir = repo.Directory;
            _refsDir = PathUtil.CombineDirectoryPath(_gitDir, "refs");
            _packedRefsFile = PathUtil.CombineFilePath(_gitDir, "packed-refs");
            ClearCache();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ClearCache()
        {
            looseRefs = new Dictionary<string, Ref>();
            looseRefsMTime = new Dictionary<string, DateTime>();
            looseSymRefs = new Dictionary<string, string>();
            packedRefs = new Dictionary<string, Ref>();
            packedRefsLastModified = DateTime.MinValue;
            packedRefsLength = 0;
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

        /**
         * Create a command to update, create or delete a ref in this repository.
         * 
         * @param name
         *            name of the ref the caller wants to modify.
         * @return an update command. The caller must finish populating this command
         *         and then invoke one of the update methods to actually make a
         *         change.
         * @throws IOException
         *             a symbolic ref was passed in and could not be resolved back
         *             to the base ref, as the symbolic ref could not be read.
         */
        public RefUpdate NewUpdate(string name)
        {
            RefreshPackedRefs();
            Ref r = ReadRefBasic(name, 0);
            if (r == null)
                r = new Ref(Ref.Storage.New, name, null);
            return new RefUpdate(this, r, FileForRef(r.Name));
        }

        //public void Stored(string name, ObjectId id, DateTime time)
        //{
        //    looseRefs.Add(name, new CachedRef(Ref.Storage.Loose, name, id, time));
        //}

        public void stored(String origName, String name, ObjectId id, DateTime time)
        {
            lock (this)
            {
                looseRefs[name] = new Ref(Ref.Storage.Loose, origName, name, id);
                looseRefsMTime[name] = time;
                setModified();
            }
            this.Repository.fireRefsMaybeChanged();
        }

        /**
         * Writes a symref (e.g. HEAD) to disk
         * 
         * @param name
         *            symref name
         * @param target
         *            pointed to ref
         * @throws IOException
         */
        public void Link(string name, string target)
        {
            byte[] content = Constants.Encoding.GetBytes("ref: " + target + "\n");
            lockAndWriteFile(FileForRef(name), content);
            lock (this)
            {
                setModified();
            }
            this.Repository.fireRefsMaybeChanged();
        }

        private void setModified()
        {
            lastRefModification = refModificationCounter++;
        }

        public Ref ReadRef(string partialName)
        {
            RefreshPackedRefs();
            foreach (var searchPath in Constants.RefSearchPaths)
            {
                Ref r = ReadRefBasic(searchPath + partialName, 0);
                if (r != null && r.ObjectId != null)
                    return r;
            }
            return null;
        }

        /**
         * @return all known refs (heads, tags, remotes).
         */
        public Dictionary<string, Ref> GetAllRefs()
        {
            return ReadRefs();
        }

        /**
         * @return all tags; key is short tag name ("v1.0") and value of the entry
         *         contains the ref with the full tag name ("refs/tags/v1.0").
         */
        public Dictionary<string, Ref> GetTags()
        {
            Dictionary<string, Ref> tags = new Dictionary<string, Ref>();
            foreach (Ref r in ReadRefs().Values)
            {
                if (r.Name.StartsWith(Constants.RefsTags))
                    tags.Add(r.Name.Substring(Constants.RefsTags.Length), r);
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
                    avail[Constants.HEAD]= r;
            }
            catch (IOException)
            {
                // ignore here
            }
            this.Repository.fireRefsMaybeChanged();
            return avail;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ReadPackedRefs(Dictionary<string, Ref> avail)
        {
            RefreshPackedRefs();
            foreach (KeyValuePair<string, Ref> kv in packedRefs)
            {
                avail.Add(kv.Key, kv.Value);
            }
        }

        private void ReadLooseRefs(Dictionary<string, Ref> avail, string prefix, DirectoryInfo dir)
        {
            var entries = dir.GetFileSystemInfos();
            if (entries.Length == 0)
                return;

            foreach (FileSystemInfo ent in entries)
            {
			 String entName = ent.Name;
			if (".".Equals(entName) || "..".Equals(entName))
				continue;
			if (ent is DirectoryInfo) {
				ReadLooseRefs(avail, prefix + entName + "/", ent as DirectoryInfo);
			} else {
				try {
					 Ref @ref = ReadRefBasic(prefix + entName, 0);
					if (@ref != null)
						avail[@ref.OriginalName]=@ref;
				} catch (IOException) {
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
            if (dref.Peeled)
                return dref;

            ObjectId peeled = null;
            try
            {
                object target = Repository.MapObject(dref.ObjectId, dref.Name);

                while (target is Tag)
                {
                    Tag tag = (Tag)target;
                    peeled = tag.Id;

                    if (tag.TagType == Constants.ObjectTypes.Tag)
                        target = Repository.MapObject(tag.Id, dref.Name);
                    else
                        break;
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
            if (name.StartsWith(Constants.Refs))
                return PathUtil.CombineFilePath(_refsDir, name.Substring(Constants.Refs.Length));
            return PathUtil.CombineFilePath(_gitDir, name);
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
            FileInfo loose = FileForRef(name);
            DateTime mtime = loose.LastWriteTime;
            Ref @ref = null;

            if (looseRefs.ContainsKey(name))
            {
                @ref = looseRefs[name];
                DateTime cachedlastModified = looseRefsMTime[name];
                if (cachedlastModified != null && cachedlastModified == mtime)
                {
                    if (packedRefs.ContainsKey(origName))
                        return new Ref(GitSharp.Ref.Storage.LoosePacked, origName, @ref.ObjectId, @ref.PeeledObjectId, @ref.Peeled);
                    else
                        return @ref;
                }
                looseRefs.Remove(origName);
                looseRefsMTime.Remove(origName);
            }

            if (!loose.Exists)
            {
                // File does not exist.
                // Try packed cache.
                //
                packedRefs.TryGetValue(name, out @ref);
                if (@ref != null)
                    if (!@ref.OriginalName.Equals(origName))
                        @ref = new Ref(GitSharp.Ref.Storage.LoosePacked, origName, name, @ref.ObjectId);
                return @ref;
            }

            String line = null;
            try
            {
                DateTime cachedlastModified = DateTime.MinValue;
                looseRefsMTime.TryGetValue(name, out cachedlastModified);
                if (cachedlastModified != null && cachedlastModified == mtime)
                {
                    looseSymRefs.TryGetValue(name, out line);
                }
                if (line == null)
                {
                    line = ReadLine(loose);
                    looseRefsMTime[name] = mtime;
                    looseSymRefs[name] = line;
                }
            }
            catch (FileNotFoundException)
            {
                return packedRefs[name];
            }

            if (line == null || line.Length == 0)
            {
                looseRefs.Remove(origName);
                looseRefsMTime.Remove(origName);
                return new Ref(Ref.Storage.Loose, origName, name, null);
            }

            if (line.StartsWith("ref: "))
            {
                if (depth >= 5)
                    throw new IOException("Exceeded maximum ref depth of " + depth + " at " + name + ".  Circular reference?");

                string target = line.Substring("ref: ".Length);
                Ref r = ReadRefBasic(target, depth + 1);
                var cachedMtime = DateTime.MinValue;
                looseRefsMTime.TryGetValue(name, out cachedMtime);
                if (cachedMtime != null && cachedMtime != mtime)
                    setModified();
                looseRefsMTime[name]=mtime;
                if (r == null)
                    return new Ref(Ref.Storage.Loose, origName, target, null);
                if (!origName.Equals(r.Name))
                    r = new Ref(Ref.Storage.LoosePacked, origName, r.Name, r.ObjectId, r.PeeledObjectId, true);
                return r;
            }

            setModified();

            ObjectId id;
            try
            {
                id = ObjectId.FromString(line);
            }
            catch (ArgumentException)
            {
                throw new IOException("Not a ref: " + name + ": " + line);
            }

            GitSharp.Ref.Storage storage;
            if (packedRefs.ContainsKey(name))
                storage = Ref.Storage.LoosePacked;
            else
                storage = Ref.Storage.Loose;
            @ref = new Ref(storage, name, id);
            looseRefs[name]= @ref;
            looseRefsMTime[name]= mtime;

            if (!origName.Equals(name))
            {
                @ref = new Ref(Ref.Storage.Loose, origName, name, id);
                looseRefs[origName]= @ref;
            }

            return @ref;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void RefreshPackedRefs()
        {
            if (!_packedRefsFile.Exists)
                return;

            DateTime currTime = _packedRefsFile.LastWriteTime;
            long currLen = currTime == DateTime.MinValue ? 0 : _packedRefsFile.Length;
            if (currTime == packedRefsLastModified && currLen == packedRefsLength)
                return;
            if (currTime == DateTime.MinValue)
            {
                packedRefsLastModified = DateTime.MinValue;
                packedRefsLength = 0;
                packedRefs = new Dictionary<string, Ref>();
                return;
            }

            Dictionary<string, Ref> newPackedRefs = new Dictionary<string, Ref>();
            try
            {
                using (var b = OpenReader(_packedRefsFile))
                {
                    string p;
                    Ref last = null;
                    while ((p = b.ReadLine()) != null)
                    {
                        if (p[0] == '#')
                            continue;

                        if (p[0] == '^')
                        {
                            if (last == null)
                                throw new IOException("Peeled line before ref.");

                            ObjectId id = ObjectId.FromString(p.Substring(1));
                            last = new Ref(Ref.Storage.Packed, last.Name, last.Name, last.ObjectId, id, true);
                            if (!newPackedRefs.ContainsKey(last.Name))
                                newPackedRefs[last.Name] = last; // [henon] <--- sometimes the same tag exits twice for some reason (i.e. a tag referencing itself) ... so we silently ignore the problem. hope this is the expected behavior
                            continue;
                        }

                        int sp = p.IndexOf(' ');
                        ObjectId id2 = ObjectId.FromString(p.Substring(0, sp));
                        string name = p.Substring(sp + 1);
                        last = new Ref(Ref.Storage.Packed, name, id2);
                        newPackedRefs.Add(last.Name, last);
                    }
                }
                packedRefsLastModified = currTime;
                packedRefsLength = currLen;
                packedRefs = newPackedRefs;
            }
            catch (FileNotFoundException)
            {
                // Ignore it and leave the new map empty.
                //
                packedRefsLastModified = DateTime.MinValue;
                packedRefsLength = 0;
                packedRefs = newPackedRefs;
            }
            catch (IOException e)
            {
                throw new GitException("Cannot read packed refs", e);
            }
        }


        private void lockAndWriteFile(FileInfo file, byte[] content)
        {
            String name = file.Name;
            LockFile lck = new LockFile(file);
            if (!lck.Lock())
                throw new ObjectWritingException("Unable to lock " + name);
            try
            {
                lck.Write(content);
            }
            catch (IOException ioe)
            {
                throw new ObjectWritingException("Unable to write " + name, ioe);
            }
            if (!lck.Commit())
                throw new ObjectWritingException("Unable to write " + name);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void removePackedRef(String name)
        {
            packedRefs.Remove(name);
            writePackedRefs();
        }

        private void writePackedRefs()
        {
            new ExtendedRefWriter(packedRefs.Values, this).writePackedRefs();
        }

        private class ExtendedRefWriter : RefWriter
        {
             RefDatabase ref_db;
             public ExtendedRefWriter(IEnumerable<Ref> refs, RefDatabase db) : base(refs)
             {
                this.ref_db=db;
            }

            protected override void writeFile(string name, byte[] content)
            {
                ref_db.lockAndWriteFile(new FileInfo(ref_db.Repository.Directory+"/"+ name), content);
            }
        }

        private string ReadLine(FileInfo file)
        {
            using (StreamReader sr = OpenReader(file))
            {
                return sr.ReadLine();
            }
        }

        private StreamReader OpenReader(FileInfo file)
        {
            return new StreamReader(file.FullName);
        }

        private class CachedRef : Ref
        {
            public DateTime LastModified { get; private set; }

            public CachedRef(Storage st, string refName, ObjectId id, DateTime mtime)
                : base(st, refName, id)
            {
                this.LastModified = mtime;
            }
        }


        public Dictionary<string, Ref> GetBranches()
        {
            var branches = new Dictionary<string, Ref>();
            foreach (Ref r in ReadRefs().Values)
            {
                if (r.Name.StartsWith(Constants.RefsHeads))
                    branches.Add(r.Name.Substring(Constants.RefsTags.Length), r);
            }
            return branches;
        }

        public Dictionary<string, Ref> GetRemotes()
        {
            var remotes = new Dictionary<string, Ref>();
            foreach (Ref r in ReadRefs().Values)
            {
                if (r.Name.StartsWith(Constants.RefsRemotes))
                    remotes.Add(r.Name.Substring(Constants.RefsRemotes.Length), r);
            }
            return remotes;
        }


    }
}
