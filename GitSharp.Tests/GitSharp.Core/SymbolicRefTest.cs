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
using GitSharp.Core;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core
{
    [TestFixture]
    public class SymbolicRefTest
    {
        private static ObjectId ID_A = ObjectId
            .FromString("41eb0d88f833b558bddeb269b7ab77399cdf98ed");

        private static ObjectId ID_B = ObjectId
            .FromString("698dd0b8d0c299f080559a1cffc7fe029479a408");

        private static string targetName = "refs/heads/a.test.ref";

        private static string name = "refs/remotes/origin/HEAD";

        [Test]
        public void testConstructor()
        {
            global::GitSharp.Core.Ref t;
            SymbolicRef r;

            t = new Unpeeled(Storage.New, targetName, null);
            r = new SymbolicRef(name, t);
            Assert.AreSame(Storage.Loose, r.getStorage());
            Assert.AreSame(name, r.getName());
            Assert.IsNull(r.getObjectId(), "no id on new ref");
            Assert.IsFalse(r.isPeeled(), "not peeled");
            Assert.IsNull(r.getPeeledObjectId(), "no peel id");
            Assert.AreSame(t, r.getLeaf(), "leaf is t");
            Assert.AreSame(t, r.getTarget(), "target is t");
            Assert.IsTrue(r.isSymbolic(), "is symbolic");

            t = new Unpeeled(Storage.Packed, targetName, ID_A);
            r = new SymbolicRef(name, t);
            Assert.AreSame(Storage.Loose, r.getStorage());
            Assert.AreSame(name, r.getName());
            Assert.AreSame(ID_A, r.getObjectId());
            Assert.IsFalse(r.isPeeled(), "not peeled");
            Assert.IsNull(r.getPeeledObjectId(), "no peel id");
            Assert.AreSame(t, r.getLeaf(), "leaf is t");
            Assert.AreSame(t, r.getTarget(), "target is t");
            Assert.IsTrue(r.isSymbolic(), "is symbolic");
        }

        [Test]
        public void testLeaf()
        {
            global::GitSharp.Core.Ref a;
            SymbolicRef b, c, d;

            a = new PeeledTag(Storage.Packed, targetName, ID_A, ID_B);
            b = new SymbolicRef("B", a);
            c = new SymbolicRef("C", b);
            d = new SymbolicRef("D", c);

            Assert.AreSame(c, d.getTarget());
            Assert.AreSame(b, c.getTarget());
            Assert.AreSame(a, b.getTarget());

            Assert.AreSame(a, d.getLeaf());
            Assert.AreSame(a, c.getLeaf());
            Assert.AreSame(a, b.getLeaf());
            Assert.AreSame(a, a.getLeaf());

            Assert.AreSame(ID_A, d.getObjectId());
            Assert.AreSame(ID_A, c.getObjectId());
            Assert.AreSame(ID_A, b.getObjectId());

            Assert.IsTrue(d.isPeeled());
            Assert.IsTrue(c.isPeeled());
            Assert.IsTrue(b.isPeeled());

            Assert.AreSame(ID_B, d.getPeeledObjectId());
            Assert.AreSame(ID_B, c.getPeeledObjectId());
            Assert.AreSame(ID_B, b.getPeeledObjectId());
        }

        [Test]
        public void testToString()
        {
            global::GitSharp.Core.Ref a;
            SymbolicRef b, c, d;

            a = new PeeledTag(Storage.Packed, targetName, ID_A, ID_B);
            b = new SymbolicRef("B", a);
            c = new SymbolicRef("C", b);
            d = new SymbolicRef("D", c);

            Assert.AreEqual("SymbolicRef[D -> C -> B -> " + targetName + "="
                            + ID_A.Name + "]", d.ToString());
        }
    }
}