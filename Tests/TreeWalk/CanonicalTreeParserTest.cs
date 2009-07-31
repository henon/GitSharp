/*
 * Copyright (C) 2008, Google Inc.
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * with@out modification, are permitted provided that the following
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
 *   products derived from this software with@out specific prior
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

using GitSharp.TreeWalk;
using System.Linq;
namespace GitSharp.Tests.TreeWalk
{

    using NUnit.Framework;
    using System.IO;
    using System.Text;
    [TestFixture]
    public class CanonicalTreeParserTest
    {

        private CanonicalTreeParser ctp = new CanonicalTreeParser();

        private GitSharp.FileMode m644 = GitSharp.FileMode.RegularFile;

        private GitSharp.FileMode mt = GitSharp.FileMode.Tree;

        private ObjectId hash_a = ObjectId.FromString("6b9c715d21d5486e59083fb6071566aa6ecd4d42");

        private ObjectId hash_foo = ObjectId.FromString("a213e8e25bb2442326e86cbfb9ef56319f482869");

        private ObjectId hash_sometree = ObjectId.FromString("daf4bdb0d7bb24319810fe0e73aa317663448c93");

        private byte[] tree1;

        private byte[] tree2;

        private byte[] tree3;

        [SetUp]
        public void setUp()
        {
            tree1 = mkree(entry(m644, "a", hash_a));
            tree2 = mkree(entry(m644, "a", hash_a), entry(m644, "foo", hash_foo));
            tree3 = mkree(entry(m644, "a", hash_a), entry(mt, "b_sometree", hash_sometree), entry(m644, "foo", hash_foo));
        }

        private static byte[] mkree(params byte[][] data)
        {
            var @out = new MemoryStream();
            foreach (byte[] e in data)
                @out.Write(e, 0, e.Length);
            return @out.ToArray();
        }

        private static byte[] entry(GitSharp.FileMode mode, string name, ObjectId id)
        {
            var @out = new MemoryStream();
            mode.CopyTo(@out);
            @out.WriteByte((byte)' ');
            var bytes = Constants.encode(name);
            @out.Write(bytes, 0, bytes.Length);
            @out.WriteByte((byte)0);
            id.copyRawTo(@out);
            return @out.ToArray();
        }

        private string path()
        {
            return RawParseUtils.decode(Constants.CHARSET, ctp.path, ctp.pathOffset, ctp.pathLen);
        }

        [Test]
        public void testEmptyTree_AtEOF()
        {
            ctp.reset(new byte[0]);
            Assert.IsTrue(ctp.eof());
        }

        [Test]
        public void testOneEntry_Forward()
        {
            ctp.reset(tree1);

            Assert.IsTrue(ctp.first());
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("a", path());
            Assert.AreEqual(hash_a, ctp.getEntryObjectId());

            ctp.next(1);
            Assert.IsFalse(ctp.first());
            Assert.IsTrue(ctp.eof());
        }

        [Test]
        public void testTwoEntries_ForwardOneAtATime()
        {
            ctp.reset(tree2);

            Assert.IsTrue(ctp.first());
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("a", path());
            Assert.AreEqual(hash_a, ctp.getEntryObjectId());

            ctp.next(1);
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("foo", path());
            Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

            ctp.next(1);
            Assert.IsFalse(ctp.first());
            Assert.IsTrue(ctp.eof());
        }

        [Test]
        public void testOneEntry_Seek1IsEOF()
        {
            ctp.reset(tree1);
            ctp.next(1);
            Assert.IsTrue(ctp.eof());
        }

        [Test]
        public void testTwoEntries_Seek2IsEOF()
        {
            ctp.reset(tree2);
            ctp.next(2);
            Assert.IsTrue(ctp.eof());
        }

        [Test]
        public void testThreeEntries_Seek3IsEOF()
        {
            ctp.reset(tree3);
            ctp.next(3);
            Assert.IsTrue(ctp.eof());
        }

        [Test]
        public void testThreeEntries_Seek2()
        {
            ctp.reset(tree3);

            ctp.next(2);
            Assert.IsFalse(ctp.eof());
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("foo", path());
            Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

            ctp.next(1);
            Assert.IsTrue(ctp.eof());
        }

        [Test]
        public void testOneEntry_Backwards()
        {
            ctp.reset(tree1);
            ctp.next(1);
            Assert.IsFalse(ctp.first());
            Assert.IsTrue(ctp.eof());

            ctp.back(1);
            Assert.IsTrue(ctp.first());
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("a", path());
            Assert.AreEqual(hash_a, ctp.getEntryObjectId());
        }

        [Test]
        public void testTwoEntries_BackwardsOneAtATime()
        {
            ctp.reset(tree2);
            ctp.next(2);
            Assert.IsTrue(ctp.eof());

            ctp.back(1);
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("foo", path());
            Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

            ctp.back(1);
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("a", path());
            Assert.AreEqual(hash_a, ctp.getEntryObjectId());
        }

        [Test]
        public void testTwoEntries_BackwardsTwo()
        {
            ctp.reset(tree2);
            ctp.next(2);
            Assert.IsTrue(ctp.eof());

            ctp.back(2);
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("a", path());
            Assert.AreEqual(hash_a, ctp.getEntryObjectId());

            ctp.next(1);
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("foo", path());
            Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

            ctp.next(1);
            Assert.IsTrue(ctp.eof());
        }

        [Test]
        public void testThreeEntries_BackwardsTwo()
        {
            ctp.reset(tree3);
            ctp.next(3);
            Assert.IsTrue(ctp.eof());

            ctp.back(2);
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(mt.Bits, ctp.mode);
            Assert.AreEqual("b_sometree", path());
            Assert.AreEqual(hash_sometree, ctp.getEntryObjectId());

            ctp.next(1);
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("foo", path());
            Assert.AreEqual(hash_foo, ctp.getEntryObjectId());

            ctp.next(1);
            Assert.IsTrue(ctp.eof());
        }

        [Test]
        public void testBackwards_ConfusingPathName()
        {
            string aVeryConfusingName = "confusing 644 entry 755 and others";
            ctp.reset(mkree(entry(m644, "a", hash_a), entry(mt, aVeryConfusingName,
                    hash_sometree), entry(m644, "foo", hash_foo)));
            ctp.next(3);
            Assert.IsTrue(ctp.eof());

            ctp.back(2);
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(mt.Bits, ctp.mode);
            Assert.AreEqual(aVeryConfusingName, path());
            Assert.AreEqual(hash_sometree, ctp.getEntryObjectId());

            ctp.back(1);
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(m644.Bits, ctp.mode);
            Assert.AreEqual("a", path());
            Assert.AreEqual(hash_a, ctp.getEntryObjectId());
        }

        [Test]
        public void testFreakingHugePathName()
        {
            int n = AbstractTreeIterator.DEFAULT_PATH_SIZE * 4;
            StringBuilder b = new StringBuilder(n);
            for (int i = 0; i < n; i++)
                b.Append('q');
            string name = b.ToString();
            ctp.reset(entry(m644, name, hash_a));
            Assert.IsFalse(ctp.eof());
            Assert.AreEqual(name, RawParseUtils.decode(Constants.CHARSET, ctp.path,
                    ctp.pathOffset, ctp.pathLen));
        }
    }
}
