/*
 * Copyright (C) 2009, 2010, Google Inc.
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 *
 * - Neither the name of the Eclipse Foundation, Inc. nor the names of its
 * contributors may be used to endorse or promote products derived from this
 * software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */


/* Wrapper to make creating test data easier. */

using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Core;
using GitSharp.Core.DirectoryCache;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.TreeWalk;
using GitSharp.Core.TreeWalk.Filter;
using GitSharp.Core.Util;
using NUnit.Framework;
using FileMode = GitSharp.Core.FileMode;
using CoreRevWalk = GitSharp.Core.RevWalk.RevWalk;

namespace GitSharp.Tests.GitSharp.Core.Util
{
    public class TestRepository
    {
        private static PersonIdent author;

        private static PersonIdent committer;

        static TestRepository()
        {
            MockSystemReader m = new MockSystemReader();
            long now = m.getCurrentTime();
            int tz = m.getTimezone(now);

            String an = "J. Author";
            String ae = "jauthor@example.com";
            author = new PersonIdent(an, ae, now, tz);

            String cn = "J. Committer";
            String ce = "jcommitter@example.com";
            committer = new PersonIdent(cn, ce, now, tz);
        }

        private global::GitSharp.Core.Repository db;

        private CoreRevWalk pool;

        private ObjectWriter writer;

        private long now;

        /**
     * Wrap a repository with test building tools.
     *
     * @param db
     *            the test repository to write into.
     * @throws Exception
     */
        public TestRepository(global::GitSharp.Core.Repository db)
            : this(db, new CoreRevWalk(db))
        {
        }

        /**
     * Wrap a repository with test building tools.
     *
     * @param db
     *            the test repository to write into.
     * @param rw
     *            the RevObject pool to use for object lookup.
     * @throws Exception
     */
        public TestRepository(global::GitSharp.Core.Repository db, CoreRevWalk rw)
        {
            this.db = db;
            this.pool = rw;
            this.writer = new ObjectWriter(db);
            this.now = 1236977987000L;
        }

        /** @return the repository this helper class operates against. */
        public global::GitSharp.Core.Repository getRepository()
        {
            return db;
        }

        /** @return get the RevWalk pool all objects are allocated through. */
        public CoreRevWalk getRevWalk()
        {
            return pool;
        }

        /** @return current time adjusted by {@link #tick(int)}. */
        public DateTime getClock()
        {
            return now.MillisToUtcDateTime();
        }

        /**
     * Adjust the current time that will used by the next commit.
     *
     * @param secDelta
     *            number of seconds to add to the current time.
     */
        public void tick(int secDelta)
        {
            now += secDelta * 1000L;
        }

        /**
     * Create a new blob object in the repository.
     *
     * @param content
     *            file content, will be UTF-8 encoded.
     * @return reference to the blob.
     * @throws Exception
     */
        public RevBlob blob(String content)
        {
            return blob(content.getBytes("UTF-8"));
        }

        /**
     * Create a new blob object in the repository.
     *
     * @param content
     *            binary file content.
     * @return reference to the blob.
     * @throws Exception
     */
        public RevBlob blob(byte[] content)
        {
            return pool.lookupBlob(writer.WriteBlob(content));
        }

        /**
     * Construct a regular file mode tree entry.
     *
     * @param path
     *            path of the file.
     * @param blob
     *            a blob, previously constructed in the repository.
     * @return the entry.
     * @throws Exception
     */
        public DirCacheEntry file(String path, RevBlob blob)
        {
            DirCacheEntry e = new DirCacheEntry(path);
            e.setFileMode(FileMode.RegularFile);
            e.setObjectId(blob);
            return e;
        }

        /**
     * Construct a tree from a specific listing of file entries.
     *
     * @param entries
     *            the files to include in the tree. The collection does not need
     *            to be sorted properly and may be empty.
     * @return reference to the tree specified by the entry list.
     * @throws Exception
     */
        public RevTree tree(params DirCacheEntry[] entries)
        {
            DirCache dc = DirCache.newInCore();
            DirCacheBuilder b = dc.builder();
            foreach (DirCacheEntry e in entries)
                b.add(e);
            b.finish();
            return pool.lookupTree(dc.writeTree(writer));
        }

        /**
     * Lookup an entry stored in a tree, failing if not present.
     *
     * @param tree
     *            the tree to search.
     * @param path
     *            the path to find the entry of.
     * @return the parsed object entry at this path, never null.
     * @throws AssertionFailedError
     *             if the path does not exist in the given tree.
     * @throws Exception
     */
        public RevObject get(RevTree tree, String path)
        {
            TreeWalk tw = new TreeWalk(db);
            tw.setFilter(PathFilterGroup.createFromStrings(new[] { path }));
            tw.reset(tree);
            while (tw.next())
            {
                if (tw.isSubtree() && !path.Equals(tw.getPathString()))
                {
                    tw.enterSubtree();
                    continue;
                }
                ObjectId entid = tw.getObjectId(0);
                FileMode entmode = tw.getFileMode(0);
                return pool.lookupAny(entid, (int)entmode.ObjectType);
            }
            Assert.Fail("Can't find " + path + " in tree " + tree.Name);
            return null; // never reached.
        }

        /*
     * Create a new commit.
     * <p>
     * See {@link #commit(int, RevTree, RevCommit...)}. The tree is the empty
     * tree (no files or subdirectories).
     *
     * @param parents
     *            zero or more parents of the commit.
     * @return the new commit.
     * @throws Exception
     */
        public RevCommit commit(params RevCommit[] parents)
        {
            return commit(1, tree(), parents);
        }

        /*
     * Create a new commit.
     * <p>
     * See {@link #commit(int, RevTree, RevCommit...)}.
     *
     * @param tree
     *            the root tree for the commit.
     * @param parents
     *            zero or more parents of the commit.
     * @return the new commit.
     * @throws Exception
     */
        public RevCommit commit(RevTree tree, params RevCommit[] parents)
        {
            return commit(1, tree, parents);
        }

        /*
     * Create a new commit.
     * <p>
     * See {@link #commit(int, RevTree, RevCommit...)}. The tree is the empty
     * tree (no files or subdirectories).
     *
     * @param secDelta
     *            number of seconds to advance {@link #tick(int)} by.
     * @param parents
     *            zero or more parents of the commit.
     * @return the new commit.
     * @throws Exception
     */
        public RevCommit commit(int secDelta, params RevCommit[] parents)
        {
            return commit(secDelta, tree(), parents);
        }

        /*
     * Create a new commit.
     * <p>
     * The author and committer identities are stored using the current
     * timestamp, after being incremented by {@code secDelta}. The message body
     * is empty.
     *
     * @param secDelta
     *            number of seconds to advance {@link #tick(int)} by.
     * @param tree
     *            the root tree for the commit.
     * @param parents
     *            zero or more parents of the commit.
     * @return the new commit.
     * @throws Exception
     */
        public RevCommit commit(int secDelta, RevTree tree,
                                params RevCommit[] parents)
        {
            tick(secDelta);

            global::GitSharp.Core.Commit c = new global::GitSharp.Core.Commit(db);
            c.TreeId = (tree);
            c.ParentIds = (parents);
            c.Author = (new PersonIdent(author, now.MillisToUtcDateTime()));
            c.Committer = (new PersonIdent(committer, now.MillisToUtcDateTime()));
            c.Message = ("");
            return pool.lookupCommit(writer.WriteCommit(c));
        }

        /* @return a new commit builder. */
        public CommitBuilder commit()
        {
            return new CommitBuilder(this);
        }

        /*
     * Construct an annotated tag object pointing at another object.
     * <p>
     * The tagger is the committer identity, at the current time as specified by
     * {@link #tick(int)}. The time is not increased.
     * <p>
     * The tag message is empty.
     *
     * @param name
     *            name of the tag. Traditionally a tag name should not start
     *            with {@code refs/tags/}.
     * @param dst
     *            object the tag should be pointed at.
     * @return the annotated tag object.
     * @throws Exception
     */
        public RevTag tag(String name, RevObject dst)
        {
            global::GitSharp.Core.Tag t = new global::GitSharp.Core.Tag(db);
            t.TagType = (Constants.typeString(dst.Type));
            t.Id = (dst.ToObjectId());
            t.TagName = (name);
            t.Tagger = (new PersonIdent(committer, now.MillisToUtcDateTime()));
            t.Message = ("");
            return (RevTag)pool.lookupAny(writer.WriteTag(t), Constants.OBJ_TAG);
        }

        /**
     * Update a reference to point to an object.
     *
     * @param ref
     *            the name of the reference to update to. If {@code ref} does
     *            not start with {@code refs/} and is not the magic names
     *            {@code HEAD} {@code FETCH_HEAD} or {@code MERGE_HEAD}, then
     *            {@code refs/heads/} will be prefixed in front of the given
     *            name, thereby assuming it is a branch.
     * @param to
     *            the target object.
     * @return the target object.
     * @throws Exception
     */
        public RevCommit update(String @ref, CommitBuilder to)
        {
            return update(@ref, to.create());
        }

        /*
     * Update a reference to point to an object.
     *
     * @param <T>
     *            type of the target object.
     * @param ref
     *            the name of the reference to update to. If {@code ref} does
     *            not start with {@code refs/} and is not the magic names
     *            {@code HEAD} {@code FETCH_HEAD} or {@code MERGE_HEAD}, then
     *            {@code refs/heads/} will be prefixed in front of the given
     *            name, thereby assuming it is a branch.
     * @param obj
     *            the target object.
     * @return the target object.
     * @throws Exception
     */
        public T update<T>(String @ref, T obj) where T : ObjectId
        {
            if (Constants.HEAD.Equals(@ref))
            {
            }
            else if ("FETCH_HEAD".Equals(@ref))
            {
            }
            else if ("MERGE_HEAD".Equals(@ref))
            {
            }
            else if (@ref.StartsWith(Constants.R_REFS))
            {
            }
            else
                @ref = Constants.R_HEADS + @ref;

            RefUpdate u = db.UpdateRef(@ref);
            u.NewObjectId = (obj);
            switch (u.forceUpdate())
            {
                case RefUpdate.RefUpdateResult.FAST_FORWARD:
                case RefUpdate.RefUpdateResult.FORCED:
                case RefUpdate.RefUpdateResult.NEW:
                case RefUpdate.RefUpdateResult.NO_CHANGE:
                    updateServerInfo();
                    return obj;

                default:
                    throw new IOException("Cannot write " + @ref + " " + u.Result);
            }
        }

        public class MockRefWriter : RefWriter
        {
            private readonly global::GitSharp.Core.Repository _db;

            public MockRefWriter(global::GitSharp.Core.Repository db, IEnumerable<global::GitSharp.Core.Ref> refs)
                : base(refs)
            {
                _db = db;
            }

            protected override void writeFile(string file, byte[] content)
            {
                FileInfo p = PathUtil.CombineFilePath(_db.Directory, file);
                LockFile lck = new LockFile(p);
                if (!lck.Lock())
                    throw new ObjectWritingException("Can't write " + p);
                try
                {
                    lck.Write(content);
                }
                catch (IOException)
                {
                    throw new ObjectWritingException("Can't write " + p);
                }
                if (!lck.Commit())
                    throw new ObjectWritingException("Can't write " + p);
            }
        }

        /*
     * Update the dumb client server info files.
     *
     * @throws Exception
     */
        public void updateServerInfo()
        {
            if (db.ObjectDatabase is ObjectDirectory)
            {
                RefWriter rw = new MockRefWriter(db, db.getAllRefs().Values);
                rw.writePackedRefs();
                rw.writeInfoRefs();
            }
        }

        /*
     * Ensure the body of the given object has been parsed.
     *
     * @param <T>
     *            type of object, e.g. {@link RevTag} or {@link RevCommit}.
     * @param object
     *            reference to the (possibly unparsed) object to force body
     *            parsing of.
     * @return {@code object}
     * @throws Exception
     */
        public T parseBody<T>(T @object) where T : RevObject
        {
            pool.parseBody(@object);
            return @object;
        }

        /*
     * Create a new branch builder for this repository.
     *
     * @param ref
     *            name of the branch to be constructed. If {@code ref} does not
     *            start with {@code refs/} the prefix {@code refs/heads/} will
     *            be added.
     * @return builder for the named branch.
     */
        public BranchBuilder branch(String @ref)
        {
            if (Constants.HEAD.Equals(@ref))
            {
            }
            else if (@ref.StartsWith(Constants.R_REFS))
            {
            }
            else
                @ref = Constants.R_HEADS + @ref;
            return new BranchBuilder(this, @ref);
        }

        /** Helper to build a branch with one or more commits */
        public class BranchBuilder
        {
            private readonly TestRepository _testRepository;
            public String @ref;

            public BranchBuilder(TestRepository testRepository, String @ref)
            {
                _testRepository = testRepository;
                this.@ref = @ref;
            }

            /**
         * @return construct a new commit builder that updates this branch. If
         *         the branch already exists, the commit builder will have its
         *         first parent as the current commit and its tree will be
         *         initialized to the current files.
         * @throws Exception
         *             the commit builder can't read the current branch state
         */
            public CommitBuilder commit()
            {
                return new CommitBuilder(_testRepository);
            }

            /**
         * Forcefully update this branch to a particular commit.
         *
         * @param to
         *            the commit to update to.
         * @return {@code to}.
         * @throws Exception
         */
            public RevCommit update(CommitBuilder to)
            {
                return update(to.create());
            }

            /**
         * Forcefully update this branch to a particular commit.
         *
         * @param to
         *            the commit to update to.
         * @return {@code to}.
         * @throws Exception
         */
            public RevCommit update(RevCommit to)
            {
                return _testRepository.update(@ref, to);
            }
        }

        /** Helper to generate a commit. */
        public class CommitBuilder
        {
            private readonly TestRepository _testRepository;
            private BranchBuilder branch;

            private DirCache tree = DirCache.newInCore();

            private List<RevCommit> parents = new List<RevCommit>();

            private int _tick = 1;

            private String _message = "";

            private RevCommit self;

            public CommitBuilder(TestRepository testRepository)
            {
                _testRepository = testRepository;
                branch = null;
            }

            CommitBuilder(TestRepository testRepository, BranchBuilder b)
            {
                _testRepository = testRepository;
                branch = b;

                global::GitSharp.Core.Ref @ref = _testRepository.db.getRef(branch.@ref);
                if (@ref != null)
                {
                    parent(_testRepository.pool.parseCommit(@ref.ObjectId));
                }
            }

            CommitBuilder(TestRepository testRepository, CommitBuilder prior)
            {
                _testRepository = testRepository;
                branch = prior.branch;

                DirCacheBuilder b = tree.builder();
                for (int i = 0; i < prior.tree.getEntryCount(); i++)
                    b.add(prior.tree.getEntry(i));
                b.finish();

                parents.Add(prior.create());
            }

            public CommitBuilder parent(RevCommit p)
            {
                if (parents.isEmpty())
                {
                    DirCacheBuilder b = tree.builder();
                    _testRepository.parseBody(p);
                    b.addTree(new byte[0], DirCacheEntry.STAGE_0, _testRepository.db, p.Tree);
                    b.finish();
                }
                parents.Add(p);
                return this;
            }

            public CommitBuilder noParents()
            {
                parents.Clear();
                return this;
            }

            public CommitBuilder noFiles()
            {
                tree.clear();
                return this;
            }

            public CommitBuilder add(String path, String content)
            {
                return add(path, _testRepository.blob(content));
            }

            public class MockPathEdit : DirCacheEditor.PathEdit
            {
                private readonly RevBlob _id;

                public MockPathEdit(RevBlob id, string entryPath)
                    : base(entryPath)
                {
                    _id = id;
                }

                public override void Apply(DirCacheEntry ent)
                {
                    ent.setFileMode(FileMode.RegularFile);
                    ent.setObjectId(_id);
                }
            }

            public CommitBuilder add(String path, RevBlob id)
            {
                DirCacheEditor e = tree.editor();
                e.add(new MockPathEdit(id, path));
                e.finish();
                return this;
            }

            public CommitBuilder rm(String path)
            {
                DirCacheEditor e = tree.editor();
                e.add(new DirCacheEditor.DeletePath(path));
                e.add(new DirCacheEditor.DeleteTree(path));
                e.finish();
                return this;
            }

            public CommitBuilder message(String m)
            {
                _message = m;
                return this;
            }

            public CommitBuilder tick(int secs)
            {
                _tick = secs;
                return this;
            }

            public RevCommit create()
            {
                if (self == null)
                {
                    _testRepository.tick(_tick);

                    global::GitSharp.Core.Commit c = new global::GitSharp.Core.Commit(_testRepository.db);
                    c.TreeId = (_testRepository.pool.lookupTree(tree.writeTree(_testRepository.writer)));
                    c.ParentIds = (parents.ToArray());
                    c.Author = (new PersonIdent(author, _testRepository.now.MillisToUtcDateTime()));
                    c.Committer = (new PersonIdent(committer, _testRepository.now.MillisToUtcDateTime()));
                    c.Message = (_message);

                    self = _testRepository.pool.lookupCommit(_testRepository.writer.WriteCommit((c)));

                    if (branch != null)
                        branch.update(self);
                }
                return self;
            }

            public CommitBuilder child()
            {
                return new CommitBuilder(_testRepository, this);
            }
        }
    }
}


