/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Google Inc.
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
using System.Linq;
using System.Text;
using System.IO;
using GitSharp.Util;
using GitSharp.Exceptions;

namespace GitSharp.DirectoryCache
{


    /**
     * Single tree record from the 'TREE' {@link DirCache} extension.
     * <p>
     * A valid cache tree record contains the object id of a tree object and the
     * total number of {@link DirCacheEntry} instances (counted recursively) from
     * the DirCache contained within the tree. This information facilitates faster
     * traversal of the index and quicker generation of tree objects prior to
     * creating a new commit.
     * <p>
     * An invalid cache tree record indicates a known subtree whose file entries
     * have changed in ways that cause the tree to no longer have a known object id.
     * Invalid cache tree records must be revalidated prior to use.
     */
    public class DirCacheTree
    {
        private static byte[] NO_NAME = { };

        private static DirCacheTree[] NO_CHILDREN = { };

        private static Comparison<DirCacheTree> TREE_CMP = (o1, o2) =>
        {
            byte[] a = o1.encodedName;
            byte[] b = o2.encodedName;
            int aLen = a.Length;
            int bLen = b.Length;
            int cPos;
            for (cPos = 0; cPos < aLen && cPos < bLen; cPos++)
            {
                int cmp = (a[cPos] & 0xff) - (b[cPos] & (byte)0xff);
                if (cmp != 0)
                    return cmp;
            }
            if (aLen == bLen)
                return 0;
            if (aLen < bLen)
                return '/' - (b[cPos] & (byte)0xff);
            return (a[cPos] & (byte)0xff) - '/';
        };

        /** Tree this tree resides in; null if we are the root. */
        private DirCacheTree parent;

        /** Name of this tree within its parent. */
        private byte[] encodedName;

        /** Number of {@link DirCacheEntry} records that belong to this tree. */
        private int entrySpan;

        /** Unique SHA-1 of this tree; null if invalid. */
        private ObjectId id;

        /** Child trees, if any, sorted by {@link #encodedName}. */
        private DirCacheTree[] children;

        /** Number of valid children in {@link #children}. */
        private int childCnt;

        public DirCacheTree()
        {
            encodedName = NO_NAME;
            children = NO_CHILDREN;
            childCnt = 0;
            entrySpan = -1;
        }

        private DirCacheTree(DirCacheTree myParent, byte[] path, int pathOff, int pathLen)
        {
            parent = myParent;
            encodedName = new byte[pathLen];
            Array.Copy(path, pathOff, encodedName, 0, pathLen);
            children = NO_CHILDREN;
            childCnt = 0;
            entrySpan = -1;
        }

        public DirCacheTree(byte[] @in, MutableInteger off,
                 DirCacheTree myParent)
        {
            parent = myParent;

            int ptr = RawParseUtils.next(@in, off.value, (byte)'\0');
            int nameLen = ptr - off.value - 1;
            if (nameLen > 0)
            {
                encodedName = new byte[nameLen];
                Array.Copy(@in, off.value, encodedName, 0, nameLen);
            }
            else
                encodedName = NO_NAME;

            entrySpan = RawParseUtils.parseBase10(@in, ptr, off);
            int subcnt = RawParseUtils.parseBase10(@in, off.value, off);
            off.value = RawParseUtils.next(@in, off.value, (byte)'\n');

            if (entrySpan >= 0)
            {
                // Valid trees have a positive entry count and an id of a
                // tree object that should exist in the object database.
                //
                id = ObjectId.FromRaw(@in, off.value);
                off.value += Constants.OBJECT_ID_LENGTH;
            }

            if (subcnt > 0)
            {
                bool alreadySorted = true;
                children = new DirCacheTree[subcnt];
                for (int i = 0; i < subcnt; i++)
                {
                    children[i] = new DirCacheTree(@in, off, this);

                    // C Git's ordering differs from our own; it prefers to
                    // sort by Length first. This sometimes produces a sort
                    // we do not desire. On the other hand it may have been
                    // created by us, and be sorted the way we want.
                    //
                    if (alreadySorted && i > 0
                            && TREE_CMP(children[i - 1], children[i]) > 0)
                        alreadySorted = false;
                }
                if (!alreadySorted)
                    Array.Sort(children, TREE_CMP);
            }
            else
            {
                // Leaf level trees have no children, only (file) entries.
                //
                children = NO_CHILDREN;
            }
            childCnt = subcnt;
        }

        public void write(byte[] tmp, Stream os)
        {
            int ptr = tmp.Length;
            tmp[--ptr] = (byte)'\n';
            ptr = RawParseUtils.formatBase10(tmp, ptr, childCnt);
            tmp[--ptr] = (byte)' ';
            ptr = RawParseUtils.formatBase10(tmp, ptr, isValid() ? entrySpan : -1);
            tmp[--ptr] = 0;

            os.Write(encodedName, 0, encodedName.Length);
            os.Write(tmp, ptr, tmp.Length - ptr);
            if (isValid())
            {
                id.copyRawTo(tmp, 0);
                os.Write(tmp, 0, Constants.OBJECT_ID_LENGTH);
            }
            for (int i = 0; i < childCnt; i++)
                children[i].write(tmp, os);
        }

        /**
         * Determine if this cache is currently valid.
         * <p>
         * A valid cache tree knows how many {@link DirCacheEntry} instances from
         * the parent {@link DirCache} reside within this tree (recursively
         * enumerated). It also knows the object id of the tree, as the tree should
         * be readily available from the repository's object database.
         *
         * @return true if this tree is knows key details about itself; false if the
         *         tree needs to be regenerated.
         */
        public bool isValid()
        {
            return id != null;
        }

        /**
         * Get the number of entries this tree spans within the DirCache.
         * <p>
         * If this tree is not valid (see {@link #isValid()}) this method's return
         * value is always strictly negative (less than 0) but is otherwise an
         * undefined result.
         *
         * @return total number of entries (recursively) contained within this tree.
         */
        public int getEntrySpan()
        {
            return entrySpan;
        }

        /**
         * Get the number of cached subtrees contained within this tree.
         *
         * @return number of child trees available through this tree.
         */
        public int getChildCount()
        {
            return childCnt;
        }

        /**
         * Get the i-th child cache tree.
         *
         * @param i
         *            index of the child to obtain.
         * @return the child tree.
         */
        public DirCacheTree getChild(int i)
        {
            return children[i];
        }

        public ObjectId getObjectId()
        {
            return id;
        }

        /**
         * Get the tree's name within its parent.
         * <p>
         * This method is not very efficient and is primarily meant for debugging
         * and  output generation. Applications should try to avoid calling it,
         * and if invoked do so only once per interesting entry, where the name is
         * absolutely required for correct function.
         *
         * @return name of the tree. This does not contain any '/' characters.
         */
        public String getNameString()
        {
            return Constants.CHARSET.GetString(encodedName);
        }

        /**
         * Get the tree's path within the repository.
         * <p>
         * This method is not very efficient and is primarily meant for debugging
         * and  output generation. Applications should try to avoid calling it,
         * and if invoked do so only once per interesting entry, where the name is
         * absolutely required for correct function.
         *
         * @return path of the tree, relative to the repository root. If this is not
         *         the root tree the path ends with '/'. The root tree's path string
         *         is the empty string ("").
         */
        public String getPathString()
        {
            StringBuilder r = new StringBuilder();
            appendName(r);
            return r.ToString();
        }

        /**
         * Write (if necessary) this tree to the object store.
         *
         * @param cache
         *            the complete cache from DirCache.
         * @param cIdx
         *            first position of <code>cache</code> that is a member of this
         *            tree. The path of <code>cache[cacheIdx].path</code> for the
         *            range <code>[0,pathOff-1)</code> matches the complete path of
         *            this tree, from the root of the repository.
         * @param pathOffset
         *            number of bytes of <code>cache[cacheIdx].path</code> that
         *            matches this tree's path. The value at array position
         *            <code>cache[cacheIdx].path[pathOff-1]</code> is always '/' if
         *            <code>pathOff</code> is > 0.
         * @param ow
         *            the writer to use when serializing to the store.
         * @return identity of this tree.
         * @throws UnmergedPathException
         *             one or more paths contain higher-order stages (stage > 0),
         *             which cannot be stored in a tree object.
         * @throws IOException
         *             an unexpected error occurred writing to the object store.
         */
        public ObjectId writeTree(DirCacheEntry[] cache, int cIdx, int pathOffset, ObjectWriter ow)
        {
            if (id == null)
            {
                int endIdx = cIdx + entrySpan;
                int size = computeSize(cache, cIdx, pathOffset, ow);
                var @out = new MemoryStream(size);
                int childIdx = 0;
                int entryIdx = cIdx;

                while (entryIdx < endIdx)
                {
                    DirCacheEntry e = cache[entryIdx];
                    byte[] ep = e.path;
                    if (childIdx < childCnt)
                    {
                        DirCacheTree st = children[childIdx];
                        if (st.contains(ep, pathOffset, ep.Length))
                        {
                            FileMode.Tree.CopyTo(@out);
                            @out.Write(new byte[] { (byte)' ' }, 0, 1);
                            @out.Write(st.encodedName, 0, st.encodedName.Length);
                            @out.Write(new byte[] { (byte)0 }, 0, 1);
                            st.id.copyRawTo(@out);

                            entryIdx += st.entrySpan;
                            childIdx++;
                            continue;
                        }
                    }

                    e.getFileMode().CopyTo(@out);
                    @out.Write(new byte[] { (byte)' ' }, 0, 1);
                    @out.Write(ep, pathOffset, ep.Length - pathOffset);
                    @out.Write(new byte[] { 0 }, 0, 1);
                    @out.Write(e.idBuffer(), e.idOffset(), Constants.OBJECT_ID_LENGTH);
                    entryIdx++;
                }

                id = ow.WriteCanonicalTree(@out.ToArray());
            }
            return id;
        }

        private int computeSize(DirCacheEntry[] cache, int cIdx, int pathOffset, ObjectWriter ow)
        {
            int endIdx = cIdx + entrySpan;
            int childIdx = 0;
            int entryIdx = cIdx;
            int size = 0;

            while (entryIdx < endIdx)
            {
                DirCacheEntry e = cache[entryIdx];
                if (e.getStage() != 0)
                    throw new UnmergedPathException(e);

                byte[] ep = e.path;
                if (childIdx < childCnt)
                {
                    DirCacheTree st = children[childIdx];
                    if (st.contains(ep, pathOffset, ep.Length))
                    {
                        int stOffset = pathOffset + st.nameLength() + 1;
                        st.writeTree(cache, entryIdx, stOffset, ow);

                        size += FileMode.Tree.copyToLength();
                        size += st.nameLength();
                        size += Constants.OBJECT_ID_LENGTH + 2;

                        entryIdx += st.entrySpan;
                        childIdx++;
                        continue;
                    }
                }

                FileMode mode = e.getFileMode();
                if ((int)mode.ObjectType == Constants.OBJ_BAD)
                    throw new InvalidOperationException("Entry \"" + e.getPathString() + "\" has incorrect mode set up.");


                size += mode.copyToLength();
                size += ep.Length - pathOffset;
                size += Constants.OBJECT_ID_LENGTH + 2;
                entryIdx++;
            }

            return size;
        }

        private void appendName(StringBuilder r)
        {
            if (parent != null)
            {
                parent.appendName(r);
                r.Append(getNameString());
                r.Append('/');
            }
            else if (nameLength() > 0)
            {
                r.Append(getNameString());
                r.Append('/');
            }
        }

        public int nameLength()
        {
            return encodedName.Length;
        }

        public bool contains(byte[] a, int aOff, int aLen)
        {
            byte[] e = encodedName;
            int eLen = e.Length;
            for (int eOff = 0; eOff < eLen && aOff < aLen; eOff++, aOff++)
                if (e[eOff] != a[aOff])
                    return false;
            if (aOff == aLen)
                return false;
            return a[aOff] == '/';
        }

        /**
         * Update (if necessary) this tree's entrySpan.
         *
         * @param cache
         *            the complete cache from DirCache.
         * @param cCnt
         *            number of entries in <code>cache</code> that are valid for
         *            iteration.
         * @param cIdx
         *            first position of <code>cache</code> that is a member of this
         *            tree. The path of <code>cache[cacheIdx].path</code> for the
         *            range <code>[0,pathOff-1)</code> matches the complete path of
         *            this tree, from the root of the repository.
         * @param pathOff
         *            number of bytes of <code>cache[cacheIdx].path</code> that
         *            matches this tree's path. The value at array position
         *            <code>cache[cacheIdx].path[pathOff-1]</code> is always '/' if
         *            <code>pathOff</code> is > 0.
         */
        public void validate(DirCacheEntry[] cache, int cCnt, int cIdx,
                 int pathOff)
        {
            if (entrySpan >= 0)
            {
                // If we are valid, our children are also valid.
                // We have no need to validate them.
                //
                return;
            }

            entrySpan = 0;
            if (cCnt == 0)
            {
                // Special case of an empty index, and we are the root tree.
                //
                return;
            }

            byte[] firstPath = cache[cIdx].path;
            int stIdx = 0;
            while (cIdx < cCnt)
            {
                byte[] currPath = cache[cIdx].path;
                if (pathOff > 0 && !peq(firstPath, currPath, pathOff))
                {
                    // The current entry is no longer in this tree. Our
                    // span is updated and the remainder goes elsewhere.
                    //
                    break;
                }

                DirCacheTree st = stIdx < childCnt ? children[stIdx] : null;
                int cc = namecmp(currPath, pathOff, st);
                if (cc > 0)
                {
                    // This subtree is now empty.
                    //
                    removeChild(stIdx);
                    continue;
                }

                if (cc < 0)
                {
                    int p = slash(currPath, pathOff);
                    if (p < 0)
                    {
                        // The entry has no '/' and thus is directly in this
                        // tree. Count it as one of our own.
                        //
                        cIdx++;
                        entrySpan++;
                        continue;
                    }

                    // Build a new subtree for this entry.
                    //
                    st = new DirCacheTree(this, currPath, pathOff, p - pathOff);
                    insertChild(stIdx, st);
                }

                // The entry is contained in this subtree.
                //
                st.validate(cache, cCnt, cIdx, pathOff + st.nameLength() + 1);
                cIdx += st.entrySpan;
                entrySpan += st.entrySpan;
                stIdx++;
            }

            if (stIdx < childCnt)
            {
                // None of our remaining children can be in this tree
                // as the current cache entry is after our own name.
                //
                DirCacheTree[] dct = new DirCacheTree[stIdx];
                Array.Copy(children, 0, dct, 0, stIdx);
                children = dct;
            }
        }

        private void insertChild(int stIdx, DirCacheTree st)
        {
            DirCacheTree[] c = children;
            if (childCnt + 1 <= c.Length)
            {
                if (stIdx < childCnt)
                    Array.Copy(c, stIdx, c, stIdx + 1, childCnt - stIdx);
                c[stIdx] = st;
                childCnt++;
                return;
            }

            int n = c.Length;
            DirCacheTree[] a = new DirCacheTree[n + 1];
            if (stIdx > 0)
                Array.Copy(c, 0, a, 0, stIdx);
            a[stIdx] = st;
            if (stIdx < n)
                Array.Copy(c, stIdx, a, stIdx + 1, n - stIdx);
            children = a;
            childCnt++;
        }

        private void removeChild(int stIdx)
        {
            int n = --childCnt;
            if (stIdx < n)
                Array.Copy(children, stIdx + 1, children, stIdx, n - stIdx);
            children[n] = null;
        }

        public static bool peq(byte[] a, byte[] b, int aLen)
        {
            if (b.Length < aLen)
                return false;
            for (aLen--; aLen >= 0; aLen--)
                if (a[aLen] != b[aLen])
                    return false;
            return true;
        }

        private static int namecmp(byte[] a, int aPos, DirCacheTree ct)
        {
            if (ct == null)
                return -1;
            byte[] b = ct.encodedName;
            int aLen = a.Length;
            int bLen = b.Length;
            int bPos = 0;
            for (; aPos < aLen && bPos < bLen; aPos++, bPos++)
            {
                int cmp = (a[aPos] & 0xff) - (b[bPos] & 0xff);
                if (cmp != 0)
                    return cmp;
            }
            if (bPos == bLen)
                return a[aPos] == '/' ? 0 : -1;
            return aLen - bLen;
        }

        private static int slash(byte[] a, int aPos)
        {
            int aLen = a.Length;
            for (; aPos < aLen; aPos++)
                if (a[aPos] == '/')
                    return aPos;
            return -1;
        }
    }
}
