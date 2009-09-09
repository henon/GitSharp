/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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
using System.IO;
using GitSharp.Exceptions;
using NUnit.Framework;
using GitSharp.Util;

namespace GitSharp.Tests
{
    [TestFixture]
    public class WorkDirCheckoutTest : RepositoryTestCase
    {
        // Methods
        [Test]
        public void testCheckingOutWithConflicts()
        {
            var index = new GitIndex(db);
            index.add(trash, writeTrashFile("bar", "bar"));
            index.add(trash, writeTrashFile("foo/bar/baz/qux", "foo/bar"));
            recursiveDelete(new FileInfo(Path.Combine(trash.FullName, "bar")));
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "foo")));
            writeTrashFile("bar/baz/qux/foo", "another nasty one");
            writeTrashFile("foo", "troublesome little bugger");
            try
            {
                new WorkDirCheckout(db, trash, index, index).checkout();
                Assert.Fail("Should have thrown exception");
            }
            catch (CheckoutConflictException)
            {
            }

            WorkDirCheckout workDirCheckout = new WorkDirCheckout(db, trash, index, index) { FailOnConflict = false };
            workDirCheckout.checkout();
            Assert.IsTrue(new FileInfo(Path.Combine(trash.FullName, "foo")).IsFile());
            Assert.IsTrue(new FileInfo(Path.Combine(trash.FullName, "foo/bar/baz/qux")).IsFile());
            var index2 = new GitIndex(db);
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "bar")));
            recursiveDelete(new FileInfo(Path.Combine(trash.FullName, "foo")));
            index2.add(trash, writeTrashFile("bar/baz/qux/foo", "bar"));
            writeTrashFile("bar/baz/qux/bar", "evil? I thought it said WEEVIL!");
            index2.add(trash, writeTrashFile("foo", "lalala"));

            workDirCheckout = new WorkDirCheckout(db, trash, index2, index) { FailOnConflict = false };
            workDirCheckout.checkout();
            Assert.IsTrue(new FileInfo(Path.Combine(trash.FullName, "bar")).IsFile());
            Assert.IsTrue(new FileInfo(Path.Combine(trash.FullName, "foo/bar/baz/qux")).IsFile());
            Assert.IsNotNull(index2.GetEntry("bar"));
            Assert.IsNotNull(index2.GetEntry("foo/bar/baz/qux"));
            Assert.IsNull(index2.GetEntry("bar/baz/qux/foo"));
            Assert.IsNull(index2.GetEntry("foo"));
        }

        [Test]
        public void testFindingConflicts()
        {
            var index = new GitIndex(db);
            index.add(trash, writeTrashFile("bar", "bar"));
            index.add(trash, writeTrashFile("foo/bar/baz/qux", "foo/bar"));
            recursiveDelete(new FileInfo(Path.Combine(trash.FullName, "bar")));
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "foo")));
            writeTrashFile("bar/baz/qux/foo", "another nasty one");
            writeTrashFile("foo", "troublesome little bugger");

            var workDirCheckout = new WorkDirCheckout(db, trash, index, index);
            workDirCheckout.PrescanOneTree();
            List<string> conflictingEntries = workDirCheckout.Conflicts;
            Assert.AreEqual("bar/baz/qux/foo", conflictingEntries[0]);
            Assert.AreEqual("foo", conflictingEntries[1]);

            var index2 = new GitIndex(db);
            recursiveDelete(new DirectoryInfo(Path.Combine(trash.FullName, "bar")));
            recursiveDelete(new FileInfo(Path.Combine(trash.FullName, "foo")));
            index2.add(trash, writeTrashFile("bar/baz/qux/foo", "bar"));
            index2.add(trash, writeTrashFile("foo", "lalala"));
            
            workDirCheckout = new WorkDirCheckout(db, trash, index2, index);
            workDirCheckout.PrescanOneTree();
            conflictingEntries = workDirCheckout.Conflicts;
            List<string> removedEntries = workDirCheckout.Removed;
            Assert.IsTrue(conflictingEntries.Count == 0);
            Assert.IsTrue(removedEntries.Contains("bar/baz/qux/foo"));
            Assert.IsTrue(removedEntries.Contains("foo"));
        }
    }
}