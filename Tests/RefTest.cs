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
using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests
{

    /**
     * Misc tests for refs. A lot of things are tested elsewhere so not having a
     * test for a ref related method, does not mean it is untested.
     */
    [TestFixture]
    public class RefTest : RepositoryTestCase
    {
#if false
	public void testReadAllIncludingSymrefs() throws Exception {
		ObjectId masterId = db.resolve("refs/heads/master");
		RefUpdate updateRef = db.updateRef("refs/remotes/origin/master");
		updateRef.setNewObjectId(masterId);
		updateRef.setForceUpdate(true);
		updateRef.update();
		db
				.writeSymref("refs/remotes/origin/HEAD",
						"refs/remotes/origin/master");

		ObjectId r = db.resolve("refs/remotes/origin/HEAD");
		assertEquals(masterId, r);

		Map<String, Ref> allRefs = db.getAllRefs();
		Ref refHEAD = allRefs.get("refs/remotes/origin/HEAD");
		assertNotNull(refHEAD);
		assertEquals(masterId, refHEAD.getObjectId());
		assertTrue(refHEAD.isPeeled());
		assertNull(refHEAD.getPeeledObjectId());

		Ref refmaster = allRefs.get("refs/remotes/origin/master");
		assertEquals(masterId, refmaster.getObjectId());
		assertFalse(refmaster.isPeeled());
		assertNull(refmaster.getPeeledObjectId());
	}

	public void testReadSymRefToPacked() throws IOException {
		db.writeSymref("HEAD", "refs/heads/b");
		Ref ref = db.getRef("HEAD");
		assertEquals(Ref.Storage.LOOSE_PACKED, ref.getStorage());
	}

	public void testReadSymRefToLoosePacked() throws IOException {
		ObjectId pid = db.resolve("refs/heads/master^");
		RefUpdate updateRef = db.updateRef("refs/heads/master");
		updateRef.setNewObjectId(pid);
		updateRef.setForceUpdate(true);
		Result update = updateRef.update();
		assertEquals(Result.FORCED, update); // internal

		db.writeSymref("HEAD", "refs/heads/master");
		Ref ref = db.getRef("HEAD");
		assertEquals(Ref.Storage.LOOSE_PACKED, ref.getStorage());
	}

	public void testReadLooseRef() throws IOException {
		RefUpdate updateRef = db.updateRef("ref/heads/new");
		updateRef.setNewObjectId(db.resolve("refs/heads/master"));
		Result update = updateRef.update();
		assertEquals(Result.NEW, update);
		Ref ref = db.getRef("ref/heads/new");
		assertEquals(Storage.LOOSE, ref.getStorage());
	}

	/**
	 * Let an "outsider" create a loose ref with the same name as a packed one
	 *
	 * @throws IOException
	 * @throws InterruptedException
	 */
	public void testReadLoosePackedRef() throws IOException,
			InterruptedException {
		Ref ref = db.getRef("refs/heads/master");
		assertEquals(Storage.PACKED, ref.getStorage());
		FileOutputStream os = new FileOutputStream(new File(db.getDirectory(),
				"refs/heads/master"));
		os.write(ref.getObjectId().name().getBytes());
		os.write('\n');
		os.close();

		ref = db.getRef("refs/heads/master");
		assertEquals(Storage.LOOSE_PACKED, ref.getStorage());
	}

	/**
	 * Modify a packed ref using the API. This creates a loose ref too, ie.
	 * LOOSE_PACKED
	 *
	 * @throws IOException
	 */
	public void testReadSimplePackedRefSameRepo() throws IOException {
		Ref ref = db.getRef("refs/heads/master");
		ObjectId pid = db.resolve("refs/heads/master^");
		assertEquals(Storage.PACKED, ref.getStorage());
		RefUpdate updateRef = db.updateRef("refs/heads/master");
		updateRef.setNewObjectId(pid);
		updateRef.setForceUpdate(true);
		Result update = updateRef.update();
		assertEquals(Result.FORCED, update);

		ref = db.getRef("refs/heads/master");
		assertEquals(Storage.LOOSE_PACKED, ref.getStorage());
	}
#endif
    }
}
