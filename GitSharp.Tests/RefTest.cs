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
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
	/**
	 * Misc tests for refs. A lot of things are tested elsewhere so not having a
	 * test for a ref related method, does not mean it is untested.
	 */
	[TestFixture]
	public class RefTest : SampleDataRepositoryTestCase
	{
		[Test]
		public virtual void testReadAllIncludingSymrefs()
		{
			ObjectId masterId = db.Resolve("refs/heads/master");
			RefUpdate updateRef = db.UpdateRef("refs/remotes/origin/master");
			updateRef.NewObjectId = masterId;
			updateRef.IsForceUpdate = true;
			updateRef.Update();
			db.WriteSymref("refs/remotes/origin/HEAD", "refs/remotes/origin/master");

			ObjectId r = db.Resolve("refs/remotes/origin/HEAD");
			Assert.AreEqual(masterId, r);

            IDictionary<string, Core.Ref> allRefs = db.getAllRefs();
            Core.Ref refHEAD = allRefs["refs/remotes/origin/HEAD"];
			Assert.IsNotNull(refHEAD);
			Assert.AreEqual(masterId, refHEAD.ObjectId);
			Assert.IsTrue(refHEAD.Peeled);
			Assert.IsNull(refHEAD.PeeledObjectId);

            Core.Ref refmaster = allRefs["refs/remotes/origin/master"];
			Assert.AreEqual(masterId, refmaster.ObjectId);
			Assert.IsFalse(refmaster.Peeled);
			Assert.IsNull(refmaster.PeeledObjectId);
		}

		[Test]
		public virtual void testReadSymRefToPacked()
		{
			db.WriteSymref("HEAD", "refs/heads/b");
            Core.Ref @ref = db.getRef("HEAD");
            Assert.AreEqual(Core.Ref.Storage.LoosePacked, @ref.StorageFormat);
		}

		[Test]
		public void testReadSymRefToLoosePacked()
		{
			ObjectId pid = db.Resolve("refs/heads/master^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = pid;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, update); // internal

			db.WriteSymref("HEAD", "refs/heads/master");
            Core.Ref @ref = db.getRef("HEAD");
            Assert.AreEqual(Core.Ref.Storage.LoosePacked, @ref.StorageFormat);
		}

		[Test]
		public void testReadLooseRef()
		{
			RefUpdate updateRef = db.UpdateRef("ref/heads/new");
			updateRef.NewObjectId = db.Resolve("refs/heads/master");
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.New, update);
            Core.Ref @ref = db.getRef("ref/heads/new");
            Assert.AreEqual(Core.Ref.Storage.Loose, @ref.StorageFormat);
		}

		/// <summary>
		/// Let an "outsider" Create a loose ref with the same name as a packed one
		/// </summary>
		[Test]
		public void testReadLoosePackedRef()
		{
            Core.Ref @ref = db.getRef("refs/heads/master");
            Assert.AreEqual(Core.Ref.Storage.Packed, @ref.StorageFormat);
			string path = Path.Combine(db.Directory.FullName, "refs/heads/master");
			FileStream os = new FileStream(path, System.IO.FileMode.OpenOrCreate);
            byte[] buffer = @ref.ObjectId.Name.getBytes();
			os.Write(buffer, 0, buffer.Length);
			os.WriteByte(Convert.ToByte('\n'));
			os.Close();

			@ref = db.getRef("refs/heads/master");
            Assert.AreEqual(Core.Ref.Storage.LoosePacked, @ref.StorageFormat);
		}

		///	<summary>
		/// Modify a packed ref using the API. This creates a loose ref too, ie. LOOSE_PACKED
		///	</summary>
		[Test]
		public void testReadSimplePackedRefSameRepo()
		{
            Core.Ref @ref = db.getRef("refs/heads/master");
			ObjectId pid = db.Resolve("refs/heads/master^");
            Assert.AreEqual(Core.Ref.Storage.Packed, @ref.StorageFormat);
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = pid;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, update);

			@ref = db.getRef("refs/heads/master");
            Assert.AreEqual(Core.Ref.Storage.LoosePacked, @ref.StorageFormat);
		}

	    [Test]
	    public void testOrigResolvedNamesBranch()
	    {
            Core.Ref @ref = db.getRef("a");
	        Assert.AreEqual("refs/heads/a", @ref.Name);
	        Assert.AreEqual("refs/heads/a", @ref.OriginalName);
	    }

	    [Test]
	    public void testOrigResolvedNamesSymRef()
	    {
            Core.Ref @ref = db.getRef("HEAD");
	        Assert.AreEqual("refs/heads/master", @ref.Name);
            Assert.AreEqual("HEAD", @ref.OriginalName);
	    }
	}
}