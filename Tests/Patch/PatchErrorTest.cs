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

namespace GitSharp.Tests.Patch
{
    [TestFixture]
    public class PatchErrorTest
    {
#if false
	public void testError_DisconnectedHunk() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		{
			final FileHeader fh = p.getFiles().get(0);
			assertEquals(
					"org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java",
					fh.getNewName());
			assertEquals(1, fh.getHunks().size());
		}

		assertEquals(1, p.getErrors().size());
		final FormatError e = p.getErrors().get(0);
		assertSame(FormatError.Severity.ERROR, e.getSeverity());
		assertEquals("Hunk disconnected from file", e.getMessage());
		assertEquals(18, e.getOffset());
		assertTrue(e.getLineText().startsWith("@@ -109,4 +109,11 @@ assert"));
	}

	public void testError_TruncatedOld() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		assertEquals(1, p.getErrors().size());

		final FormatError e = p.getErrors().get(0);
		assertSame(FormatError.Severity.ERROR, e.getSeverity());
		assertEquals("Truncated hunk, at least 1 old lines is missing", e
				.getMessage());
		assertEquals(313, e.getOffset());
		assertTrue(e.getLineText().startsWith("@@ -236,9 +236,9 @@ protected "));
	}

	public void testError_TruncatedNew() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		assertEquals(1, p.getErrors().size());

		final FormatError e = p.getErrors().get(0);
		assertSame(FormatError.Severity.ERROR, e.getSeverity());
		assertEquals("Truncated hunk, at least 1 new lines is missing", e
				.getMessage());
		assertEquals(313, e.getOffset());
		assertTrue(e.getLineText().startsWith("@@ -236,9 +236,9 @@ protected "));
	}

	public void testError_BodyTooLong() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		assertEquals(1, p.getErrors().size());

		final FormatError e = p.getErrors().get(0);
		assertSame(FormatError.Severity.WARNING, e.getSeverity());
		assertEquals("Hunk header 4:11 does not match body line count of 4:12",
				e.getMessage());
		assertEquals(349, e.getOffset());
		assertTrue(e.getLineText().startsWith("@@ -109,4 +109,11 @@ assert"));
	}

	public void testError_GarbageBetweenFiles() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(2, p.getFiles().size());
		{
			final FileHeader fh = p.getFiles().get(0);
			assertEquals(
					"org.spearce.jgit.test/tst/org/spearce/jgit/lib/RepositoryConfigTest.java",
					fh.getNewName());
			assertEquals(1, fh.getHunks().size());
		}
		{
			final FileHeader fh = p.getFiles().get(1);
			assertEquals(
					"org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java",
					fh.getNewName());
			assertEquals(1, fh.getHunks().size());
		}

		assertEquals(1, p.getErrors().size());
		final FormatError e = p.getErrors().get(0);
		assertSame(FormatError.Severity.WARNING, e.getSeverity());
		assertEquals("Unexpected hunk trailer", e.getMessage());
		assertEquals(926, e.getOffset());
		assertEquals("I AM NOT HERE\n", e.getLineText());
	}

	public void testError_GitBinaryNoForwardHunk() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(2, p.getFiles().size());
		{
			final FileHeader fh = p.getFiles().get(0);
			assertEquals("org.spearce.egit.ui/icons/toolbar/fetchd.png", fh
					.getNewName());
			assertSame(FileHeader.PatchType.GIT_BINARY, fh.getPatchType());
			assertTrue(fh.getHunks().isEmpty());
			assertNull(fh.getForwardBinaryHunk());
		}
		{
			final FileHeader fh = p.getFiles().get(1);
			assertEquals("org.spearce.egit.ui/icons/toolbar/fetche.png", fh
					.getNewName());
			assertSame(FileHeader.PatchType.UNIFIED, fh.getPatchType());
			assertTrue(fh.getHunks().isEmpty());
			assertNull(fh.getForwardBinaryHunk());
		}

		assertEquals(1, p.getErrors().size());
		final FormatError e = p.getErrors().get(0);
		assertSame(FormatError.Severity.ERROR, e.getSeverity());
		assertEquals("Missing forward-image in GIT binary patch", e
				.getMessage());
		assertEquals(297, e.getOffset());
		assertEquals("\n", e.getLineText());
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
