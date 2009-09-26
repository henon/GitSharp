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

using System.IO;
using GitSharp.Exceptions;
using GitSharp.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
	public class RepositoryCacheTest : RepositoryTestCase
	{
        [Test]
		public void testNonBareFileKey()
		{
            DirectoryInfo gitdir = db.Directory;
            DirectoryInfo parent = gitdir.Parent;
			Assert.IsNotNull(parent);

            var other = new DirectoryInfo(Path.Combine(parent.FullName, "notagit"));
			Assert.AreEqual(gitdir, RepositoryCache.FileKey.exact(gitdir).getFile());
			Assert.AreEqual(parent, RepositoryCache.FileKey.exact(parent).getFile());
			Assert.AreEqual(other, RepositoryCache.FileKey.exact(other).getFile());

            Assert.AreEqual(gitdir, RepositoryCache.FileKey.lenient(gitdir).getFile());
            
            // Test was "fixed" because DirectoryInfo.Equals() compares two different references
            Assert.AreEqual(gitdir.FullName, RepositoryCache.FileKey.lenient(parent).getFile().FullName);
            Assert.AreEqual(other, RepositoryCache.FileKey.lenient(other).getFile());
        }

        [Test]
        public void testBareFileKey()
        {
            Repository bare = createNewEmptyRepo(true);
            DirectoryInfo gitdir = bare.Directory;
            DirectoryInfo parent = gitdir.Parent;
			Assert.IsNotNull(parent);

            string name = gitdir.Name;
            Assert.IsTrue(name.EndsWith(".git"));
            name = name.Slice(0, name.Length - 4);

            Assert.AreEqual(gitdir, RepositoryCache.FileKey.exact(gitdir).getFile());

            Assert.AreEqual(gitdir, RepositoryCache.FileKey.lenient(gitdir).getFile());

            // Test was "fixed" because DirectoryInfo.Equals() compares two different references
            Assert.AreEqual(gitdir.FullName, RepositoryCache.FileKey.lenient(new DirectoryInfo(Path.Combine(parent.FullName, name))).getFile().FullName);
        }

        [Test]
        public void testFileKeyOpenExisting()
        {
			Repository r = new RepositoryCache.FileKey(db.Directory).open(true);
            Assert.IsNotNull(r);
			Assert.AreEqual(db.Directory.FullName, r.Directory.FullName);
            r.Close();

            r = new RepositoryCache.FileKey(db.Directory).open(false);
            Assert.IsNotNull(r);
			Assert.AreEqual(db.Directory.FullName, r.Directory.FullName);
            r.Close();
        }

        [Test]
        public void testFileKeyOpenNew()
        {
            Repository n = createNewEmptyRepo(true);
            DirectoryInfo gitdir = n.Directory;
            n.Close();
            recursiveDelete(gitdir);
            Assert.IsFalse(gitdir.Exists);

            var e = AssertHelper.Throws<RepositoryNotFoundException>(() => new RepositoryCache.FileKey(gitdir).open(true));
            Assert.AreEqual("repository not found: " + gitdir, e.Message);

            Repository o = new RepositoryCache.FileKey(gitdir).open(false);
            Assert.IsNotNull(o);
			Assert.AreEqual(gitdir.FullName, o.Directory.FullName);
            Assert.IsFalse(gitdir.Exists);
        }

        [Test]
        public void testCacheRegisterOpen()
        {
            DirectoryInfo dir = db.Directory;
            RepositoryCache.register(db);
            Assert.AreSame(db, RepositoryCache.open(RepositoryCache.FileKey.exact(dir)));

			Assert.IsTrue(dir.Name.EndsWith(".git"));
            Assert.AreEqual(".git", dir.Name);
            DirectoryInfo parent = dir.Parent;
            Assert.AreSame(db, RepositoryCache.open(RepositoryCache.FileKey.lenient(parent)));
        }

        [Test]
        public void testCacheOpen()
        {
            RepositoryCache.FileKey loc = RepositoryCache.FileKey.exact(db.Directory);
            Repository d2 = RepositoryCache.open(loc);
            Assert.AreNotSame(db, d2);
            Assert.AreSame(d2, RepositoryCache.open(RepositoryCache.FileKey.exact(loc.getFile())));
            d2.Close();
            d2.Close();
        }
    }
}