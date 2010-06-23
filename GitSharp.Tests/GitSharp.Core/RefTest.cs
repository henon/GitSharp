/*
 * Copyright (C) 2009, Robin Rosenberg
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


using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Core;
using GitSharp.Core.Tests;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core
{
    /**
     * Misc tests for refs. A lot of things are tested elsewhere so not having a
     * test for a ref related method, does not mean it is untested.
     */
    [TestFixture]
    public class RefTest : SampleDataRepositoryTestCase
    {
        private void writeSymref(String src, String dst)
        {
            RefUpdate u = db.UpdateRef(src);
            switch (u.link(dst))
            {
                case RefUpdate.RefUpdateResult.NEW:
                case RefUpdate.RefUpdateResult.FORCED:
                case RefUpdate.RefUpdateResult.NO_CHANGE:
                    break;
                default:
                    Assert.Fail("link " + src + " to " + dst);
                    break;
            }
        }

        [Test]
        public virtual void testReadAllIncludingSymrefs()
        {
            ObjectId masterId = db.Resolve("refs/heads/master");
            RefUpdate updateRef = db.UpdateRef("refs/remotes/origin/master");
            updateRef.setNewObjectId(masterId);
            updateRef.setForceUpdate(true);
            updateRef.update();
            writeSymref("refs/remotes/origin/HEAD", "refs/remotes/origin/master");

            ObjectId r = db.Resolve("refs/remotes/origin/HEAD");
            Assert.AreEqual(masterId, r);

            IDictionary<string, global::GitSharp.Core.Ref> allRefs = db.getAllRefs();
            global::GitSharp.Core.Ref refHEAD = allRefs["refs/remotes/origin/HEAD"];
            Assert.IsNotNull(refHEAD);
            Assert.AreEqual(masterId, refHEAD.ObjectId);
            Assert.IsFalse(refHEAD.IsPeeled);
            Assert.IsNull(refHEAD.PeeledObjectId);

            global::GitSharp.Core.Ref refmaster = allRefs["refs/remotes/origin/master"];
            Assert.AreEqual(masterId, refmaster.ObjectId);
            Assert.IsFalse(refmaster.IsPeeled);
            Assert.IsNull(refmaster.PeeledObjectId);
        }

        [Test]
        public virtual void testReadSymRefToPacked()
        {
            writeSymref("HEAD", "refs/heads/b");
            global::GitSharp.Core.Ref @ref = db.getRef("HEAD");
            Assert.AreEqual(Storage.Loose, @ref.StorageFormat);
            Assert.IsTrue(@ref.isSymbolic(), "is symref");
            @ref = @ref.getTarget();
            Assert.AreEqual("refs/heads/b", @ref.Name);
            Assert.AreEqual(Storage.Packed, @ref.StorageFormat);
        }

        [Test]
        public void testReadSymRefToLoosePacked()
        {
            ObjectId pid = db.Resolve("refs/heads/master^");
            RefUpdate updateRef = db.UpdateRef("refs/heads/master");
            updateRef.setNewObjectId(pid);
            updateRef.setForceUpdate(true);
            RefUpdate.RefUpdateResult update = updateRef.update();
            Assert.AreEqual(RefUpdate.RefUpdateResult.FORCED, update); // internal

            writeSymref("HEAD", "refs/heads/master");
            global::GitSharp.Core.Ref @ref = db.getRef("HEAD");
            Assert.AreEqual(Storage.Loose, @ref.StorageFormat);
            @ref = @ref.getTarget();
            Assert.AreEqual("refs/heads/master", @ref.Name);
            Assert.AreEqual(Storage.Loose, @ref.StorageFormat);
        }

        [Test]
        public void testReadLooseRef()
        {
            RefUpdate updateRef = db.UpdateRef("ref/heads/new");
            updateRef.setNewObjectId(db.Resolve("refs/heads/master"));
            RefUpdate.RefUpdateResult update = updateRef.update();
            Assert.AreEqual(RefUpdate.RefUpdateResult.NEW, update);
            global::GitSharp.Core.Ref @ref = db.getRef("ref/heads/new");
            Assert.AreEqual(Storage.Loose, @ref.StorageFormat);
        }

        /// <summary>
        /// Let an "outsider" Create a loose ref with the same name as a packed one
        /// </summary>
        [Test]
        public void testReadLoosePackedRef()
        {
            global::GitSharp.Core.Ref @ref = db.getRef("refs/heads/master");
            Assert.AreEqual(Storage.Packed, @ref.StorageFormat);
            string path = Path.Combine(db.Directory.FullName, "refs/heads/master");
            using (FileStream os = new FileStream(path, System.IO.FileMode.OpenOrCreate))
            {
                byte[] buffer = @ref.ObjectId.Name.getBytes();
                os.Write(buffer, 0, buffer.Length);
                os.WriteByte(Convert.ToByte('\n'));
            }

            @ref = db.getRef("refs/heads/master");
            Assert.AreEqual(Storage.Loose, @ref.StorageFormat);
        }

        ///	<summary>
        /// Modify a packed ref using the API. This creates a loose ref too, ie. LOOSE_PACKED
        ///	</summary>
        [Test]
        public void testReadSimplePackedRefSameRepo()
        {
            global::GitSharp.Core.Ref @ref = db.getRef("refs/heads/master");
            ObjectId pid = db.Resolve("refs/heads/master^");
            Assert.AreEqual(Storage.Packed, @ref.StorageFormat);
            RefUpdate updateRef = db.UpdateRef("refs/heads/master");
            updateRef.setNewObjectId(pid);
            updateRef.setForceUpdate(true);
            RefUpdate.RefUpdateResult update = updateRef.update();
            Assert.AreEqual(RefUpdate.RefUpdateResult.FORCED, update);

            @ref = db.getRef("refs/heads/master");
            Assert.AreEqual(Storage.Loose, @ref.StorageFormat);
        }

        [Test]
        public void testResolvedNamesBranch()
        {
            global::GitSharp.Core.Ref @ref = db.getRef("a");
            Assert.AreEqual("refs/heads/a", @ref.Name);
        }

        [Test]
        public void testResolvedNamesSymRef()
        {
            global::GitSharp.Core.Ref @ref = db.getRef(Constants.HEAD);
            Assert.AreEqual(Constants.HEAD, @ref.Name);
            Assert.IsTrue(@ref.isSymbolic(), "is symbolic ref");
            Assert.AreSame(Storage.Loose, @ref.StorageFormat);

            global::GitSharp.Core.Ref dst = @ref.getTarget();
            Assert.IsNotNull(dst, "has target");
            Assert.AreEqual("refs/heads/master", dst.Name);

            Assert.AreSame(dst.ObjectId, @ref.ObjectId);
            Assert.AreSame(dst.PeeledObjectId, @ref.PeeledObjectId);
            Assert.AreEqual(dst.IsPeeled, @ref.IsPeeled);
        }
    }
}