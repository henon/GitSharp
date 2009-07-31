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

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests
{
    [TestFixture]
    public class PatchTest
    {
#if false
	public void testEmpty() {
		final Patch p = new Patch();
		assertTrue(p.getFiles().isEmpty());
		assertTrue(p.getErrors().isEmpty());
	}

	public void testParse_ConfigCaseInsensitive() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(2, p.getFiles().size());
		assertTrue(p.getErrors().isEmpty());

		final FileHeader fRepositoryConfigTest = p.getFiles().get(0);
		final FileHeader fRepositoryConfig = p.getFiles().get(1);

		assertEquals(
				"org.spearce.jgit.test/tst/org/spearce/jgit/lib/RepositoryConfigTest.java",
				fRepositoryConfigTest.getNewName());

		assertEquals(
				"org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java",
				fRepositoryConfig.getNewName());

		assertEquals(572, fRepositoryConfigTest.startOffset);
		assertEquals(1490, fRepositoryConfig.startOffset);

		assertEquals("da7e704", fRepositoryConfigTest.getOldId().name());
		assertEquals("34ce04a", fRepositoryConfigTest.getNewId().name());
		assertSame(FileHeader.PatchType.UNIFIED, fRepositoryConfigTest
				.getPatchType());
		assertSame(FileMode.REGULAR_FILE, fRepositoryConfigTest.getOldMode());
		assertSame(FileMode.REGULAR_FILE, fRepositoryConfigTest.getNewMode());
		assertEquals(1, fRepositoryConfigTest.getHunks().size());
		{
			final HunkHeader h = fRepositoryConfigTest.getHunks().get(0);
			assertSame(fRepositoryConfigTest, h.getFileHeader());
			assertEquals(921, h.startOffset);
			assertEquals(109, h.getOldImage().getStartLine());
			assertEquals(4, h.getOldImage().getLineCount());
			assertEquals(109, h.getNewStartLine());
			assertEquals(11, h.getNewLineCount());

			assertEquals(4, h.getLinesContext());
			assertEquals(7, h.getOldImage().getLinesAdded());
			assertEquals(0, h.getOldImage().getLinesDeleted());
			assertSame(fRepositoryConfigTest.getOldId(), h.getOldImage()
					.getId());

			assertEquals(1490, h.endOffset);
		}

		assertEquals("45c2f8a", fRepositoryConfig.getOldId().name());
		assertEquals("3291bba", fRepositoryConfig.getNewId().name());
		assertSame(FileHeader.PatchType.UNIFIED, fRepositoryConfig
				.getPatchType());
		assertSame(FileMode.REGULAR_FILE, fRepositoryConfig.getOldMode());
		assertSame(FileMode.REGULAR_FILE, fRepositoryConfig.getNewMode());
		assertEquals(3, fRepositoryConfig.getHunks().size());
		{
			final HunkHeader h = fRepositoryConfig.getHunks().get(0);
			assertSame(fRepositoryConfig, h.getFileHeader());
			assertEquals(1803, h.startOffset);
			assertEquals(236, h.getOldImage().getStartLine());
			assertEquals(9, h.getOldImage().getLineCount());
			assertEquals(236, h.getNewStartLine());
			assertEquals(9, h.getNewLineCount());

			assertEquals(7, h.getLinesContext());
			assertEquals(2, h.getOldImage().getLinesAdded());
			assertEquals(2, h.getOldImage().getLinesDeleted());
			assertSame(fRepositoryConfig.getOldId(), h.getOldImage().getId());

			assertEquals(2434, h.endOffset);
		}
		{
			final HunkHeader h = fRepositoryConfig.getHunks().get(1);
			assertEquals(2434, h.startOffset);
			assertEquals(300, h.getOldImage().getStartLine());
			assertEquals(7, h.getOldImage().getLineCount());
			assertEquals(300, h.getNewStartLine());
			assertEquals(7, h.getNewLineCount());

			assertEquals(6, h.getLinesContext());
			assertEquals(1, h.getOldImage().getLinesAdded());
			assertEquals(1, h.getOldImage().getLinesDeleted());

			assertEquals(2816, h.endOffset);
		}
		{
			final HunkHeader h = fRepositoryConfig.getHunks().get(2);
			assertEquals(2816, h.startOffset);
			assertEquals(954, h.getOldImage().getStartLine());
			assertEquals(7, h.getOldImage().getLineCount());
			assertEquals(954, h.getNewStartLine());
			assertEquals(7, h.getNewLineCount());

			assertEquals(6, h.getLinesContext());
			assertEquals(1, h.getOldImage().getLinesAdded());
			assertEquals(1, h.getOldImage().getLinesDeleted());

			assertEquals(3035, h.endOffset);
		}
	}

	public void testParse_NoBinary() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(5, p.getFiles().size());
		assertTrue(p.getErrors().isEmpty());

		for (int i = 0; i < 4; i++) {
			final FileHeader fh = p.getFiles().get(i);
			assertSame(FileHeader.ChangeType.ADD, fh.getChangeType());
			assertNotNull(fh.getOldId());
			assertNotNull(fh.getNewId());
			assertEquals("0000000", fh.getOldId().name());
			assertSame(FileMode.MISSING, fh.getOldMode());
			assertSame(FileMode.REGULAR_FILE, fh.getNewMode());
			assertTrue(fh.getNewName().startsWith(
					"org.spearce.egit.ui/icons/toolbar/"));
			assertSame(FileHeader.PatchType.BINARY, fh.getPatchType());
			assertTrue(fh.getHunks().isEmpty());
			assertTrue(fh.hasMetaDataChanges());

			assertNull(fh.getForwardBinaryHunk());
			assertNull(fh.getReverseBinaryHunk());
		}

		final FileHeader fh = p.getFiles().get(4);
		assertEquals("org.spearce.egit.ui/plugin.xml", fh.getNewName());
		assertSame(FileHeader.ChangeType.MODIFY, fh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		assertFalse(fh.hasMetaDataChanges());
		assertEquals("ee8a5a0", fh.getNewId().name());
		assertNull(fh.getForwardBinaryHunk());
		assertNull(fh.getReverseBinaryHunk());
		assertEquals(1, fh.getHunks().size());
		assertEquals(272, fh.getHunks().get(0).getOldImage().getStartLine());
	}

	public void testParse_GitBinaryLiteral() throws IOException {
		final Patch p = parseTestPatchFile();
		final int[] binsizes = { 359, 393, 372, 404 };
		assertEquals(5, p.getFiles().size());
		assertTrue(p.getErrors().isEmpty());

		for (int i = 0; i < 4; i++) {
			final FileHeader fh = p.getFiles().get(i);
			assertSame(FileHeader.ChangeType.ADD, fh.getChangeType());
			assertNotNull(fh.getOldId());
			assertNotNull(fh.getNewId());
			assertEquals(ObjectId.zeroId().name(), fh.getOldId().name());
			assertSame(FileMode.REGULAR_FILE, fh.getNewMode());
			assertTrue(fh.getNewName().startsWith(
					"org.spearce.egit.ui/icons/toolbar/"));
			assertSame(FileHeader.PatchType.GIT_BINARY, fh.getPatchType());
			assertTrue(fh.getHunks().isEmpty());
			assertTrue(fh.hasMetaDataChanges());

			final BinaryHunk fwd = fh.getForwardBinaryHunk();
			final BinaryHunk rev = fh.getReverseBinaryHunk();
			assertNotNull(fwd);
			assertNotNull(rev);
			assertEquals(binsizes[i], fwd.getSize());
			assertEquals(0, rev.getSize());

			assertSame(fh, fwd.getFileHeader());
			assertSame(fh, rev.getFileHeader());

			assertSame(BinaryHunk.Type.LITERAL_DEFLATED, fwd.getType());
			assertSame(BinaryHunk.Type.LITERAL_DEFLATED, rev.getType());
		}

		final FileHeader fh = p.getFiles().get(4);
		assertEquals("org.spearce.egit.ui/plugin.xml", fh.getNewName());
		assertSame(FileHeader.ChangeType.MODIFY, fh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		assertFalse(fh.hasMetaDataChanges());
		assertEquals("ee8a5a0", fh.getNewId().name());
		assertNull(fh.getForwardBinaryHunk());
		assertNull(fh.getReverseBinaryHunk());
		assertEquals(1, fh.getHunks().size());
		assertEquals(272, fh.getHunks().get(0).getOldImage().getStartLine());
	}

	public void testParse_GitBinaryDelta() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		assertTrue(p.getErrors().isEmpty());

		final FileHeader fh = p.getFiles().get(0);
		assertTrue(fh.getNewName().startsWith("zero.bin"));
		assertSame(FileHeader.ChangeType.MODIFY, fh.getChangeType());
		assertSame(FileHeader.PatchType.GIT_BINARY, fh.getPatchType());
		assertSame(FileMode.REGULAR_FILE, fh.getNewMode());

		assertNotNull(fh.getOldId());
		assertNotNull(fh.getNewId());
		assertEquals("08e7df176454f3ee5eeda13efa0adaa54828dfd8", fh.getOldId()
				.name());
		assertEquals("d70d8710b6d32ff844af0ee7c247e4b4b051867f", fh.getNewId()
				.name());

		assertTrue(fh.getHunks().isEmpty());
		assertFalse(fh.hasMetaDataChanges());

		final BinaryHunk fwd = fh.getForwardBinaryHunk();
		final BinaryHunk rev = fh.getReverseBinaryHunk();
		assertNotNull(fwd);
		assertNotNull(rev);
		assertEquals(12, fwd.getSize());
		assertEquals(11, rev.getSize());

		assertSame(fh, fwd.getFileHeader());
		assertSame(fh, rev.getFileHeader());

		assertSame(BinaryHunk.Type.DELTA_DEFLATED, fwd.getType());
		assertSame(BinaryHunk.Type.DELTA_DEFLATED, rev.getType());

		assertEquals(496, fh.endOffset);
	}

	public void testParse_FixNoNewline() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		assertTrue(p.getErrors().isEmpty());

		final FileHeader f = p.getFiles().get(0);

		assertEquals("a", f.getNewName());
		assertEquals(252, f.startOffset);

		assertEquals("2e65efe", f.getOldId().name());
		assertEquals("f2ad6c7", f.getNewId().name());
		assertSame(FileHeader.PatchType.UNIFIED, f.getPatchType());
		assertSame(FileMode.REGULAR_FILE, f.getOldMode());
		assertSame(FileMode.REGULAR_FILE, f.getNewMode());
		assertEquals(1, f.getHunks().size());
		{
			final HunkHeader h = f.getHunks().get(0);
			assertSame(f, h.getFileHeader());
			assertEquals(317, h.startOffset);
			assertEquals(1, h.getOldImage().getStartLine());
			assertEquals(1, h.getOldImage().getLineCount());
			assertEquals(1, h.getNewStartLine());
			assertEquals(1, h.getNewLineCount());

			assertEquals(0, h.getLinesContext());
			assertEquals(1, h.getOldImage().getLinesAdded());
			assertEquals(1, h.getOldImage().getLinesDeleted());
			assertSame(f.getOldId(), h.getOldImage().getId());

			assertEquals(363, h.endOffset);
		}
	}

	public void testParse_AddNoNewline() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		assertTrue(p.getErrors().isEmpty());

		final FileHeader f = p.getFiles().get(0);

		assertEquals("a", f.getNewName());
		assertEquals(256, f.startOffset);

		assertEquals("f2ad6c7", f.getOldId().name());
		assertEquals("c59d9b6", f.getNewId().name());
		assertSame(FileHeader.PatchType.UNIFIED, f.getPatchType());
		assertSame(FileMode.REGULAR_FILE, f.getOldMode());
		assertSame(FileMode.REGULAR_FILE, f.getNewMode());
		assertEquals(1, f.getHunks().size());
		{
			final HunkHeader h = f.getHunks().get(0);
			assertSame(f, h.getFileHeader());
			assertEquals(321, h.startOffset);
			assertEquals(1, h.getOldImage().getStartLine());
			assertEquals(1, h.getOldImage().getLineCount());
			assertEquals(1, h.getNewStartLine());
			assertEquals(1, h.getNewLineCount());

			assertEquals(0, h.getLinesContext());
			assertEquals(1, h.getOldImage().getLinesAdded());
			assertEquals(1, h.getOldImage().getLinesDeleted());
			assertSame(f.getOldId(), h.getOldImage().getId());

			assertEquals(367, h.endOffset);
		}
	}

	private Patch parseTestPatchFile() throws IOException {
		final String patchFile = getName() + ".patch";
		final InputStream in = getClass().getResourceAsStream(patchFile);
		if (in == null) {
			fail("No " + patchFile + " test vector");
			return null; // Never happens
		}
		try {
			final Patch p = new Patch();
			p.parse(in);
			return p;
		} finally {
			in.close();
		}
	}
#endif
    }
}
