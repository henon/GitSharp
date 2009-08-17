/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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

namespace GitSharp.Tests.Transport
{
    [TestFixture]
    public class TransportTest : RepositoryTestCase
    {
#if false
	private Transport transport;

	private RemoteConfig remoteConfig;

	@Override
	public void setUp() throws Exception {
		super.setUp();
		final RepositoryConfig config = db.getConfig();
		remoteConfig = new RemoteConfig(config, "test");
		remoteConfig.addURI(new URIish("http://everyones.loves.git/u/2"));
		transport = null;
	}

	@Override
	protected void tearDown() throws Exception {
		if (transport != null) {
			transport.close();
			transport = null;
		}
		super.tearDown();
	}

	/**
	 * Test RefSpec to RemoteRefUpdate conversion with simple RefSpec - no
	 * wildcard, no tracking ref in repo configuration.
	 *
	 * @throws IOException
	 */
	public void testFindRemoteRefUpdatesNoWildcardNoTracking()
			throws IOException {
		transport = Transport.open(db, remoteConfig);
		final Collection<RemoteRefUpdate> result = transport
				.findRemoteRefUpdatesFor(Collections.nCopies(1, new RefSpec(
						"refs/heads/master:refs/heads/x")));

		assertEquals(1, result.size());
		final RemoteRefUpdate rru = result.iterator().next();
		assertNull(rru.getExpectedOldObjectId());
		assertFalse(rru.isForceUpdate());
		assertEquals("refs/heads/master", rru.getSrcRef());
		assertEquals(db.resolve("refs/heads/master"), rru.getNewObjectId());
		assertEquals("refs/heads/x", rru.getRemoteName());
	}

	/**
	 * Test RefSpec to RemoteRefUpdate conversion with no-destination RefSpec
	 * (destination should be set up for the same name as source).
	 *
	 * @throws IOException
	 */
	public void testFindRemoteRefUpdatesNoWildcardNoDestination()
			throws IOException {
		transport = Transport.open(db, remoteConfig);
		final Collection<RemoteRefUpdate> result = transport
				.findRemoteRefUpdatesFor(Collections.nCopies(1, new RefSpec(
						"+refs/heads/master")));

		assertEquals(1, result.size());
		final RemoteRefUpdate rru = result.iterator().next();
		assertNull(rru.getExpectedOldObjectId());
		assertTrue(rru.isForceUpdate());
		assertEquals("refs/heads/master", rru.getSrcRef());
		assertEquals(db.resolve("refs/heads/master"), rru.getNewObjectId());
		assertEquals("refs/heads/master", rru.getRemoteName());
	}

	/**
	 * Test RefSpec to RemoteRefUpdate conversion with wildcard RefSpec.
	 *
	 * @throws IOException
	 */
	public void testFindRemoteRefUpdatesWildcardNoTracking() throws IOException {
		transport = Transport.open(db, remoteConfig);
		final Collection<RemoteRefUpdate> result = transport
				.findRemoteRefUpdatesFor(Collections.nCopies(1, new RefSpec(
						"+refs/heads/*:refs/heads/test/*")));

		assertEquals(9, result.size());
		boolean foundA = false;
		boolean foundB = false;
		for (final RemoteRefUpdate rru : result) {
			if ("refs/heads/a".equals(rru.getSrcRef())
					&& "refs/heads/test/a".equals(rru.getRemoteName()))
				foundA = true;
			if ("refs/heads/b".equals(rru.getSrcRef())
					&& "refs/heads/test/b".equals(rru.getRemoteName()))
				foundB = true;
		}
		assertTrue(foundA);
		assertTrue(foundB);
	}

	/**
	 * Test RefSpec to RemoteRefUpdate conversion for more than one RefSpecs
	 * handling.
	 *
	 * @throws IOException
	 */
	public void testFindRemoteRefUpdatesTwoRefSpecs() throws IOException {
		transport = Transport.open(db, remoteConfig);
		final RefSpec specA = new RefSpec("+refs/heads/a:refs/heads/b");
		final RefSpec specC = new RefSpec("+refs/heads/c:refs/heads/d");
		final Collection<RefSpec> specs = Arrays.asList(specA, specC);
		final Collection<RemoteRefUpdate> result = transport
				.findRemoteRefUpdatesFor(specs);

		assertEquals(2, result.size());
		boolean foundA = false;
		boolean foundC = false;
		for (final RemoteRefUpdate rru : result) {
			if ("refs/heads/a".equals(rru.getSrcRef())
					&& "refs/heads/b".equals(rru.getRemoteName()))
				foundA = true;
			if ("refs/heads/c".equals(rru.getSrcRef())
					&& "refs/heads/d".equals(rru.getRemoteName()))
				foundC = true;
		}
		assertTrue(foundA);
		assertTrue(foundC);
	}

	/**
	 * Test RefSpec to RemoteRefUpdate conversion for tracking ref search.
	 *
	 * @throws IOException
	 */
	public void testFindRemoteRefUpdatesTrackingRef() throws IOException {
		remoteConfig.addFetchRefSpec(new RefSpec(
				"refs/heads/*:refs/remotes/test/*"));
		transport = Transport.open(db, remoteConfig);
		final Collection<RemoteRefUpdate> result = transport
				.findRemoteRefUpdatesFor(Collections.nCopies(1, new RefSpec(
						"+refs/heads/a:refs/heads/a")));

		assertEquals(1, result.size());
		final TrackingRefUpdate tru = result.iterator().next()
				.getTrackingRefUpdate();
		assertEquals("refs/remotes/test/a", tru.getLocalName());
		assertEquals("refs/heads/a", tru.getRemoteName());
		assertEquals(db.resolve("refs/heads/a"), tru.getNewObjectId());
		assertNull(tru.getOldObjectId());
	}
#endif
    }
}
