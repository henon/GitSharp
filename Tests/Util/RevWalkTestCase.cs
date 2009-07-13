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
using System.Linq;
using System.Text;
using GitSharp.RevWalk;

namespace GitSharp.Tests.Util
{
    /** Support for tests of the {@link RevWalk} class. */
    public abstract class RevWalkTestCase : RepositoryTestCase
    {
        protected ObjectWriter ow;

        protected RevTree emptyTree;

        protected long nowTick; // [henon] this are seconds in git internal time representaiton

        protected RevWalk.RevWalk rw;

        public void setUp()
        {
            base.setUp();
            ow = new ObjectWriter(db);
            rw = createRevWalk();
            emptyTree = rw.parseTree(ow.writeTree(new Tree(db)));
            nowTick = 1236977987L;
        }

        protected RevWalk createRevWalk()
        {
            return new RevWalk(db);
        }

        protected void tick(int secDelta)
        {
            nowTick += secDelta;
        }

        protected RevBlob blob(String content)
        {
            return rw.lookupBlob(ow.writeBlob(content.getBytes(Constants.CHARACTER_ENCODING)));
        }

        protected DirCacheEntry file(String path, RevBlob blob)
        {
            DirCacheEntry e = new DirCacheEntry(path);
            e.setFileMode(FileMode.REGULAR_FILE);
            e.setObjectId(blob);
            return e;
        }

        protected RevTree tree(params DirCacheEntry[] entries)
        {
            DirCache dc = DirCache.newInCore();
            DirCacheBuilder b = dc.builder();
            foreach (DirCacheEntry e in entries)
                b.add(e);
            b.finish();
            return rw.lookupTree(dc.writeTree(ow));
        }

        protected RevObject get(RevTree tree, String path)
        {
            TreeWalk tw = new TreeWalk(db);
            tw.setFilter(PathFilterGroup.createFromStrings(Collections
                    .singleton(path)));
            tw.reset(tree);
            while (tw.next())
            {
                if (tw.isSubtree() && !path.equals(tw.getPathString()))
                {
                    tw.enterSubtree();
                    continue;
                }
                ObjectId entid = tw.getObjectId(0);
                FileMode entmode = tw.getFileMode(0);
                return rw.lookupAny(entid, entmode.getObjectType());
            }
            fail("Can't find " + path + " in tree " + tree.name());
            return null; // never reached.
        }

        protected RevCommit commit(params RevCommit[] parents)
        {
            return commit(1, emptyTree, parents);
        }

        protected RevCommit commit(RevTree tree, params RevCommit[] parents)
        {
            return commit(1, tree, parents);
        }

        protected RevCommit commit(int secDelta, params RevCommit[] parents)
        {
            return commit(secDelta, emptyTree, parents);
        }

        protected RevCommit commit(int secDelta, RevTree tree, params RevCommit[] parents)
        {
            tick(secDelta);
            Commit c = new Commit(db);
            c.setTreeId(tree);
            c.setParentIds(parents);
            c.setAuthor(new PersonIdent(jauthor, new Date(nowTick)));
            c.setCommitter(new PersonIdent(jcommitter, new Date(nowTick)));
            c.setMessage("");
            return rw.lookupCommit(ow.writeCommit(c));
        }

        protected RevTag tag(String name, RevObject dst)
        {
            Tag t = new Tag(db);
            t.setType(Constants.typeString(dst.getType()));
            t.setObjId(dst.toObjectId());
            t.setTag(name);
            t.setTagger(new PersonIdent(jcommitter, new Date(nowTick)));
            t.setMessage("");
            return (RevTag)rw.lookupAny(ow.writeTag(t), Constants.OBJ_TAG);
        }

        protected T parse<T>(T t)
            where T : RevObject
        {
            rw.parse(t);
            return t;
        }

        protected void markStart(RevCommit commit)
        {
            rw.markStart(commit);
        }

        protected void markUninteresting(RevCommit commit)
        {
            rw.markUninteresting(commit);
        }

        protected void assertCommit(RevCommit exp, RevCommit act)
        {
            assertSame(exp, act);
        }
    }
}
