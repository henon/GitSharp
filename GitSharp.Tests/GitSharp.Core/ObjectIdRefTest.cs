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
    public class ObjectIdRefTest
    {
        private static ObjectId ID_A = ObjectId
            .FromString("41eb0d88f833b558bddeb269b7ab77399cdf98ed");

        private static ObjectId ID_B = ObjectId
            .FromString("698dd0b8d0c299f080559a1cffc7fe029479a408");

        private static string name = "refs/heads/a.test.ref";

        [Test]
        public void testConstructor_PeeledStatusNotKnown()
        {
            ObjectIdRef r;

            r = new Unpeeled(Storage.Loose, name, ID_A);
            Assert.AreSame(Storage.Loose, r.getStorage());
            Assert.AreSame(name, r.getName());
            Assert.AreSame(ID_A, r.getObjectId());
            Assert.IsFalse(r.isPeeled(), "not peeled");
            Assert.IsNull(r.getPeeledObjectId(), "no peel id");
            Assert.AreSame(r, r.getLeaf(), "leaf is this");
            Assert.AreSame(r, r.getTarget(), "target is this");
            Assert.IsFalse(r.isSymbolic(), "not symbolic");

            r = new Unpeeled(Storage.Packed, name, ID_A);
            Assert.AreSame(Storage.Packed, r.getStorage());

            r = new Unpeeled(Storage.LoosePacked, name, ID_A);
            Assert.AreSame(Storage.LoosePacked, r.getStorage());

            r = new Unpeeled(Storage.New, name, null);
            Assert.AreSame(Storage.New, r.getStorage());
            Assert.AreSame(name, r.getName());
            Assert.IsNull(r.getObjectId(), "no id on new ref");
            Assert.IsFalse(r.isPeeled(), "not peeled");
            Assert.IsNull(r.getPeeledObjectId(), "no peel id");
            Assert.AreSame(r, r.getLeaf(), "leaf is this");
            Assert.AreSame(r, r.getTarget(), "target is this");
            Assert.IsFalse(r.isSymbolic(), "not symbolic");
        }

        [Test]
        public void testConstructor_Peeled()
        {
            ObjectIdRef r;

            r = new Unpeeled(Storage.Loose, name, ID_A);
            Assert.AreSame(Storage.Loose, r.getStorage());
            Assert.AreSame(name, r.getName());
            Assert.AreSame(ID_A, r.getObjectId());
            Assert.IsFalse(r.isPeeled(), "not peeled");
            Assert.IsNull(r.getPeeledObjectId(), "no peel id");
            Assert.AreSame(r, r.getLeaf(), "leaf is this");
            Assert.AreSame(r, r.getTarget(), "target is this");
            Assert.IsFalse(r.isSymbolic(), "not symbolic");

            r = new PeeledNonTag(Storage.Loose, name, ID_A);
            Assert.IsTrue(r.isPeeled(), "is peeled");
            Assert.IsNull(r.getPeeledObjectId(), "no peel id");

            r = new PeeledTag(Storage.Loose, name, ID_A, ID_B);
            Assert.IsTrue(r.isPeeled(), "is peeled");
            Assert.AreSame(ID_B, r.getPeeledObjectId());
        }

        [Test]
        public void testToString()
        {
            ObjectIdRef r;

            r = new Unpeeled(Storage.Loose, name, ID_A);
            Assert.AreEqual("Ref[" + name + "=" + ID_A.Name + "]", r.ToString());
        }
    }
}