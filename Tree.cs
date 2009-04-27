/*
 * Copyright (C) 2007, Robin Rosenberg <me@lathund.dewire.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using Gitty.Core.Exceptions;

namespace Gitty.Core
{
    public class Tree : TreeEntry, Treeish
    {
        #region Internals
        private static readonly TreeEntry[] EmptyTree = new TreeEntry[0];

        private readonly Repository _db;
        private TreeEntry[] _contents;
        #endregion

        #region Constructors
        public Tree(Repository repo)
            : base(null, null, null)
        {
            _db = repo;
            _contents = EmptyTree;
        }

        public Tree(Repository repo, ObjectId id, byte[] raw)
            : base(null, id, null)
        {
            _db = repo;
            ReadTree(raw);
        }

        public Tree(Tree parent, byte[] nameUTF8)
            : base(parent, null, nameUTF8)
        {
            _db = Repository;
            _contents = EmptyTree;
        }

        public Tree(Tree parent, ObjectId id, byte[] nameUTF8)
            : base(parent, id, nameUTF8)
        {
            _db = Repository;
        }
        #endregion

        #region Properties

        public bool IsRoot
        {
            get { return Parent == null; }
        }

        public override Repository Repository
        {
            get
            {
                return _db;
            }
        }

        public override FileMode Mode
        {
            get { return FileMode.Tree; }
        }
        #endregion

        public static int CompareNames(byte[] a, byte[] b, int lasta, int lastb)
        {
            return CompareNames(a, b, 0, b.Length, lasta, lastb);
        }


        /**
         * Compare two names represented as bytes. Since git treats names of trees and
         * blobs differently we have one parameter that represents a '/' for trees. For
         * other objects the value should be NUL. The names are compare by their positive
         * byte value (0..255).
         *
         * A blob and a tree with the same name will not compare equal.
         *
         * @param a name
         * @param b name
         * @param lasta '/' if a is a tree, else NUL
         * @param lastb '/' if b is a tree, else NUL
         *
         * @return < 0 if a is sorted before b, 0 if they are the same, else b
         */
        private static int CompareNames(byte[] a, byte[] nameUTF8, int nameStart, int nameEnd, int lasta, int lastb)
        {
            // There must be a .NET way of doing this! I assume there are both UTF8 names, 
            // perhaps Encoding.UTF8.GetString on both then .Compare on the strings?
            // I'm pretty sure this is just doing that but the long way round, however 
            // I could be wrong so we'll leave it at this for now. - NR
            int j = 0;
            int k = 0;

            for (j = 0; j < a.Length && k < nameEnd; j++, k++)
            {
                int aj = a[j] & 0xff;
                int bk = nameUTF8[k] & 0xff;
                if (aj < bk)
                    return -1;
                else if (aj > bk)
                    return 1;
            }

            if (j < a.Length)
            {
                int aj = a[j] & 0xff;
                if (aj < lastb)
                    return -1;
                else if (aj > lastb)
                    return 1;
                else
                    if (j == a.Length - 1)
                        return 0;
                    else
                        return -1;
            }

            if (k < nameEnd)
            {
                int bk = nameUTF8[k] & 0xff;
                if (lasta < bk)
                    return -1;
                else if (lasta > bk)
                    return 1;
                else
                    if (k == nameEnd - 1)
                        return 0;
                    else
                        return -1;
            }

            if (lasta < lastb)
                return -1;
            else if (lasta > lastb)
                return 1;

            int nameLength = nameEnd - nameStart;
            if (a.Length == nameLength)
                return 0;
            else if (a.Length < nameLength)
                return -1;
            else
                return 1;
        }

        private static byte[] SubString(byte[] s, int nameStart, int nameEnd)
        {
            if (nameStart == 0 && nameStart == s.Length)
                return new byte[] { };

            byte[] n = new byte[nameEnd - nameStart];
            Array.Copy(s, nameStart, n, 0, n.Length);
            return n;
        }

        private static int BinarySearch(
            TreeEntry[] entries, byte[] nameUTF8, int nameUTF8last, int nameStart, int nameEnd)
        {
            if (entries.Length == 0)
                return -1;
            int high = entries.Length;
            int low = 0;
            do
            {
                int mid = (low + high) / 2;
                int cmp = CompareNames(entries[mid].NameUTF8, nameUTF8,
                    nameStart, nameEnd, Gitty.Core.TreeEntry.LastChar(entries[mid]), nameUTF8last);

                if (cmp < 0)
                    low = mid + 1;
                else if (cmp == 0)
                    return mid;
                else
                    return high = mid;

            } while (low < high);
            return -(low + 1);
        }

        public override void Accept(TreeVisitor tv, int flags)
        {
            TreeEntry[] c;

            if ((MODIFIED_ONLY & flags) == MODIFIED_ONLY && !IsModified)
                return;

            if ((LOADED_ONLY & flags) == LOADED_ONLY && !IsLoaded)
            {
                tv.StartVisitTree(this);
                tv.EndVisitTree(this);
                return;
            }

            EnsureLoaded();
            tv.StartVisitTree(this);

            if ((CONCURRENT_MODIFICATION & flags) == CONCURRENT_MODIFICATION)
                c = Members;
            else
                c = _contents;

            for (int k = 0; k < c.Length; k++)
                c[k].Accept(tv, flags);

            tv.EndVisitTree(this);
        }

        public FileTreeEntry AddFile(string name)
        {
            return AddFile(Gitty.Core.Repository.GitInternalSlash(Encoding.UTF8.GetBytes(name)), 0);
        }

        public FileTreeEntry AddFile(byte[] s, int offset)
        {
            int slash;
            int p;

            for (slash = offset; slash < s.Length && s[slash] != '/'; slash++)
            {
                // search for path component terminator
            }

            EnsureLoaded();
            byte xlast = (byte)(slash < s.Length ? '/' : 0);
            p = BinarySearch(_contents, s, xlast, offset, slash);
            if (p >= 0 && slash < s.Length && _contents[p] is Tree)
                return ((Tree)_contents[p]).AddFile(s, slash + 1);

            byte[] newName = SubString(s, offset, slash);
            if (p >= 0)
                throw new EntryExistsException(Encoding.UTF8.GetString(newName));
            else if (slash < s.Length)
            {
                Tree t = new Tree(this, newName);
                InsertEntry(p, t);
                return t.AddFile(s, slash + 1);
            }
            else
            {
                FileTreeEntry f = new FileTreeEntry(this, null, newName, false);
                InsertEntry(p, f);
                return f;
            }
        }

        public Tree AddTree(string name)
        {
            return AddTree(Repository.GitInternalSlash(Encoding.UTF8.GetBytes(name)), 0);
        }

        public Tree AddTree(byte[] s, int offset)
        {
            int slash;
            int p;

            for (slash = offset; slash < s.Length && s[slash] != '/'; slash++)
            {
                // search for path component terminator
            }

            EnsureLoaded();
            p = BinarySearch(_contents, s, (byte)'/', offset, slash);
            if (p >= 0 && slash < s.Length && _contents[p] is Tree)
                return ((Tree)_contents[p]).AddTree(s, slash + 1);

            byte[] newName = SubString(s, offset, slash);
            if (p >= 0)
                throw new EntryExistsException(Encoding.UTF8.GetString(newName));

            Tree t = new Tree(this, newName);
            InsertEntry(p, t);
            return slash == s.Length ? t : t.AddTree(s, slash + 1);
        }

        private void InsertEntry(int p, TreeEntry e)
        {
            TreeEntry[] c = _contents;
            TreeEntry[] n = new TreeEntry[c.Length + 1];
            p = -(p + 1);
            for (int k = c.Length - 1; k >= p; k--)
                n[k + 1] = c[k];
            n[p] = e;
            for (int k = p - 1; k >= 0; k--)
                n[k] = c[k];
            _contents = n;
            SetModified();
        }

        private void EnsureLoaded()
        {
            if (IsLoaded)
                return;

            ObjectLoader or = _db.OpenTree(this.Id);
            if (or == null)
                throw new MissingObjectException(this.Id, ObjectType.Tree);
            ReadTree(or.Bytes);
        }

        private void ReadTree(byte[] raw)
        {
            int rawSize = raw.Length;
            int rawPtr = 0;
            TreeEntry[] temp;
            int nextIndex = 0;

            while (rawPtr < rawSize)
            {
                while (rawPtr < rawSize && raw[rawPtr] != 0)
                    rawPtr++;
                rawPtr++;
                rawPtr += ObjectId.Constants.ObjectIdLength;
                nextIndex++;
            }

            temp = new TreeEntry[nextIndex];
            rawPtr = 0;
            nextIndex = 0;
            while (rawPtr < rawSize)
            {
                int c = raw[rawPtr++];
                if (c < '0' || c > '7')
                    throw new CorruptObjectException(this.Id, "invalid entry mode");
                int mode = c - '0';
                for (; ; )
                {
                    c = raw[rawPtr++];
                    if (' ' == c)
                        break;
                    else if (c < '0' || c > '7')
                        throw new CorruptObjectException(this.Id, "invalid mode");
                    mode <<= 3;
                    mode += c - '0';
                }

                int nameLen = 0;
                while (raw[rawPtr + nameLen] != 0)
                    nameLen++;
                byte[] name = new byte[nameLen];
                Array.Copy(raw, rawPtr, name, 0, nameLen);
                rawPtr += nameLen + 1;

                ObjectId id = ObjectId.FromRaw(raw, rawPtr);
                rawPtr += ObjectId.Constants.ObjectIdLength;

                TreeEntry ent;
                if (FileMode.RegularFile.Equals(mode))
                    ent = new FileTreeEntry(this, id, name, false);
                else if (FileMode.ExecutableFile.Equals(mode))
                    ent = new FileTreeEntry(this, id, name, true);
                else if (FileMode.Tree.Equals(mode))
                {
                    ent = new Tree(this, id, name);
                }
                else if (FileMode.Symlink.Equals(mode))
                    ent = new SymlinkTreeEntry(this, id, name);
                else
                    throw new CorruptObjectException(this.Id, "Invalid mode: "
                            + Convert.ToString(mode, 8));
                temp[nextIndex++] = ent;
            }

            _contents = temp;
        }

        #region Treeish Members
        public ObjectId TreeId
        {
            get
            {
                return Id;
            }
        }

        public Tree TreeEntry
        {
            get
            {
                return this;
            }
        }
        #endregion

        internal void RemoveEntry(TreeEntry e)
        {
            TreeEntry[] c = _contents;
            int p = BinarySearch(c, e.NameUTF8, Gitty.Core.TreeEntry.LastChar(e), 0, e.NameUTF8.Length);
            if (p >= 0)
            {
                TreeEntry[] n = new TreeEntry[c.Length - 1];
                for (int k = c.Length - 1; k > p; k--)
                    n[k - 1] = c[k];
                for (int k = p - 1; k >= 0; k--)
                    n[k] = c[k];
                _contents = n;
                SetModified();
            }
        }

        public void AddEntry(TreeEntry e)
        {
            int p;

            EnsureLoaded();
            p = BinarySearch(_contents, e.NameUTF8, Gitty.Core.TreeEntry.LastChar(e), 0, e.NameUTF8.Length);
            if (p < 0)
            {
                e.AttachParent(this);
                InsertEntry(p, e);
            }
            else
            {
                throw new EntryExistsException(e.Name);
            }
        }

        public bool IsLoaded
        {
            get
            {
                return _contents != null;
            }
        }

        public void Unload()
        {
            if (IsModified)
                throw new InvalidOperationException("Cannot unload a modified tree.");
            _contents = null;
        }

        public int MemberCount
        {
            get
            {
                EnsureLoaded();
                return _contents.Length;
            }
        }

        public TreeEntry[] Members
        {
            get
            {
                EnsureLoaded();
                TreeEntry[] c = _contents;
                if (c.Length != 0)
                {
                    TreeEntry[] r = new TreeEntry[c.Length];
                    for (int k = c.Length - 1; k >= 0; k--)
                        r[k] = c[k];
                    return r;
                }
                else
                    return c;
            }
        }

        private bool Exists(string s, byte slast)
        {
            return FindMember(s, slast) != null;
        }

        public bool ExistsTree(string path)
        {
            return Exists(path, (byte)'/');
        }

        /**
         * @param path
         * @return true if a blob or symlink with the specified name can be found
         *         under this tree.
         * @throws IOException
         */
        public bool ExistsBlob(string path)
        {
            return Exists(path, (byte)0);
        }

        private TreeEntry FindMember(string s, byte slast)
        {
            return FindMember(Repository.GitInternalSlash(Encoding.UTF8.GetBytes(s)), slast, 0);
        }


        private TreeEntry FindMember(byte[] s, byte slast, int offset)
        {
            int slash;
            int p;

            for (slash = offset; slash < s.Length && s[slash] != '/'; slash++)
            {
                // search for path component terminator
            }

            EnsureLoaded();
            byte xlast = slash < s.Length ? (byte)'/' : slast;
            p = BinarySearch(_contents, s, xlast, offset, slash);
            if (p >= 0)
            {
                TreeEntry r = _contents[p];
                if (slash < s.Length - 1)
                    return r is Tree ? ((Tree)r).FindMember(s, slast, slash + 1)
                            : null;
                return r;
            }
            return null;
        }


        /**
         * @param s
         *            blob name
         * @return a {@link TreeEntry} representing an object with the specified
         *         relative path.
         * @throws IOException
         */
        public TreeEntry FindBlobMember(string s)
        {
            return FindMember(s, (byte)0);
        }



        /**
         * @param s Tree Name
         * @return a Tree with the name s or null
         * @throws IOException
         */
        public TreeEntry findTreeMember(string s)
        {
            return FindMember(s, (byte)'/');
        }

        public override string ToString()
        {
            var r = new StringBuilder();
            r.Append(ObjectId.ToString(this.Id));
            r.Append(" T ");
            r.Append(FullName);
            return r.ToString();
        }
    }
}
