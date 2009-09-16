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

using System.Collections.Generic;
using System.IO;
using GitSharp.Exceptions;
using GitSharp.RevWalk;
using GitSharp.Tests.Util;
using GitSharp.Transport;
using Xunit;

namespace GitSharp.Tests.Transport
{
    public class BundleWriterTest : RepositoryTestCase
    {
		[Fact]
        public void testWrite0()
        {
            // Create a tiny bundle, (well one of) the first commits only
            byte[] bundle = MakeBundle("refs/heads/firstcommit", "42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", null);

            // Then we clone a new repo from that bundle and do a simple test. This
            // makes sure
            // we could Read the bundle we created.
            Repository newRepo = createNewEmptyRepo();
            FetchResult fetchResult = FetchFromBundle(newRepo, bundle);
            Ref advertisedRef = fetchResult.GetAdvertisedRef("refs/heads/firstcommit");

            // We expect firstcommit to appear by id
            Assert.Equal("42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", advertisedRef.ObjectId.Name);
            // ..and by name as the bundle created a new ref
            Assert.Equal("42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", newRepo.Resolve(("refs/heads/firstcommit")).Name);
        }

    	[Fact]					
        public void testWrite1()
        {
        	// Create a small bundle, an early commit
            byte[] bundle = MakeBundle("refs/heads/aa", db.Resolve("a").Name, null);

            // Then we clone a new repo from that bundle and do a simple test. This
            // makes sure
            // we could Read the bundle we created.
            Repository newRepo = createNewEmptyRepo();
            FetchResult fetchResult = FetchFromBundle(newRepo, bundle);
            Ref advertisedRef = fetchResult.GetAdvertisedRef("refs/heads/aa");

            Assert.Equal(db.Resolve("a").Name, advertisedRef.ObjectId.Name);
            Assert.Equal(db.Resolve("a").Name, newRepo.Resolve("refs/heads/aa").Name);
            Assert.Null(newRepo.Resolve("refs/heads/a"));

            // Next an incremental bundle
            bundle = MakeBundle(
                    "refs/heads/cc",
                    db.Resolve("c").Name,
                    new GitSharp.RevWalk.RevWalk(db).parseCommit(db.Resolve("a").ToObjectId()));

            fetchResult = FetchFromBundle(newRepo, bundle);
            advertisedRef = fetchResult.GetAdvertisedRef("refs/heads/cc");
            Assert.Equal(db.Resolve("c").Name, advertisedRef.ObjectId.Name);
            Assert.Equal(db.Resolve("c").Name, newRepo.Resolve("refs/heads/cc").Name);
            Assert.Null(newRepo.Resolve("refs/heads/c"));
            Assert.Null(newRepo.Resolve("refs/heads/a")); // still unknown

            try
            {
                // Check that we actually needed the first bundle
                Repository newRepo2 = createNewEmptyRepo();
                fetchResult = FetchFromBundle(newRepo2, bundle);
                Assert.False(true, "We should not be able to fetch from bundle with prerequisites that are not fulfilled");
            }
            catch (MissingBundlePrerequisiteException e)
            {
                Assert.True(e.Message.IndexOf(db.Resolve("refs/heads/a").Name) >= 0);
            }
        }

    	private static FetchResult FetchFromBundle(Repository newRepo, byte[] bundle)
        {
            var uri = new URIish("in-memory://");
            var @in = new MemoryStream(bundle);
            var rs = new RefSpec("refs/heads/*:refs/heads/*");
            var refs = new List<RefSpec>{rs};
            return new TransportBundleStream(newRepo, uri, @in).fetch(NullProgressMonitor.Instance, refs);
        }

        private byte[] MakeBundle(string name, string anObjectToInclude, RevCommit assume)
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
    }
}