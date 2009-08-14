/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2008, Robin Rosenberg
 * Copyright (C) 2009, Dan Rigby <dan@danrigby.com>
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

using System.IO;
using GitSharp.DirectoryCache;
using GitSharp.Merge;
using NUnit.Framework;

using System;

namespace GitSharp.Tests.Merge
{
    [TestFixture]
    public class CherryPickTest : RepositoryTestCase
    {
        [Test]
	    public void TestPick()
        {
		    // B---O
		    // \----P---T
		    //
		    // Cherry-pick "T" onto "O". This shouldn't introduce "p-fail", which
		    // was created by "P", nor should it modify "a", which was done by "P".
		    //
		    DirCache treeB = DirCache.read(db);
		    DirCache treeO = DirCache.read(db);
		    DirCache treeP = DirCache.read(db);
		    DirCache treeT = DirCache.read(db);
		    {
			    DirCacheBuilder b = treeB.builder();
			    DirCacheBuilder o = treeO.builder();
			    DirCacheBuilder p = treeP.builder();
			    DirCacheBuilder t = treeT.builder();

			    b.add(MakeEntry("a", FileMode.RegularFile));

                o.add(MakeEntry("a", FileMode.RegularFile));
                o.add(MakeEntry("o", FileMode.RegularFile));

                p.add(MakeEntry("a", FileMode.RegularFile, "q"));
                p.add(MakeEntry("p-fail", FileMode.RegularFile));

                t.add(MakeEntry("a", FileMode.RegularFile));
                t.add(MakeEntry("t", FileMode.RegularFile));

			    b.finish();
			    o.finish();
			    p.finish();
			    t.finish();
		    }

		    ObjectWriter ow = new ObjectWriter(db);
		    ObjectId B = Commit(ow, treeB, new ObjectId[] {});
		    ObjectId O = Commit(ow, treeO, new[] { B });
		    ObjectId P = Commit(ow, treeP, new[] { B });
		    ObjectId T = Commit(ow, treeT, new[] { P });

		    ThreeWayMerger twm = (ThreeWayMerger) MergeStrategy.SimpleTwoWayInCore.NewMerger(db);
		    twm.SetBase(P);
		    bool merge = twm.Merge(new[] { O, T });
	        Assert.IsTrue(merge);

            GitSharp.TreeWalk.TreeWalk tw = new GitSharp.TreeWalk.TreeWalk(db);
		    tw.setRecursive(true);
		    tw.reset(twm.GetResultTreeId());

		    Assert.IsTrue(tw.next());
		    Assert.Equals("a", tw.getPathString());
		    AssertCorrectId(treeO, tw);

		    Assert.IsTrue(tw.next());
		    Assert.Equals("o", tw.getPathString());
		    AssertCorrectId(treeO, tw);

		    Assert.IsTrue(tw.next());
		    Assert.Equals("t", tw.getPathString());
		    AssertCorrectId(treeT, tw);

		    Assert.IsFalse(tw.next());
	    }

        private void AssertCorrectId(DirCache treeT, GitSharp.TreeWalk.TreeWalk tw) 
        {
		    Assert.Equals(treeT.getEntry(tw.getPathString()).getObjectId(), tw.getObjectId(0));
	    }

	    private ObjectId Commit(ObjectWriter ow, DirCache treeB, ObjectId[] parentIds)
        {
		    Commit c = new Commit(db);
		    c.TreeId = treeB.writeTree(ow);
		    c.Author = new PersonIdent("A U Thor", "a.u.thor", 1L, 0);
		    c.Committer = c.Author;
		    c.ParentIds = parentIds;
		    c.Message = "Tree " + c.TreeId.Name;
		    return ow.WriteCommit(c);
	    }

	    private DirCacheEntry MakeEntry(String path, FileMode mode)
        {
		    return MakeEntry(path, mode, path);
	    }

	    private DirCacheEntry MakeEntry(String path, FileMode mode, String content)
        {
		    DirCacheEntry ent = new DirCacheEntry(path);
		    ent.setFileMode(mode);
		    byte[] contentBytes = Constants.encode(content);
		    ent.setObjectId(new ObjectWriter(db).ComputeBlobSha1(contentBytes.Length, new MemoryStream(contentBytes)));
		    return ent;
	    }
    }
}
