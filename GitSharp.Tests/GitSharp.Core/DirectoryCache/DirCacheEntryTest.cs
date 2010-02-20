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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
 * ADVISED OF TOSSIBILITY OF SUCH DAMAGE.
 */

using System;
using NUnit.Framework;
using GitSharp.Core;
using GitSharp.Core.DirectoryCache;

namespace GitSharp.Tests.GitSharp.Core.DirectoryCache
{
    [TestFixture]
    public class DirCacheEntryTest
    {
        [Test]
        public void testIsValidPath()
        {
            Assert.IsTrue(isValidPath("a"));
            Assert.IsTrue(isValidPath("a/b"));
            Assert.IsTrue(isValidPath("ab/cd/ef"));

            Assert.IsFalse(isValidPath(""));
            Assert.IsFalse(isValidPath("/a"));
            Assert.IsFalse(isValidPath("a//b"));
            Assert.IsFalse(isValidPath("ab/cd//ef"));
            Assert.IsFalse(isValidPath("a/"));
            Assert.IsFalse(isValidPath("ab/cd/ef/"));
            Assert.IsFalse(isValidPath("a\u0000b"));
        }

        private static bool isValidPath(string path)
        {
            return DirCacheEntry.isValidPath(Constants.encode(path));
        }

        [Test]
        public void testCreate_ByStringPath()
        {
            Assert.AreEqual("a", new DirCacheEntry("a").getPathString());
            Assert.AreEqual("a/b", new DirCacheEntry("a/b").getPathString());

            try
            {
                new DirCacheEntry("/a");
                Assert.Fail("Incorrectly created DirCacheEntry");
            }
            catch (ArgumentException err)
            {
                Assert.AreEqual("Invalid path: /a", err.Message);
            }
        }

        [Test]
        public void testCreate_ByStringPathAndStage()
        {
            DirCacheEntry e;

            e = new DirCacheEntry("a", 0);
            Assert.AreEqual("a", e.getPathString());
            Assert.AreEqual(0, e.getStage());

            e = new DirCacheEntry("a/b", 1);
            Assert.AreEqual("a/b", e.getPathString());
            Assert.AreEqual(1, e.getStage());

            e = new DirCacheEntry("a/c", 2);
            Assert.AreEqual("a/c", e.getPathString());
            Assert.AreEqual(2, e.getStage());

            e = new DirCacheEntry("a/d", 3);
            Assert.AreEqual("a/d", e.getPathString());
            Assert.AreEqual(3, e.getStage());

            try
            {
                new DirCacheEntry("/a", 1);
                Assert.Fail("Incorrectly created DirCacheEntry");
            }
            catch (ArgumentException err)
            {
                Assert.AreEqual("Invalid path: /a", err.Message);
            }

            try
            {
                new DirCacheEntry("a", -11);
                Assert.Fail("Incorrectly created DirCacheEntry");
            }
            catch (ArgumentException err)
            {
                Assert.AreEqual("Invalid stage -11 for path a", err.Message);
            }

            try
            {
                new DirCacheEntry("a", 4);
                Assert.Fail("Incorrectly created DirCacheEntry");
            }
            catch (ArgumentException err)
            {
                Assert.AreEqual("Invalid stage 4 for path a", err.Message);
            }
        }
    }
}


