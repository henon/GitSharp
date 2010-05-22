/*
 * Copyright (C) 2010, Google Inc.
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
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Text;
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Util
{
    [TestFixture]
    public class RefListTest
    {
        private static readonly ObjectId ID = ObjectId
            .FromString("41eb0d88f833b558bddeb269b7ab77399cdf98ed");

        private static readonly global::GitSharp.Core.Ref REF_A = newRef("A");

        private static readonly global::GitSharp.Core.Ref REF_B = newRef("B");

        private static readonly global::GitSharp.Core.Ref REF_c = newRef("c");

        [Test]
        public void testEmpty()
        {
            RefList<global::GitSharp.Core.Ref> list = RefList<global::GitSharp.Core.Ref>.emptyList();
            Assert.AreEqual(0, list.size());
            Assert.IsTrue(list.isEmpty());
            Assert.IsFalse(list.iterator().hasNext());
            Assert.AreEqual(-1, list.find("a"));
            Assert.AreEqual(-1, list.find("z"));
            Assert.IsFalse(list.contains("a"));
            Assert.IsNull(list.get("a"));
            try
            {
                list.get(0);
                Assert.Fail("RefList.emptyList should have 0 element array");
            }
            catch (IndexOutOfRangeException)
            {
                // expected
            }
        }

        [Test]
        public void testEmptyBuilder()
        {
            RefList<global::GitSharp.Core.Ref> list = new RefList<global::GitSharp.Core.Ref>.Builder<global::GitSharp.Core.Ref>().toRefList();
            Assert.AreEqual(0, list.size());
            Assert.IsFalse(list.iterator().hasNext());
            Assert.AreEqual(-1, list.find("a"));
            Assert.AreEqual(-1, list.find("z"));
            Assert.IsFalse(list.contains("a"));
            Assert.IsNull(list.get("a"));
            Assert.IsTrue(list.asList().Count == 0);
            Assert.AreEqual("[]", list.ToString());

            // default array capacity should be 16, with no bounds checking.
            Assert.IsNull(list.get(16 - 1));
            try
            {
                list.get(16);
                Assert.Fail("default RefList should have 16 element array");
            }
            catch (IndexOutOfRangeException)
            {
                // expected
            }
        }

        [Test]
        public void testBuilder_AddThenSort()
        {
            var builder = new RefList<global::GitSharp.Core.Ref>.Builder<global::GitSharp.Core.Ref>(1);
            builder.add(REF_B);
            builder.add(REF_A);

            RefList<global::GitSharp.Core.Ref> list = builder.toRefList();
            Assert.AreEqual(2, list.size());
            Assert.AreSame(REF_B, list.get(0));
            Assert.AreSame(REF_A, list.get(1));

            builder.sort();
            list = builder.toRefList();
            Assert.AreEqual(2, list.size());
            Assert.AreSame(REF_A, list.get(0));
            Assert.AreSame(REF_B, list.get(1));
        }

        [Test]
        public void testBuilder_AddAll()
        {
            var builder = new RefList<global::GitSharp.Core.Ref>.Builder<global::GitSharp.Core.Ref>(1);
            global::GitSharp.Core.Ref[] src = { REF_A, REF_B, REF_c, REF_A };
            builder.addAll(src, 1, 2);

            RefList<global::GitSharp.Core.Ref> list = builder.toRefList();
            Assert.AreEqual(2, list.size());
            Assert.AreSame(REF_B, list.get(0));
            Assert.AreSame(REF_c, list.get(1));
        }

        [Test]
        public void testBuilder_Set()
        {
            var builder = new RefList<global::GitSharp.Core.Ref>.Builder<global::GitSharp.Core.Ref>();
            builder.add(REF_A);
            builder.add(REF_A);

            Assert.AreEqual(2, builder.size());
            Assert.AreSame(REF_A, builder.get(0));
            Assert.AreSame(REF_A, builder.get(1));

            RefList<global::GitSharp.Core.Ref> list = builder.toRefList();
            Assert.AreEqual(2, list.size());
            Assert.AreSame(REF_A, list.get(0));
            Assert.AreSame(REF_A, list.get(1));
            builder.set(1, REF_B);

            list = builder.toRefList();
            Assert.AreEqual(2, list.size());
            Assert.AreSame(REF_A, list.get(0));
            Assert.AreSame(REF_B, list.get(1));
        }

        [Test]
        public void testBuilder_Remove()
        {
            var builder = new RefList<global::GitSharp.Core.Ref>.Builder<global::GitSharp.Core.Ref>();
            builder.add(REF_A);
            builder.add(REF_B);
            builder.remove(0);

            Assert.AreEqual(1, builder.size());
            Assert.AreSame(REF_B, builder.get(0));
        }

        [Test]
        public void testSet()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A, REF_A);
            RefList<global::GitSharp.Core.Ref> two = one.set(1, REF_B);
            Assert.AreNotSame(one, two);

            // one is not modified
            Assert.AreEqual(2, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreSame(REF_A, one.get(1));

            // but two is
            Assert.AreEqual(2, two.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreSame(REF_B, two.get(1));
        }

        [Test]
        public void testAddToEmptyList()
        {
            RefList<global::GitSharp.Core.Ref> one = toList();
            RefList<global::GitSharp.Core.Ref> two = one.add(0, REF_B);
            Assert.AreNotSame(one, two);

            // one is not modified, but two is
            Assert.AreEqual(0, one.size());
            Assert.AreEqual(1, two.size());
            Assert.IsFalse(two.isEmpty());
            Assert.AreSame(REF_B, two.get(0));
        }

        [Test]
        public void testAddToFrontOfList()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A);
            RefList<global::GitSharp.Core.Ref> two = one.add(0, REF_B);
            Assert.AreNotSame(one, two);

            // one is not modified, but two is
            Assert.AreEqual(1, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreEqual(2, two.size());
            Assert.AreSame(REF_B, two.get(0));
            Assert.AreSame(REF_A, two.get(1));
        }

        [Test]
        public void testAddToEndOfList()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A);
            RefList<global::GitSharp.Core.Ref> two = one.add(1, REF_B);
            Assert.AreNotSame(one, two);

            // one is not modified, but two is
            Assert.AreEqual(1, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreEqual(2, two.size());
            Assert.AreSame(REF_A, two.get(0));
            Assert.AreSame(REF_B, two.get(1));
        }

        [Test]
        public void testAddToMiddleOfListByInsertionPosition()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A, REF_c);

            Assert.AreEqual(-2, one.find(REF_B.Name));

            RefList<global::GitSharp.Core.Ref> two = one.add(one.find(REF_B.Name), REF_B);
            Assert.AreNotSame(one, two);

            // one is not modified, but two is
            Assert.AreEqual(2, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreSame(REF_c, one.get(1));

            Assert.AreEqual(3, two.size());
            Assert.AreSame(REF_A, two.get(0));
            Assert.AreSame(REF_B, two.get(1));
            Assert.AreSame(REF_c, two.get(2));
        }

        [Test]
        public void testPutNewEntry()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A, REF_c);
            RefList<global::GitSharp.Core.Ref> two = one.put(REF_B);
            Assert.AreNotSame(one, two);

            // one is not modified, but two is
            Assert.AreEqual(2, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreSame(REF_c, one.get(1));

            Assert.AreEqual(3, two.size());
            Assert.AreSame(REF_A, two.get(0));
            Assert.AreSame(REF_B, two.get(1));
            Assert.AreSame(REF_c, two.get(2));
        }

        [Test]
        public void testPutReplaceEntry()
        {
            global::GitSharp.Core.Ref otherc = newRef(REF_c.Name);
            Assert.AreNotSame(REF_c, otherc);

            RefList<global::GitSharp.Core.Ref> one = toList(REF_A, REF_c);
            RefList<global::GitSharp.Core.Ref> two = one.put(otherc);
            Assert.AreNotSame(one, two);

            // one is not modified, but two is
            Assert.AreEqual(2, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreSame(REF_c, one.get(1));

            Assert.AreEqual(2, two.size());
            Assert.AreSame(REF_A, two.get(0));
            Assert.AreSame(otherc, two.get(1));
        }

        [Test]
        public void testRemoveFrontOfList()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A, REF_B, REF_c);
            RefList<global::GitSharp.Core.Ref> two = one.remove(0);
            Assert.AreNotSame(one, two);

            Assert.AreEqual(3, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreSame(REF_B, one.get(1));
            Assert.AreSame(REF_c, one.get(2));

            Assert.AreEqual(2, two.size());
            Assert.AreSame(REF_B, two.get(0));
            Assert.AreSame(REF_c, two.get(1));
        }

        [Test]
        public void testRemoveMiddleOfList()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A, REF_B, REF_c);
            RefList<global::GitSharp.Core.Ref> two = one.remove(1);
            Assert.AreNotSame(one, two);

            Assert.AreEqual(3, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreSame(REF_B, one.get(1));
            Assert.AreSame(REF_c, one.get(2));

            Assert.AreEqual(2, two.size());
            Assert.AreSame(REF_A, two.get(0));
            Assert.AreSame(REF_c, two.get(1));
        }

        [Test]
        public void testRemoveEndOfList()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A, REF_B, REF_c);
            RefList<global::GitSharp.Core.Ref> two = one.remove(2);
            Assert.AreNotSame(one, two);

            Assert.AreEqual(3, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreSame(REF_B, one.get(1));
            Assert.AreSame(REF_c, one.get(2));

            Assert.AreEqual(2, two.size());
            Assert.AreSame(REF_A, two.get(0));
            Assert.AreSame(REF_B, two.get(1));
        }

        [Test]
        public void testRemoveMakesEmpty()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A);
            RefList<global::GitSharp.Core.Ref> two = one.remove(1);
            Assert.AreNotSame(one, two);
            Assert.AreSame(two, RefList<global::GitSharp.Core.Ref>.emptyList());
        }

        [Test]
        public void testToString()
        {
            var exp = new StringBuilder();
            exp.Append("[");
            exp.Append(REF_A);
            exp.Append(", ");
            exp.Append(REF_B);
            exp.Append("]");

            RefList<global::GitSharp.Core.Ref> list = toList(REF_A, REF_B);
            Assert.AreEqual(exp.ToString(), list.ToString());
        }

        [Test]
        public void testBuilder_ToString()
        {
            var exp = new StringBuilder();
            exp.Append("[");
            exp.Append(REF_A);
            exp.Append(", ");
            exp.Append(REF_B);
            exp.Append("]");

            var list = new RefList<global::GitSharp.Core.Ref>.Builder<global::GitSharp.Core.Ref>();
            list.add(REF_A);
            list.add(REF_B);
            Assert.AreEqual(exp.ToString(), list.ToString());
        }

        [Test]
        public void testFindContainsGet()
        {
            RefList<global::GitSharp.Core.Ref> list = toList(REF_A, REF_B, REF_c);

            Assert.AreEqual(0, list.find("A"));
            Assert.AreEqual(1, list.find("B"));
            Assert.AreEqual(2, list.find("c"));

            Assert.AreEqual(-1, list.find("0"));
            Assert.AreEqual(-2, list.find("AB"));
            Assert.AreEqual(-3, list.find("a"));
            Assert.AreEqual(-4, list.find("z"));

            Assert.AreSame(REF_A, list.get("A"));
            Assert.AreSame(REF_B, list.get("B"));
            Assert.AreSame(REF_c, list.get("c"));
            Assert.IsNull(list.get("AB"));
            Assert.IsNull(list.get("z"));

            Assert.IsTrue(list.contains("A"));
            Assert.IsTrue(list.contains("B"));
            Assert.IsTrue(list.contains("c"));
            Assert.IsFalse(list.contains("AB"));
            Assert.IsFalse(list.contains("z"));
        }

        [Test]
        public void testIterable()
        {
            RefList<global::GitSharp.Core.Ref> list = toList(REF_A, REF_B, REF_c);

            int idx = 0;
            foreach (global::GitSharp.Core.Ref @ref in list)
                Assert.AreSame(list.get(idx++), @ref);
            Assert.AreEqual(3, idx);

            var i = RefList<global::GitSharp.Core.Ref>.emptyList().iterator();
            try
            {
                i.next();
                Assert.Fail("did not throw NoSuchElementException");
            }
            catch (IndexOutOfRangeException)
            {
                // expected
            }

            i = list.iterator();
            Assert.IsTrue(i.hasNext());
            Assert.AreSame(REF_A, i.next());
            try
            {
                i.remove();
                Assert.Fail("did not throw UnsupportedOperationException");
            }
            catch (NotSupportedException)
            {
                // expected
            }
        }

        [Test]
        public void testCopyLeadingPrefix()
        {
            RefList<global::GitSharp.Core.Ref> one = toList(REF_A, REF_B, REF_c);
            RefList<global::GitSharp.Core.Ref> two = one.copy(2).toRefList();
            Assert.AreNotSame(one, two);

            Assert.AreEqual(3, one.size());
            Assert.AreSame(REF_A, one.get(0));
            Assert.AreSame(REF_B, one.get(1));
            Assert.AreSame(REF_c, one.get(2));

            Assert.AreEqual(2, two.size());
            Assert.AreSame(REF_A, two.get(0));
            Assert.AreSame(REF_B, two.get(1));
        }

        [Test]
        public void testCopyConstructorReusesArray()
        {
            var one = new RefList<global::GitSharp.Core.Ref>.Builder<global::GitSharp.Core.Ref>();
            one.add(REF_A);

            var two = new RefList<global::GitSharp.Core.Ref>(one.toRefList());
            one.set(0, REF_B);
            Assert.AreSame(REF_B, two.get(0));
        }

        private static RefList<global::GitSharp.Core.Ref> toList(params global::GitSharp.Core.Ref[] refs)
        {
            var b = new RefList<global::GitSharp.Core.Ref>.Builder<global::GitSharp.Core.Ref>(refs.Length);
            b.addAll(refs, 0, refs.Length);
            return b.toRefList();
        }

        private static global::GitSharp.Core.Ref newRef(string name)
        {
            return new Unpeeled(Storage.Loose, name, ID);
        }
    }
}