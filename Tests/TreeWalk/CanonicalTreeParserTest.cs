/*
 * Copyright (C) 2008, Google Inc.
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

using GitSharp.TreeWalk;
namespace GitSharp.Tests.TreeWalk
{

    using NUnit.Framework;
    [TestFixture]
    public class CanonicalTreeParserTest
    {
#if false
	private final CanonicalTreeParser ctp = new CanonicalTreeParser();

	private final FileMode m644 = FileMode.REGULAR_FILE;

	private final FileMode mt = FileMode.TREE;

	private final ObjectId hash_a = ObjectId
			.fromString("6b9c715d21d5486e59083fb6071566aa6ecd4d42");

	private final ObjectId hash_foo = ObjectId
			.fromString("a213e8e25bb2442326e86cbfb9ef56319f482869");

	private final ObjectId hash_sometree = ObjectId
			.fromString("daf4bdb0d7bb24319810fe0e73aa317663448c93");

	private byte[] tree1;

	private byte[] tree2;

	private byte[] tree3;

	public void setUp() throws Exception {
		super.setUp();

		tree1 = mkree(entry(m644, "a", hash_a));
		tree2 = mkree(entry(m644, "a", hash_a), entry(m644, "foo", hash_foo));
		tree3 = mkree(entry(m644, "a", hash_a), entry(mt, "b_sometree",
				hash_sometree), entry(m644, "foo", hash_foo));
	}

	private static byte[] mkree(final byte[]... data) throws Exception {
		final ByteArrayOutputStream out = new ByteArrayOutputStream();
		for (final byte[] e : data)
			out.write(e);
		return out.toByteArray();
	}

	private static byte[] entry(final FileMode mode, final String name,
			final ObjectId id) throws Exception {
		final ByteArrayOutputStream out = new ByteArrayOutputStream();
		mode.copyTo(out);
		out.write(' ');
		out.write(Constants.encode(name));
		out.write(0);
		id.copyRawTo(out);
		return out.toByteArray();
	}

	private String path() {
		return RawParseUtils.decode(Constants.CHARSET, ctp.path,
				ctp.pathOffset, ctp.pathLen);
	}

	public void testEmptyTree_AtEOF() throws Exception {
		ctp.reset(new byte[0]);
		assertTrue(ctp.eof());
	}

	public void testOneEntry_Forward() throws Exception {
		ctp.reset(tree1);

		assertTrue(ctp.first());
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("a", path());
		assertEquals(hash_a, ctp.getEntryObjectId());

		ctp.next(1);
		assertFalse(ctp.first());
		assertTrue(ctp.eof());
	}

	public void testTwoEntries_ForwardOneAtATime() throws Exception {
		ctp.reset(tree2);

		assertTrue(ctp.first());
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("a", path());
		assertEquals(hash_a, ctp.getEntryObjectId());

		ctp.next(1);
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("foo", path());
		assertEquals(hash_foo, ctp.getEntryObjectId());

		ctp.next(1);
		assertFalse(ctp.first());
		assertTrue(ctp.eof());
	}

	public void testOneEntry_Seek1IsEOF() throws Exception {
		ctp.reset(tree1);
		ctp.next(1);
		assertTrue(ctp.eof());
	}

	public void testTwoEntries_Seek2IsEOF() throws Exception {
		ctp.reset(tree2);
		ctp.next(2);
		assertTrue(ctp.eof());
	}

	public void testThreeEntries_Seek3IsEOF() throws Exception {
		ctp.reset(tree3);
		ctp.next(3);
		assertTrue(ctp.eof());
	}

	public void testThreeEntries_Seek2() throws Exception {
		ctp.reset(tree3);

		ctp.next(2);
		assertFalse(ctp.eof());
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("foo", path());
		assertEquals(hash_foo, ctp.getEntryObjectId());

		ctp.next(1);
		assertTrue(ctp.eof());
	}

	public void testOneEntry_Backwards() throws Exception {
		ctp.reset(tree1);
		ctp.next(1);
		assertFalse(ctp.first());
		assertTrue(ctp.eof());

		ctp.back(1);
		assertTrue(ctp.first());
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("a", path());
		assertEquals(hash_a, ctp.getEntryObjectId());
	}

	public void testTwoEntries_BackwardsOneAtATime() throws Exception {
		ctp.reset(tree2);
		ctp.next(2);
		assertTrue(ctp.eof());

		ctp.back(1);
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("foo", path());
		assertEquals(hash_foo, ctp.getEntryObjectId());

		ctp.back(1);
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("a", path());
		assertEquals(hash_a, ctp.getEntryObjectId());
	}

	public void testTwoEntries_BackwardsTwo() throws Exception {
		ctp.reset(tree2);
		ctp.next(2);
		assertTrue(ctp.eof());

		ctp.back(2);
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("a", path());
		assertEquals(hash_a, ctp.getEntryObjectId());

		ctp.next(1);
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("foo", path());
		assertEquals(hash_foo, ctp.getEntryObjectId());

		ctp.next(1);
		assertTrue(ctp.eof());
	}

	public void testThreeEntries_BackwardsTwo() throws Exception {
		ctp.reset(tree3);
		ctp.next(3);
		assertTrue(ctp.eof());

		ctp.back(2);
		assertFalse(ctp.eof());
		assertEquals(mt.getBits(), ctp.mode);
		assertEquals("b_sometree", path());
		assertEquals(hash_sometree, ctp.getEntryObjectId());

		ctp.next(1);
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("foo", path());
		assertEquals(hash_foo, ctp.getEntryObjectId());

		ctp.next(1);
		assertTrue(ctp.eof());
	}

	public void testBackwards_ConfusingPathName() throws Exception {
		final String aVeryConfusingName = "confusing 644 entry 755 and others";
		ctp.reset(mkree(entry(m644, "a", hash_a), entry(mt, aVeryConfusingName,
				hash_sometree), entry(m644, "foo", hash_foo)));
		ctp.next(3);
		assertTrue(ctp.eof());

		ctp.back(2);
		assertFalse(ctp.eof());
		assertEquals(mt.getBits(), ctp.mode);
		assertEquals(aVeryConfusingName, path());
		assertEquals(hash_sometree, ctp.getEntryObjectId());

		ctp.back(1);
		assertFalse(ctp.eof());
		assertEquals(m644.getBits(), ctp.mode);
		assertEquals("a", path());
		assertEquals(hash_a, ctp.getEntryObjectId());
	}

	public void testFreakingHugePathName() throws Exception {
		final int n = AbstractTreeIterator.DEFAULT_PATH_SIZE * 4;
		final StringBuilder b = new StringBuilder(n);
		for (int i = 0; i < n; i++)
			b.append('q');
		final String name = b.toString();
		ctp.reset(entry(m644, name, hash_a));
		assertFalse(ctp.eof());
		assertEquals(name, RawParseUtils.decode(Constants.CHARSET, ctp.path,
				ctp.pathOffset, ctp.pathLen));
	}
#endif
    }
}
