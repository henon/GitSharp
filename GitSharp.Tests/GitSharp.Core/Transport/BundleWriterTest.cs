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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitSharp.Core;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Transport;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Transport
{
    [TestFixture]
    public class BundleWriterTest : SampleDataRepositoryTestCase
    {
        private List<TransportBundleStream>	 _transportBundleStreams = new List<TransportBundleStream>();

        #region Test methods

        #region testWrite0

        public override void tearDown()
        {
            _transportBundleStreams.ForEach((t) => t.Dispose());

            base.tearDown();
        }

        [Test]
        public void testWrite0()
        {
            // Create a tiny bundle, (well one of) the first commits only
            byte[] bundle = makeBundle("refs/heads/firstcommit", "42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", null);

            // Then we clone a new repo from that bundle and do a simple test. This
            // makes sure
            // we could Read the bundle we created.
            Core.Repository newRepo = createBareRepository();
            FetchResult fetchResult = fetchFromBundle(newRepo, bundle);
            Core.Ref advertisedRef = fetchResult.GetAdvertisedRef("refs/heads/firstcommit");

            // We expect firstcommit to appear by id
            Assert.AreEqual("42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", advertisedRef.ObjectId.Name);
            // ..and by name as the bundle created a new ref
            Assert.AreEqual("42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", newRepo.Resolve(("refs/heads/firstcommit")).Name);
        }

        #endregion

        /**
         * Incremental bundle test
         * 
         * @throws Exception
         */

        #region testWrite1

        [Test]
        public void testWrite1()
        {
        	// Create a small bundle, an early commit
            byte[] bundle = makeBundle("refs/heads/aa", db.Resolve("a").Name, null);

            // Then we clone a new repo from that bundle and do a simple test. This
            // makes sure
            // we could Read the bundle we created.
            Core.Repository newRepo = createBareRepository();
            FetchResult fetchResult = fetchFromBundle(newRepo, bundle);
            Core.Ref advertisedRef = fetchResult.GetAdvertisedRef("refs/heads/aa");

            Assert.AreEqual(db.Resolve("a").Name, advertisedRef.ObjectId.Name);
            Assert.AreEqual(db.Resolve("a").Name, newRepo.Resolve("refs/heads/aa").Name);
            Assert.IsNull(newRepo.Resolve("refs/heads/a"));

            // Next an incremental bundle
            bundle = makeBundle(
                    "refs/heads/cc",
                    db.Resolve("c").Name,
                    new GitSharp.Core.RevWalk.RevWalk(db).parseCommit(db.Resolve("a").ToObjectId()));

            fetchResult = fetchFromBundle(newRepo, bundle);
            advertisedRef = fetchResult.GetAdvertisedRef("refs/heads/cc");
            Assert.AreEqual(db.Resolve("c").Name, advertisedRef.ObjectId.Name);
            Assert.AreEqual(db.Resolve("c").Name, newRepo.Resolve("refs/heads/cc").Name);
            Assert.IsNull(newRepo.Resolve("refs/heads/c"));
            Assert.IsNull(newRepo.Resolve("refs/heads/a")); // still unknown

            try
            {
                // Check that we actually needed the first bundle
                Core.Repository newRepo2 = createBareRepository();
                fetchResult = fetchFromBundle(newRepo2, bundle);
                Assert.Fail("We should not be able to fetch from bundle with prerequisites that are not fulfilled");
            }
            catch (MissingBundlePrerequisiteException e)
            {
                Assert.IsTrue(e.Message.IndexOf(db.Resolve("refs/heads/a").Name) >= 0);
            }
        }

        #endregion

        #endregion

        #region Other methods

        #region fetchFromBundle

        private FetchResult fetchFromBundle(Core.Repository newRepo, byte[] bundle)
        {
            var uri = new URIish("in-memory://");
            var @in = new MemoryStream(bundle);
            var rs = new RefSpec("refs/heads/*:refs/heads/*");
            var refs = new List<RefSpec>{rs};
            var transportBundleStream = new TransportBundleStream(newRepo, uri, @in);
            
            _transportBundleStreams.Add(transportBundleStream);

            return transportBundleStream.fetch(NullProgressMonitor.Instance, refs);
        }

        #endregion

        #region makeBundle

        private byte[] makeBundle(string name, string anObjectToInclude, RevCommit assume)
        {
            var bw = new BundleWriter(db, NullProgressMonitor.Instance);
            bw.include(name, ObjectId.FromString(anObjectToInclude));
            if (assume != null)
            {
                bw.assume(assume);
            }
            var @out = new MemoryStream();
            bw.writeBundle(@out);
            return @out.ToArray();
        }

        #endregion

        #endregion
    }
}