/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

namespace GitSharp
{
    [Complete]
    public class RefDatabase
    {
        public Repository Repository { get; private set; }

        private DirectoryInfo _gitDir;
        private DirectoryInfo _refsDir;
        private FileInfo _packedRefsFile;

        private Dictionary<string, CachedRef> looseRefs;
        private Dictionary<string, Ref> packedRefs;

        private DateTime packedRefsLastModified;
        private long packedRefsLength;

        public RefDatabase(Repository repo)
        {
            this.Repository = repo;
            _gitDir = repo.Directory;
            _refsDir = PathUtil.CombineDirectoryPath(_gitDir, "refs");
            _packedRefsFile = PathUtil.CombineFilePath(_gitDir, "packed-refs");
            ClearCache();
        }

        public void ClearCache()
        {
            looseRefs = new Dictionary<string, CachedRef>();
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
            Ref r = ReadRefBasic(name, 0);
            return (r != null) ? r.ObjectId : null;
        }

        public RefUpdate NewUpdate(string name)
        {
            Ref r = ReadRefBasic(name, 0);
            if (r == null)
                r = new Ref(Ref.Storage.New, name, null);
            return new RefUpdate(this, r, FileForRef(r.Name));
        }

        public void Stored(string name, ObjectId id, DateTime time)
        {
            looseRefs.Add(name, new CachedRef(Ref.Storage.Loose, name, id, time));
        }

        public void Link(string name, string target)
        {
            byte[] content = Constants.Encoding.GetBytes("ref: " + target + "\n");
            LockFile lck = new LockFile(FileForRef(name));
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

        public Dictionary<string, Ref> GetAllRefs()
        {
            return ReadRefs();
        }

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

        private Dictionary<string, Ref> ReadRefs()
        {
            var avail = new Dictionary<string, Ref>();
            ReadPackedRefs(avail);
            ReadLooseRefs(avail, Constants.Refs, _refsDir);
            ReadOneLooseRef(avail, Constants.Head, PathUtil.CombineFilePath(_gitDir, Constants.Head));
            return avail;
        }

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
                if(ent is DirectoryInfo)
                    ReadLooseRefs(avail, prefix + ent.Name, (DirectoryInfo)ent);
                else
                    ReadOneLooseRef(avail, prefix + "/" + ent.Name, ent);
            }

        }

        private void ReadOneLooseRef(Dictionary<string, Ref> avail, string refName, FileSystemInfo ent)
        {
            CachedRef reff;

            if (looseRefs.TryGetValue(refName, out reff) && reff != null)
            {
                if (reff.LastModified == ent.LastWriteTime)
                {
                    avail.Add(reff.Name, reff);
                    return;
                }
                looseRefs.Remove(refName);
            }

            // Assume its a valid loose reference we need to cache.
            //
            try
            {

                using (var reader = new StreamReader(ent.FullName))
                {
                    var str = reader.ReadToEnd().Trim();                    
                    var id = ObjectId.FromString(str);

                    if (id == null)
                        return;

                    reff = new CachedRef(Ref.Storage.Loose, refName, id, ent.LastWriteTime);

                    looseRefs.AddOrReplace(reff.Name, reff);
                    avail.AddOrReplace(reff.Name, reff);
                }
            }
            catch (FileNotFoundException)
            {
                // Deleted while we were reading? Its gone now!
                //
            }
            catch (IOException err)
            {
                // Whoops.
                //
                throw new GitException("Cannot read ref " + ent, err);
            }
        }

        private FileInfo FileForRef(string name)
        {
            if (name.StartsWith(Constants.Refs))
                return PathUtil.CombineFilePath(_refsDir, name.Substring(Constants.Refs.Length));
            return PathUtil.CombineFilePath(_gitDir, name);
        }

        private Ref ReadRefBasic(string name, int depth)
        {
            // Prefer loose ref to packed ref as the loose
            // file can be more up-to-date than a packed one.
            //
            FileInfo loose = FileForRef(name);
            DateTime mtime = loose.LastWriteTime;

            if (looseRefs.ContainsKey(name))
            {
                if (looseRefs[name].LastModified == mtime)
                    return looseRefs[name];
                looseRefs.Remove(name);
            }

            if (!loose.Exists)
            {
                // If last modified is 0 the file does not exist.
                // Try packed cache.
                //
                return (packedRefs.ContainsKey(name)) ? packedRefs[name] : null;
            }

            var line = ReadLine(loose);

            if (string.IsNullOrEmpty(line))
                return new Ref(Ref.Storage.Loose, name, null);

            if (line.StartsWith("ref: "))
            {
                if (depth >= 5)
                    throw new IOException("Exceeded maximum ref depth of " + depth + " at " + name + ".  Circular reference?");

                string target = line.Substring("ref: ".Length);
                Ref r = ReadRefBasic(target, depth + 1);
                return r != null ? r : new Ref(Ref.Storage.Loose, target, null);
            }

            ObjectId id;
            try
            {
                id = ObjectId.FromString(line);
            }
            catch (ArgumentException)
            {
                throw new IOException("Not a ref: " + name + ": " + line);
            }

            var reff = new CachedRef(Ref.Storage.Loose, name, id, mtime);
            looseRefs.Add(name, reff);
            return reff;
        }

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
                using(var b = OpenReader(_packedRefsFile))
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
                                newPackedRefs[last.Name]=last; // [henon] <--- sometimes the same tag exits twice for some reason (i.e. a tag referencing itself) ... so we silently ignore the problem. hope this is the expected behavior
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

        private string ReadLine(FileInfo file)
        {
            using (BufferedReader br = OpenReader(file))
            {
                return br.ReadLine();
            }
        }

        private BufferedReader OpenReader(FileInfo file)
        {
            return new BufferedReader(file.FullName);
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

    }
}
