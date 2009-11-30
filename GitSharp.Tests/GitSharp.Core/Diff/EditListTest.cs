/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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

using NUnit.Framework;
using GitSharp.Core.Diff;
using System.Collections;

namespace GitSharp.Core.Tests.Diff
{
    [TestFixture]
    public class EditListTest
    {
        [Test]
        public void testEmpty()
        {
            EditList l = new EditList();
            Assert.AreEqual(0, l.size());
            Assert.IsTrue(l.isEmpty());
            Assert.AreEqual("EditList[]", l.ToString());

            Assert.IsTrue(l.Equals(l));
            Assert.IsTrue(l.Equals(new EditList()));
            Assert.IsFalse(l.Equals(string.Empty));
            Assert.AreEqual(l.GetHashCode(), new EditList().GetHashCode());
        }

        [Test]
        public void testAddOne()
        {
            Edit e = new Edit(1, 2, 1, 1);
            EditList l = new EditList();
            l.Add(e);
            Assert.AreEqual(1, l.size());
            Assert.IsFalse(l.isEmpty());
            Assert.AreSame(e, l.get(0));
            IEnumerator i = l.GetEnumerator();
            i.Reset();
            i.MoveNext();
            Assert.AreSame(e, i.Current);

            Assert.IsTrue(l.Equals(l));
            Assert.IsFalse(l.Equals(new EditList()));

            EditList l2 = new EditList();
            l2.Add(e);
            Assert.IsTrue(l.Equals(l2));
            Assert.IsTrue(l2.Equals(l));
            Assert.AreEqual(l.GetHashCode(), l2.GetHashCode());
        }

        [Test]
        public void testAddTwo()
        {
            Edit e1 = new Edit(1, 2, 1, 1);
            Edit e2 = new Edit(8, 8, 8, 12);
            EditList l = new EditList();
            l.Add(e1);
            l.Add(e2);
            Assert.AreEqual(2, l.size());
            Assert.AreSame(e1, l.get(0));
            Assert.AreSame(e2, l.get(1));

            IEnumerator i = l.GetEnumerator();
            i.Reset();
            i.MoveNext();
            Assert.AreSame(e1, i.Current);
            i.MoveNext();
            Assert.AreSame(e2, i.Current);

            Assert.IsTrue(l.Equals(l));
            Assert.IsFalse(l.Equals(new EditList()));

            EditList l2 = new EditList();
            l2.Add(e1);
            l2.Add(e2);
            Assert.IsTrue(l.Equals(l2));
            Assert.IsTrue(l2.Equals(l));
            Assert.AreEqual(l.GetHashCode(), l2.GetHashCode());
        }

        [Test]
        public void testSet()
        {
            Edit e1 = new Edit(1, 2, 1, 1);
            Edit e2 = new Edit(3, 4, 3, 3);
            EditList l = new EditList();
            l.Add(e1);
            Assert.AreSame(e1, l.get(0));
            Assert.AreSame(e1, l.set(0, e2));
            Assert.AreSame(e2, l.get(0));
        }

        [Test]
        public void testRemove()
        {
            Edit e1 = new Edit(1, 2, 1, 1);
            Edit e2 = new Edit(8, 8, 8, 12);
            EditList l = new EditList();
            l.Add(e1);
            l.Add(e2);
            l.Remove(e1);
            Assert.AreEqual(1, l.size());
            Assert.AreSame(e2, l.get(0));
        }
    }
}