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
    public class PatchCcTest : BasePatchTest
    {
#if false
	public void testParse_OneFileCc() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		assertTrue(p.getErrors().isEmpty());

		final CombinedFileHeader cfh = (CombinedFileHeader) p.getFiles().get(0);

		assertEquals("org.spearce.egit.ui/src/org/spearce/egit/ui/UIText.java",
				cfh.getNewName());
		assertEquals(cfh.getNewName(), cfh.getOldName());

		assertEquals(98, cfh.startOffset);

		assertEquals(2, cfh.getParentCount());
		assertSame(cfh.getOldId(0), cfh.getOldId());
		assertEquals("169356b", cfh.getOldId(0).name());
		assertEquals("dd8c317", cfh.getOldId(1).name());
		assertEquals("fd85931", cfh.getNewId().name());

		assertSame(cfh.getOldMode(0), cfh.getOldMode());
		assertSame(FileMode.REGULAR_FILE, cfh.getOldMode(0));
		assertSame(FileMode.REGULAR_FILE, cfh.getOldMode(1));
		assertSame(FileMode.EXECUTABLE_FILE, cfh.getNewMode());
		assertSame(FileHeader.ChangeType.MODIFY, cfh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, cfh.getPatchType());

		assertEquals(1, cfh.getHunks().size());
		{
			final CombinedHunkHeader h = cfh.getHunks().get(0);

			assertSame(cfh, h.getFileHeader());
			assertEquals(346, h.startOffset);
			assertEquals(764, h.endOffset);

			assertSame(h.getOldImage(0), h.getOldImage());
			assertSame(cfh.getOldId(0), h.getOldImage(0).getId());
			assertSame(cfh.getOldId(1), h.getOldImage(1).getId());

			assertEquals(55, h.getOldImage(0).getStartLine());
			assertEquals(12, h.getOldImage(0).getLineCount());
			assertEquals(3, h.getOldImage(0).getLinesAdded());
			assertEquals(0, h.getOldImage(0).getLinesDeleted());

			assertEquals(163, h.getOldImage(1).getStartLine());
			assertEquals(13, h.getOldImage(1).getLineCount());
			assertEquals(2, h.getOldImage(1).getLinesAdded());
			assertEquals(0, h.getOldImage(1).getLinesDeleted());

			assertEquals(163, h.getNewStartLine());
			assertEquals(15, h.getNewLineCount());

			assertEquals(10, h.getLinesContext());
		}
	}

	public void testParse_CcNewFile() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		assertTrue(p.getErrors().isEmpty());

		final CombinedFileHeader cfh = (CombinedFileHeader) p.getFiles().get(0);

		assertSame(FileHeader.DEV_NULL, cfh.getOldName());
		assertEquals("d", cfh.getNewName());

		assertEquals(187, cfh.startOffset);

		assertEquals(2, cfh.getParentCount());
		assertSame(cfh.getOldId(0), cfh.getOldId());
		assertEquals("0000000", cfh.getOldId(0).name());
		assertEquals("0000000", cfh.getOldId(1).name());
		assertEquals("4bcfe98", cfh.getNewId().name());

		assertSame(cfh.getOldMode(0), cfh.getOldMode());
		assertSame(FileMode.MISSING, cfh.getOldMode(0));
		assertSame(FileMode.MISSING, cfh.getOldMode(1));
		assertSame(FileMode.REGULAR_FILE, cfh.getNewMode());
		assertSame(FileHeader.ChangeType.ADD, cfh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, cfh.getPatchType());

		assertEquals(1, cfh.getHunks().size());
		{
			final CombinedHunkHeader h = cfh.getHunks().get(0);

			assertSame(cfh, h.getFileHeader());
			assertEquals(273, h.startOffset);
			assertEquals(300, h.endOffset);

			assertSame(h.getOldImage(0), h.getOldImage());
			assertSame(cfh.getOldId(0), h.getOldImage(0).getId());
			assertSame(cfh.getOldId(1), h.getOldImage(1).getId());

			assertEquals(1, h.getOldImage(0).getStartLine());
			assertEquals(0, h.getOldImage(0).getLineCount());
			assertEquals(1, h.getOldImage(0).getLinesAdded());
			assertEquals(0, h.getOldImage(0).getLinesDeleted());

			assertEquals(1, h.getOldImage(1).getStartLine());
			assertEquals(0, h.getOldImage(1).getLineCount());
			assertEquals(1, h.getOldImage(1).getLinesAdded());
			assertEquals(0, h.getOldImage(1).getLinesDeleted());

			assertEquals(1, h.getNewStartLine());
			assertEquals(1, h.getNewLineCount());

			assertEquals(0, h.getLinesContext());
		}
	}

	public void testParse_CcDeleteFile() throws IOException {
		final Patch p = parseTestPatchFile();
		assertEquals(1, p.getFiles().size());
		assertTrue(p.getErrors().isEmpty());

		final CombinedFileHeader cfh = (CombinedFileHeader) p.getFiles().get(0);

		assertEquals("a", cfh.getOldName());
		assertSame(FileHeader.DEV_NULL, cfh.getNewName());

		assertEquals(187, cfh.startOffset);

		assertEquals(2, cfh.getParentCount());
		assertSame(cfh.getOldId(0), cfh.getOldId());
		assertEquals("7898192", cfh.getOldId(0).name());
		assertEquals("2e65efe", cfh.getOldId(1).name());
		assertEquals("0000000", cfh.getNewId().name());

		assertSame(cfh.getOldMode(0), cfh.getOldMode());
		assertSame(FileMode.REGULAR_FILE, cfh.getOldMode(0));
		assertSame(FileMode.REGULAR_FILE, cfh.getOldMode(1));
		assertSame(FileMode.MISSING, cfh.getNewMode());
		assertSame(FileHeader.ChangeType.DELETE, cfh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, cfh.getPatchType());

		assertTrue(cfh.getHunks().isEmpty());
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
