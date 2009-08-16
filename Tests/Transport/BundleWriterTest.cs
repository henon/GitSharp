/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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

using System.IO;
using GitSharp.RevWalk;
using GitSharp.Transport;
using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests
{
    [TestFixture]
    public class BundleWriterTest : RepositoryTestCase
    {

        [Test]
        public void test001_Write()
        {
            byte[] bundle = makeBundle("refs/heads/firstcommit", "42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", null);

            Repository newRepo = createNewEmptyRepo();
            FetchResult fetchResult = fetchFromBundle(newRepo, bundle);
            Ref advertisedRef = fetchResult.GetAdvertisedRef("refs/heads/firstcommit");

            Assert.AreEqual("42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", advertisedRef.ObjectId.Name);
            Assert.AreEqual("42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", newRepo.Resolve("refs/heads/firstcommit").Name);
        }

#if false
	/**
	 * Incremental bundle test
	 * 
	 * @throws Exception
	 */
	public void testWrite1() throws Exception {
		byte[] bundle;

		// Create a small bundle, an early commit
		bundle = makeBundle("refs/heads/aa", db.resolve("a").name(), null);

		// Then we clone a new repo from that bundle and do a simple test. This
		// makes sure
		// we could read the bundle we created.
		Repository newRepo = createNewEmptyRepo();
		FetchResult fetchResult = fetchFromBundle(newRepo, bundle);
		Ref advertisedRef = fetchResult.getAdvertisedRef("refs/heads/aa");

		assertEquals(db.resolve("a").name(), advertisedRef.getObjectId().name());
		assertEquals(db.resolve("a").name(), newRepo.resolve("refs/heads/aa")
				.name());
		assertNull(newRepo.resolve("refs/heads/a"));

		// Next an incremental bundle
		bundle = makeBundle("refs/heads/cc", db.resolve("c").name(),
				new RevWalk(db).parseCommit(db.resolve("a").toObjectId()));
		fetchResult = fetchFromBundle(newRepo, bundle);
		advertisedRef = fetchResult.getAdvertisedRef("refs/heads/cc");
		assertEquals(db.resolve("c").name(), advertisedRef.getObjectId().name());
		assertEquals(db.resolve("c").name(), newRepo.resolve("refs/heads/cc")
				.name());
		assertNull(newRepo.resolve("refs/heads/c"));
		assertNull(newRepo.resolve("refs/heads/a")); // still unknown

		try {
			// Check that we actually needed the first bundle
			Repository newRepo2 = createNewEmptyRepo();
			fetchResult = fetchFromBundle(newRepo2, bundle);
			fail("We should not be able to fetch from bundle with prerequisites that are not fulfilled");
		} catch (MissingBundlePrerequisiteException e) {
			assertTrue(e.getMessage()
					.indexOf(db.resolve("refs/heads/a").name()) >= 0);
		}
	}
#endif

        private FetchResult fetchFromBundle(Repository newRepo, byte[] bundle)
        {
            URIish uri = new URIish("in-memory://");
            MemoryStream i = new MemoryStream(bundle);
            RefSpec rs = new RefSpec("refs/heads/*:refs/heads/*");
            List<RefSpec> refs = new List<RefSpec>{rs};
            return new TransportBundleStream(newRepo, uri, i).fetch(new NullProgressMonitor(), refs);
        }

        private byte[] makeBundle(string name, string anObjectToInclude, RevCommit assume)
        {
            BundleWriter bw;
            bw = new BundleWriter(db, new NullProgressMonitor());
            bw.include(name, ObjectId.FromString(anObjectToInclude));
            if (assume != null)
                bw.assume(assume);
            MemoryStream o = new MemoryStream();
            bw.writeBundle(o);
            return o.ToArray();
        }
    }
}
