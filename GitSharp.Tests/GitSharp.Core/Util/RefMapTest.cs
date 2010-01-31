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
    public class RefMapTest
    {
        private static ObjectId ID_ONE = ObjectId
            .FromString("41eb0d88f833b558bddeb269b7ab77399cdf98ed");

        private static ObjectId ID_TWO = ObjectId
            .FromString("698dd0b8d0c299f080559a1cffc7fe029479a408");

        private RefList<global::GitSharp.Core.Ref> packed;

        private RefList<global::GitSharp.Core.Ref> loose;

        private RefList<global::GitSharp.Core.Ref> resolved;

        [SetUp]
        protected void setUp()
        {
            packed = RefList<global::GitSharp.Core.Ref>.emptyList();
            loose = RefList<global::GitSharp.Core.Ref>.emptyList();
            resolved = RefList<global::GitSharp.Core.Ref>.emptyList();
        }

        [Test]
        public void testEmpty_NoPrefix1()
        {
            RefMap map = new RefMap("", packed, loose, resolved);
            Assert.IsTrue(map.isEmpty()); // before size was computed
            Assert.AreEqual(0, map.size());
            Assert.IsTrue(map.isEmpty()); // after size was computed

            Assert.IsFalse(map.entrySet().iterator().hasNext());
            Assert.IsFalse(map.keySet().iterator().hasNext());
            Assert.IsFalse(map.containsKey("a"));
            Assert.IsNull(map.get("a"));
        }

        [Test]
        public void testEmpty_NoPrefix2()
        {
            RefMap map = new RefMap();
            Assert.IsTrue(map.isEmpty()); // before size was computed
            Assert.AreEqual(0, map.size());
            Assert.IsTrue(map.isEmpty()); // after size was computed

            Assert.IsFalse(map.entrySet().iterator().hasNext());
            Assert.IsFalse(map.keySet().iterator().hasNext());
            Assert.IsFalse(map.containsKey("a"));
            Assert.IsNull(map.get("a"));
        }

        [Test]
        public void testNotEmpty_NoPrefix()
        {
            global::GitSharp.Core.Ref master = newRef("refs/heads/master", ID_ONE);
            packed = toList(master);

            RefMap map = new RefMap("", packed, loose, resolved);
            Assert.IsFalse(map.isEmpty()); // before size was computed
            Assert.AreEqual(1, map.size());
            Assert.IsFalse(map.isEmpty()); // after size was computed
            Assert.AreSame(master, map.values().iterator().next());
        }

        [Test]
        public void testEmpty_WithPrefix()
        {
            global::GitSharp.Core.Ref master = newRef("refs/heads/master", ID_ONE);
            packed = toList(master);

            RefMap map = new RefMap("refs/tags/", packed, loose, resolved);
            Assert.IsTrue(map.isEmpty()); // before size was computed
            Assert.AreEqual(0, map.size());
            Assert.IsTrue(map.isEmpty()); // after size was computed

            Assert.IsFalse(map.entrySet().iterator().hasNext());
            Assert.IsFalse(map.keySet().iterator().hasNext());
        }

        [Test]
        public void testNotEmpty_WithPrefix()
        {
            global::GitSharp.Core.Ref master = newRef("refs/heads/master", ID_ONE);
            packed = toList(master);

            RefMap map = new RefMap("refs/heads/", packed, loose, resolved);
            Assert.IsFalse(map.isEmpty()); // before size was computed
            Assert.AreEqual(1, map.size());
            Assert.IsFalse(map.isEmpty()); // after size was computed
            Assert.AreSame(master, map.values().iterator().next());
        }

        [Test]
        public void testClear()
        {
            global::GitSharp.Core.Ref master = newRef("refs/heads/master", ID_ONE);
            loose = toList(master);

            RefMap map = new RefMap("", packed, loose, resolved);
            Assert.AreSame(master, map.get("refs/heads/master"));

            map.clear();
            Assert.IsNull(map.get("refs/heads/master"));
            Assert.IsTrue(map.isEmpty());
            Assert.AreEqual(0, map.size());
        }

        [Test]
        public void testIterator_RefusesRemove()
        {
            global::GitSharp.Core.Ref master = newRef("refs/heads/master", ID_ONE);
            loose = toList(master);

            RefMap map = new RefMap("", packed, loose, resolved);
            IteratorBase<global::GitSharp.Core.Ref> itr = map.values().iterator();
            Assert.IsTrue(itr.hasNext());
            Assert.AreSame(master, itr.next());
            try
            {
                itr.remove();
                Assert.Fail("iterator allowed remove");
            }
            catch (NotSupportedException err)
            {
                // expected
            }
        }

        [Test]
        public void testIterator_FailsAtEnd()
        {
            global::GitSharp.Core.Ref master = newRef("refs/heads/master", ID_ONE);
            loose = toList(master);

            RefMap map = new RefMap("", packed, loose, resolved);
            IteratorBase<global::GitSharp.Core.Ref> itr = map.values().iterator();
            Assert.IsTrue(itr.hasNext());
            Assert.AreSame(master, itr.next());
            try
            {
                itr.next();
                Assert.Fail("iterator allowed next");
            }
            catch (ArgumentOutOfRangeException err)
            {
                // expected
            }
        }

        [Test]
        public void testMerge_PackedLooseLoose()
        {
            global::GitSharp.Core.Ref refA = newRef("A", ID_ONE);
            global::GitSharp.Core.Ref refB_ONE = newRef("B", ID_ONE);
            global::GitSharp.Core.Ref refB_TWO = newRef("B", ID_TWO);
            global::GitSharp.Core.Ref refc = newRef("c", ID_ONE);

            packed = toList(refA, refB_ONE);
            loose = toList(refB_TWO, refc);

            RefMap map = new RefMap("", packed, loose, resolved);
            Assert.AreEqual(3, map.size());
            Assert.IsFalse(map.isEmpty());
            Assert.IsTrue(map.containsKey(refA.Name));
            Assert.AreSame(refA, map.get(refA.Name));

            // loose overrides packed given same name
            Assert.AreSame(refB_TWO, map.get(refB_ONE.Name));

            var itr = map.values().iterator();
            Assert.IsTrue(itr.hasNext());
            Assert.AreSame(refA, itr.next());
            Assert.IsTrue(itr.hasNext());
            Assert.AreSame(refB_TWO, itr.next());
            Assert.IsTrue(itr.hasNext());
            Assert.AreSame(refc, itr.next());
            Assert.IsFalse(itr.hasNext());
        }

        [Test]
        public void testMerge_WithPrefix()
        {
            global::GitSharp.Core.Ref a = newRef("refs/heads/A", ID_ONE);
            global::GitSharp.Core.Ref b = newRef("refs/heads/foo/bar/B", ID_TWO);
            global::GitSharp.Core.Ref c = newRef("refs/heads/foo/rab/C", ID_TWO);
            global::GitSharp.Core.Ref g = newRef("refs/heads/g", ID_ONE);
            packed = toList(a, b, c, g);

            RefMap map = new RefMap("refs/heads/foo/", packed, loose, resolved);
            Assert.AreEqual(2, map.size());

            Assert.AreSame(b, map.get("bar/B"));
            Assert.AreSame(c, map.get("rab/C"));
            Assert.IsNull(map.get("refs/heads/foo/bar/B"));
            Assert.IsNull(map.get("refs/heads/A"));

            Assert.IsTrue(map.containsKey("bar/B"));
            Assert.IsTrue(map.containsKey("rab/C"));
            Assert.IsFalse(map.containsKey("refs/heads/foo/bar/B"));
            Assert.IsFalse(map.containsKey("refs/heads/A"));

            IteratorBase<RefMap.Ent> itr = map.entrySet().iterator();
            RefMap.Ent ent;
            Assert.IsTrue(itr.hasNext());
            ent = itr.next();
            Assert.AreEqual("bar/B", ent.getKey());
            Assert.AreSame(b, ent.getValue());
            Assert.IsTrue(itr.hasNext());
            ent = itr.next();
            Assert.AreEqual("rab/C", ent.getKey());
            Assert.AreSame(c, ent.getValue());
            Assert.IsFalse(itr.hasNext());
        }

        [Test]
        public void testPut_KeyMustMatchName_NoPrefix()
        {
            global::GitSharp.Core.Ref refA = newRef("refs/heads/A", ID_ONE);
            RefMap map = new RefMap("", packed, loose, resolved);
            try
            {
                map.put("FOO", refA);
                Assert.Fail("map accepted invalid key/value pair");
            }
            catch (ArgumentException err)
            {
                // expected
            }
        }

        [Test]
        public void testPut_KeyMustMatchName_WithPrefix()
        {
            global::GitSharp.Core.Ref refA = newRef("refs/heads/A", ID_ONE);
            RefMap map = new RefMap("refs/heads/", packed, loose, resolved);
            try
            {
                map.put("FOO", refA);
                Assert.Fail("map accepted invalid key/value pair");
            }
            catch (ArgumentException err)
            {
                // expected
            }
        }

        [Test]
        public void testPut_NoPrefix()
        {
            global::GitSharp.Core.Ref refA_one = newRef("refs/heads/A", ID_ONE);
            global::GitSharp.Core.Ref refA_two = newRef("refs/heads/A", ID_TWO);

            packed = toList(refA_one);

            RefMap map = new RefMap("", packed, loose, resolved);
            Assert.AreSame(refA_one, map.get(refA_one.Name));
            Assert.AreSame(refA_one, map.put(refA_one.Name, refA_two));

            // map changed, but packed, loose did not
            Assert.AreSame(refA_two, map.get(refA_one.Name));
            Assert.AreSame(refA_one, packed.get(0));
            Assert.AreEqual(0, loose.size());

            Assert.AreSame(refA_two, map.put(refA_one.Name, refA_one));
            Assert.AreSame(refA_one, map.get(refA_one.Name));
        }

        [Test]
        public void testPut_WithPrefix()
        {
            global::GitSharp.Core.Ref refA_one = newRef("refs/heads/A", ID_ONE);
            global::GitSharp.Core.Ref refA_two = newRef("refs/heads/A", ID_TWO);

            packed = toList(refA_one);

            RefMap map = new RefMap("refs/heads/", packed, loose, resolved);
            Assert.AreSame(refA_one, map.get("A"));
            Assert.AreSame(refA_one, map.put("A", refA_two));

            // map changed, but packed, loose did not
            Assert.AreSame(refA_two, map.get("A"));
            Assert.AreSame(refA_one, packed.get(0));
            Assert.AreEqual(0, loose.size());

            Assert.AreSame(refA_two, map.put("A", refA_one));
            Assert.AreSame(refA_one, map.get("A"));
        }

        [Test]
        public void testToString_NoPrefix()
        {
            global::GitSharp.Core.Ref a = newRef("refs/heads/A", ID_ONE);
            global::GitSharp.Core.Ref b = newRef("refs/heads/B", ID_TWO);

            packed = toList(a, b);

            StringBuilder exp = new StringBuilder();
            exp.Append("[");
            exp.Append(a.ToString());
            exp.Append(", ");
            exp.Append(b.ToString());
            exp.Append("]");

            RefMap map = new RefMap("", packed, loose, resolved);
            Assert.AreEqual(exp.ToString(), map.ToString());
        }

        [Test]
        public void testToString_WithPrefix()
        {
            global::GitSharp.Core.Ref a = newRef("refs/heads/A", ID_ONE);
            global::GitSharp.Core.Ref b = newRef("refs/heads/foo/B", ID_TWO);
            global::GitSharp.Core.Ref c = newRef("refs/heads/foo/C", ID_TWO);
            global::GitSharp.Core.Ref g = newRef("refs/heads/g", ID_ONE);

            packed = toList(a, b, c, g);

            StringBuilder exp = new StringBuilder();
            exp.Append("[");
            exp.Append(b.ToString());
            exp.Append(", ");
            exp.Append(c.ToString());
            exp.Append("]");

            RefMap map = new RefMap("refs/heads/foo/", packed, loose, resolved);
            Assert.AreEqual(exp.ToString(), map.ToString());
        }

        [Test]
        public void testEntryType()
        {
            global::GitSharp.Core.Ref a = newRef("refs/heads/A", ID_ONE);
            global::GitSharp.Core.Ref b = newRef("refs/heads/B", ID_TWO);

            packed = toList(a, b);

            RefMap map = new RefMap("refs/heads/", packed, loose, resolved);
            IteratorBase<RefMap.Ent> itr = map.entrySet().iterator();
            RefMap.Ent ent_a = itr.next();
            RefMap.Ent ent_b = itr.next();

            Assert.AreEqual(ent_a.GetHashCode(), "A".GetHashCode());
            Assert.IsTrue(ent_a.Equals(ent_a));
            Assert.IsFalse(ent_a.Equals(ent_b));

            Assert.AreEqual(a.ToString(), ent_a.ToString());
        }

        [Test]
        public void testEntryTypeSet()
        {
            global::GitSharp.Core.Ref refA_one = newRef("refs/heads/A", ID_ONE);
            global::GitSharp.Core.Ref refA_two = newRef("refs/heads/A", ID_TWO);

            packed = toList(refA_one);

            RefMap map = new RefMap("refs/heads/", packed, loose, resolved);
            Assert.AreSame(refA_one, map.get("A"));

            RefMap.Ent ent = map.entrySet().iterator().next();
            Assert.AreEqual("A", ent.getKey());
            Assert.AreSame(refA_one, ent.getValue());

            Assert.AreSame(refA_one, ent.setValue(refA_two));
            Assert.AreSame(refA_two, ent.getValue());
            Assert.AreSame(refA_two, map.get("A"));
            Assert.AreEqual(1, map.size());
        }

        private RefList<global::GitSharp.Core.Ref> toList(params global::GitSharp.Core.Ref[] refs)
        {
            var b = new RefList<global::GitSharp.Core.Ref>.Builder<global::GitSharp.Core.Ref>(refs.Length);
            b.addAll(refs, 0, refs.Length);
            return b.toRefList();
        }

        private static global::GitSharp.Core.Ref newRef(string name, ObjectId id)
        {
            return new global::GitSharp.Core.Ref(global::GitSharp.Core.Ref.Storage.Loose, name, id);
        }
    }
}