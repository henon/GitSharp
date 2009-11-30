/*
 * Copyright (C) 2009, Google Inc.
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

using System.Collections.Generic;
using GitSharp.Core;
using GitSharp.Core.DirectoryCache;
using GitSharp.Core.TreeWalk.Filter;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests.TreeWalk.Filter
{
    [TestFixture]
    public class PathSuffixFilterTestCase : RepositoryTestCase {
        [Test]
        public void testNonRecursiveFiltering()
        {
            var ow = new ObjectWriter(db);
            ObjectId aSth = ow.WriteBlob("a.sth".getBytes());
            ObjectId aTxt = ow.WriteBlob("a.txt".getBytes());
            DirCache dc = DirCache.read(db);
            DirCacheBuilder builder = dc.builder();
            var aSthEntry = new DirCacheEntry("a.sth");
            aSthEntry.setFileMode(FileMode.RegularFile);
            aSthEntry.setObjectId(aSth);
            var aTxtEntry = new DirCacheEntry("a.txt");
            aTxtEntry.setFileMode(FileMode.RegularFile);
            aTxtEntry.setObjectId(aTxt);
            builder.add(aSthEntry);
            builder.add(aTxtEntry);
            builder.finish();
            ObjectId treeId = dc.writeTree(ow);


            var tw = new GitSharp.Core.TreeWalk.TreeWalk(db);
            tw.setFilter(PathSuffixFilter.create(".txt"));
            tw.addTree(treeId);

            var paths = new LinkedList<string>();
            while (tw.next())
            {
                paths.AddLast(tw.getPathString());
            }

            var expected = new LinkedList<string>();
            expected.AddLast("a.txt");

            Assert.AreEqual(expected, paths);
        }

        [Test]
        public void testRecursiveFiltering()
        {
            var ow = new ObjectWriter(db);
            ObjectId aSth = ow.WriteBlob("a.sth".getBytes());
            ObjectId aTxt = ow.WriteBlob("a.txt".getBytes());
            ObjectId bSth = ow.WriteBlob("b.sth".getBytes());
            ObjectId bTxt = ow.WriteBlob("b.txt".getBytes());
            DirCache dc = DirCache.read(db);
            DirCacheBuilder builder = dc.builder();
            var aSthEntry = new DirCacheEntry("a.sth");
            aSthEntry.setFileMode(FileMode.RegularFile);
            aSthEntry.setObjectId(aSth);
            var aTxtEntry = new DirCacheEntry("a.txt");
            aTxtEntry.setFileMode(FileMode.RegularFile);
            aTxtEntry.setObjectId(aTxt);
            builder.add(aSthEntry);
            builder.add(aTxtEntry);
            var bSthEntry = new DirCacheEntry("sub/b.sth");
            bSthEntry.setFileMode(FileMode.RegularFile);
            bSthEntry.setObjectId(bSth);
            var bTxtEntry = new DirCacheEntry("sub/b.txt");
            bTxtEntry.setFileMode(FileMode.RegularFile);
            bTxtEntry.setObjectId(bTxt);
            builder.add(bSthEntry);
            builder.add(bTxtEntry);
            builder.finish();
            ObjectId treeId = dc.writeTree(ow);


            var tw = new GitSharp.Core.TreeWalk.TreeWalk(db);
            tw.Recursive = true;
            tw.setFilter(PathSuffixFilter.create(".txt"));
            tw.addTree(treeId);

            var paths = new LinkedList<string>();
            while (tw.next())
            {
                paths.AddLast(tw.getPathString());
            }

            var expected = new LinkedList<string>();
            expected.AddLast("a.txt");
            expected.AddLast("sub/b.txt");

            Assert.AreEqual(expected, paths);
        }

    }
}