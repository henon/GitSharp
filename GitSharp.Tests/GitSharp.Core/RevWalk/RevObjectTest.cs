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

using GitSharp.Core;
using GitSharp.Core.RevWalk;
using NUnit.Framework;

namespace GitSharp.Core.Tests.RevWalk
{
    [TestFixture]
    public class RevObjectTest : RevWalkTestCase
    {
        [Test]
        public void testId()
        {
            RevCommit a = Commit();
            Assert.AreSame(a, a.getId());
        }

        [Test]
        public void testEqualsIsIdentity()
        {
            RevCommit a1 = Commit();
            RevCommit b1 = Commit();

            Assert.IsTrue(a1.Equals(a1));
            Assert.IsTrue(a1.Equals((object)a1));
            Assert.IsFalse(a1.Equals(b1));

            Assert.IsFalse(a1.Equals(a1.Copy()));
            Assert.IsFalse(a1.Equals((object)a1.Copy()));
            Assert.IsFalse(a1.Equals(string.Empty));

            var rw2 = new GitSharp.Core.RevWalk.RevWalk(db);
            RevCommit a2 = rw2.parseCommit(a1);
            RevCommit b2 = rw2.parseCommit(b1);
            Assert.AreNotSame(a1, a2);
            Assert.AreNotSame(b1, b2);

            Assert.IsFalse(a1.Equals(a2));
            Assert.IsFalse(b1.Equals(b2));

            Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());
            Assert.AreEqual(b1.GetHashCode(), b2.GetHashCode());

            Assert.IsTrue(AnyObjectId.equals(a1, a2));
            Assert.IsTrue(AnyObjectId.equals(b1, b2));
        }

        [Test]
        public void testRevObjectTypes()
        {
            Assert.AreEqual(Constants.OBJ_TREE, tree().Type);
            Assert.AreEqual(Constants.OBJ_COMMIT, Commit().Type);
            Assert.AreEqual(Constants.OBJ_BLOB, blob(string.Empty).Type);
            Assert.AreEqual(Constants.OBJ_TAG, Tag("emptyTree", tree()).Type);
        }

        [Test]
        public void testHasRevFlag()
        {
            RevCommit a = Commit();
            Assert.IsFalse(a.has(RevFlag.UNINTERESTING));
            a.Flags |= GitSharp.Core.RevWalk.RevWalk.UNINTERESTING;
            Assert.IsTrue(a.has(RevFlag.UNINTERESTING));
        }

        [Test]
        public void testHasAnyFlag()
        {
            RevCommit a = Commit();
            RevFlag flag1 = rw.newFlag("flag1");
            RevFlag flag2 = rw.newFlag("flag2");
            var s = new RevFlagSet { flag1, flag2 };

            Assert.IsFalse(a.hasAny(s));
            a.Flags |= flag1.Mask;
            Assert.IsTrue(a.hasAny(s));
        }

        [Test]
        public void testHasAllFlag()
        {
            RevCommit a = Commit();
            RevFlag flag1 = rw.newFlag("flag1");
            RevFlag flag2 = rw.newFlag("flag2");
            var s = new RevFlagSet { flag1, flag2 };

            Assert.IsFalse(a.hasAll(s));
            a.Flags |= flag1.Mask;
            Assert.IsFalse(a.hasAll(s));
            a.Flags |= flag2.Mask;
            Assert.IsTrue(a.hasAll(s));
        }

        [Test]
        public void testAddRevFlag()
        {
            RevCommit a = Commit();
            RevFlag flag1 = rw.newFlag("flag1");
            RevFlag flag2 = rw.newFlag("flag2");
            Assert.AreEqual(0, a.Flags);

            a.add(flag1);
            Assert.AreEqual(flag1.Mask, a.Flags);

            a.add(flag2);
            Assert.AreEqual(flag1.Mask | flag2.Mask, a.Flags);
        }

        [Test]
        public void testAddRevFlagSet()
        {
            RevCommit a = Commit();
            RevFlag flag1 = rw.newFlag("flag1");
            RevFlag flag2 = rw.newFlag("flag2");
            var s = new RevFlagSet { flag1, flag2 };

            Assert.AreEqual(0, a.Flags);

            a.add(s);
            Assert.AreEqual(flag1.Mask | flag2.Mask, a.Flags);
        }

        [Test]
        public void testRemoveRevFlag()
        {
            RevCommit a = Commit();
            RevFlag flag1 = rw.newFlag("flag1");
            RevFlag flag2 = rw.newFlag("flag2");
            a.add(flag1);
            a.add(flag2);
            Assert.AreEqual(flag1.Mask | flag2.Mask, a.Flags);
            a.remove(flag2);
            Assert.AreEqual(flag1.Mask, a.Flags);
        }

        [Test]
        public void testRemoveRevFlagSet()
        {
            RevCommit a = Commit();
            RevFlag flag1 = rw.newFlag("flag1");
            RevFlag flag2 = rw.newFlag("flag2");
            RevFlag flag3 = rw.newFlag("flag3");
            var s = new RevFlagSet { flag1, flag2 };
            a.add(flag3);
            a.add(s);
            Assert.AreEqual(flag1.Mask | flag2.Mask | flag3.Mask, a.Flags);
            a.remove(s);
            Assert.AreEqual(flag3.Mask, a.Flags);
        }
    }
}
