/*
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

namespace GitSharp.Tests.DirectoryCache
{
    using NUnit.Framework;
    using System.Collections.Generic;
    using GitSharp.DirectoryCache;
    using TreeWalk = GitSharp.TreeWalk.TreeWalk;
    using System.Text;
    using GitSharp.Util;

    [TestFixture]
    public class DirCacheCGitCompatabilityTest : RepositoryTestCase
    {

        private System.IO.FileInfo index = new System.IO.FileInfo("Resources/gitgit.index");

        [Test]
        public void testReadIndex_LsFiles()
        {
            Dictionary<string, CGitIndexRecord> ls = readLsFiles();
            DirCache dc = new DirCache(index);
            Assert.AreEqual(0, dc.getEntryCount());
            dc.read();
            Assert.AreEqual(ls.Count, dc.getEntryCount());
            int i = 0;
            foreach (var val in ls.Values)
            {
                i++;
                Assert.AreEqual(val, dc.getEntry(i));
            }
        }

        [Test]
        public void testTreeWalk_LsFiles()
        {
            Dictionary<string, CGitIndexRecord> ls = readLsFiles();
            DirCache dc = new DirCache(index);
            Assert.AreEqual(0, dc.getEntryCount());
            dc.read();
            Assert.AreEqual(ls.Count, dc.getEntryCount());
            {
                var rItr = ls.Values.GetEnumerator();
                TreeWalk tw = new TreeWalk(db);
                tw.reset();
                tw.setRecursive(true);
                tw.addTree(new DirCacheIterator(dc));
                while (rItr.MoveNext())
                {
                    Assert.IsTrue(tw.next());
                    var dcItr = tw.getTree<DirCacheIterator>(0, typeof(DirCacheIterator));
                    Assert.IsNotNull(dcItr);
                    AssertAreEqual(rItr.Current, dcItr.getDirCacheEntry());
                }
            }
        }

        private static void AssertAreEqual(CGitIndexRecord c, DirCacheEntry j)
        {
            Assert.IsNotNull(c);
            Assert.IsNotNull(j);

            Assert.AreEqual(c.path, j.getPathString());
            Assert.AreEqual(c.id, j.getObjectId());
            Assert.AreEqual(c.mode, j.getRawMode());
            Assert.AreEqual(c.stage, j.getStage());
        }

        [Test]
        public void testReadIndex_DirCacheTree()
        {
            Dictionary<string, CGitIndexRecord> cList = readLsFiles();
            Dictionary<string, CGitLsTreeRecord> cTree = readLsTree();
            DirCache dc = new DirCache(index);
            Assert.AreEqual(0, dc.getEntryCount());
            dc.read();
            Assert.AreEqual(cList.Count, dc.getEntryCount());

            DirCacheTree jTree = dc.getCacheTree(false);
            Assert.IsNotNull(jTree);
            Assert.AreEqual("", jTree.getNameString());
            Assert.AreEqual("", jTree.getPathString());
            Assert.IsTrue(jTree.isValid());
            Assert.AreEqual(ObjectId
                    .FromString("698dd0b8d0c299f080559a1cffc7fe029479a408"), jTree
                    .getObjectId());
            Assert.AreEqual(cList.Count, jTree.getEntrySpan());

            List<CGitLsTreeRecord> subtrees = new List<CGitLsTreeRecord>();
            foreach (CGitLsTreeRecord r in cTree.Values)
            {
                if (FileMode.Tree.Equals(r.mode))
                    subtrees.Add(r);
            }
            Assert.AreEqual(subtrees.Count, jTree.getChildCount());

            for (int i = 0; i < jTree.getChildCount(); i++)
            {
                DirCacheTree sj = jTree.getChild(i);
                CGitLsTreeRecord sc = subtrees[i];
                Assert.AreEqual(sc.path, sj.getNameString());
                Assert.AreEqual(sc.path + "/", sj.getPathString());
                Assert.IsTrue(sj.isValid());
                Assert.AreEqual(sc.id, sj.getObjectId());
            }
        }

        private System.IO.FileInfo pathOf(string name)
        {
            return new System.IO.FileInfo( name);
        }

        private Dictionary<string, CGitIndexRecord> readLsFiles()
        {
            Dictionary<string, CGitIndexRecord> r = new Dictionary<string, CGitIndexRecord>();
            using (var br = new System.IO.StreamReader(new System.IO.FileStream("Resources/gitgit.lsfiles", System.IO.FileMode.Open, System.IO.FileAccess.Read), Encoding.UTF8))
            {
                string line;
                while ((line = br.ReadLine()) != null)
                {
                    CGitIndexRecord cr = new CGitIndexRecord(line);
                    r[cr.path]= cr;
                }
            }
            return r;
        }

        private Dictionary<string, CGitLsTreeRecord> readLsTree()
        {
            Dictionary<string, CGitLsTreeRecord> r = new Dictionary<string, CGitLsTreeRecord>();
            using (var br = new System.IO.StreamReader(new System.IO.FileStream("Resources/gitgit.lstree", System.IO.FileMode.Open, System.IO.FileAccess.Read), Encoding.UTF8))
            {
                string line;
                while ((line = br.ReadLine()) != null)
                {
                    CGitLsTreeRecord cr = new CGitLsTreeRecord(line);
                    r[cr.path]= cr;
                }
            }
            return r;
        }

        private class CGitIndexRecord
        {
            public int mode;

            public ObjectId id;

            public int stage;

            public string path;

            public CGitIndexRecord(string line)
            {
                int tab = line.IndexOf('\t');
                int sp1 = line.IndexOf(' ');
                int sp2 = line.IndexOf(' ', sp1 + 1);
                mode = NB.BaseToDecimal(line.Slice(0, sp1), 8);
                id = ObjectId.FromString(line.Slice(sp1 + 1, sp2));
                stage = int.Parse(line.Slice(sp2 + 1, tab));
                path = line.Substring(tab + 1);
            }
        }

        private class CGitLsTreeRecord
        {
            public int mode;

            public ObjectId id;

            public string path;

            public CGitLsTreeRecord(string line)
            {
                int tab = line.IndexOf('\t');
                int sp1 = line.IndexOf(' ');
                int sp2 = line.IndexOf(' ', sp1 + 1);
                mode = NB.BaseToDecimal(line.Slice(0, sp1), 8);
                id = ObjectId.FromString(line.Slice(sp2 + 1, tab));
                path = line.Substring(tab + 1);
            }
        }

    }
}
