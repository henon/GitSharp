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
using GitSharp.Core;
using GitSharp.Core.RevWalk;
using GitSharp.Core.DirectoryCache;
using GitSharp.Core.TreeWalk.Filter;
using NUnit.Framework;
using GitSharp.Core.Util;

namespace GitSharp.Core.Tests.RevWalk
{

    public abstract class RevWalkTestCase : RepositoryTestCase
    {
        private TestRepository util;
        protected GitSharp.Core.RevWalk.RevWalk rw;

        [SetUp]
        public override void setUp()
        {
            base.setUp();
            util = new TestRepository(db, createRevWalk());
            rw = util.getRevWalk();
        }

        protected virtual GitSharp.Core.RevWalk.RevWalk createRevWalk()
        {
            return new GitSharp.Core.RevWalk.RevWalk(db);
        }

        protected DateTime getClock()
        {
            return util.getClock();
        }

        protected void Tick(int secDelta)
        {
            util.tick(secDelta);
        }

        protected RevBlob blob(string content)
        {
            return util.blob(content);
        }

        protected DirCacheEntry File(string path, RevBlob blob)
        {
            return util.file(path, blob);
        }

        protected RevTree tree(params DirCacheEntry[] entries)
        {
            return util.tree(entries);
        }

        protected RevObject get(RevTree tree, string path)
        {
            return util.get(tree, path);
        }

        protected RevCommit Commit(params RevCommit[] parents)
        {
            return util.commit(parents);
        }

        protected RevCommit Commit(RevTree tree, params RevCommit[] parents)
        {
            return util.commit(tree, parents);
        }

        protected RevCommit Commit(int secDelta, params RevCommit[] parents)
        {
            return util.commit(secDelta, parents);
        }

        protected RevCommit Commit(int secDelta, RevTree tree, params RevCommit[] parents)
        {
            return util.commit(secDelta, tree, parents);
        }

        protected RevTag Tag(string name, RevObject dst)
        {
            return util.tag(name, dst);
        }

        protected T parseBody<T>(T t) where T : RevObject
        {
            return util.parseBody(t);
        }

        protected void MarkStart(RevCommit commit)
        {
            rw.markStart(commit);
        }

        protected void MarkUninteresting(RevCommit commit)
        {
            rw.markUninteresting(commit);
        }

        protected static void AssertCommit(RevCommit exp, RevCommit act)
        {
            Assert.AreSame(exp, act);
        }
    }
}