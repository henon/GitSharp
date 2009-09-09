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

using GitSharp.Patch;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests.Patch
{
	public class PatchCcTest : BasePatchTest
	{
		[Fact]
		public void testParse_OneFileCc()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_OneFileCc.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.True(p.getErrors().isEmpty());

			var cfh = (CombinedFileHeader)p.getFiles()[0];

			Assert.Equal("org.spearce.egit.ui/src/org/spearce/egit/ui/UIText.java", cfh.getNewName());
			Assert.Equal(cfh.getNewName(), cfh.getOldName());

			Assert.Equal(98, cfh.startOffset);

			Assert.Equal(2, cfh.getParentCount());
			Assert.Same(cfh.getOldId(0), cfh.getOldId());
			Assert.Equal("169356b", cfh.getOldId(0).name());
			Assert.Equal("dd8c317", cfh.getOldId(1).name());
			Assert.Equal("fd85931", cfh.getNewId().name());

			Assert.Equal(cfh.getOldMode(0), cfh.getOldMode());
			Assert.Equal(FileMode.RegularFile, cfh.getOldMode(0));
			Assert.Equal(FileMode.RegularFile, cfh.getOldMode(1));
			Assert.Equal(FileMode.ExecutableFile, cfh.getNewMode());
			Assert.Equal(FileHeader.ChangeType.MODIFY, cfh.getChangeType());
			Assert.Equal(FileHeader.PatchType.UNIFIED, cfh.getPatchType());

			Assert.Equal(1, cfh.getHunks().Count);
			{
				var h = (CombinedHunkHeader)cfh.getHunks()[0];

				Assert.Same(cfh, h.File);
				Assert.Equal(346, h.StartOffset);
				Assert.Equal(764, h.EndOffset);

				Assert.Same(h.GetOldImage(0), h.OldImage);
				Assert.Same(cfh.getOldId(0), h.GetOldImage(0).Id);
				Assert.Same(cfh.getOldId(1), h.GetOldImage(1).Id);

				Assert.Equal(55, h.GetOldImage(0).StartLine);
				Assert.Equal(12, h.GetOldImage(0).LineCount);
				Assert.Equal(3, h.GetOldImage(0).LinesAdded);
				Assert.Equal(0, h.GetOldImage(0).LinesDeleted);

				Assert.Equal(163, h.GetOldImage(1).StartLine);
				Assert.Equal(13, h.GetOldImage(1).LineCount);
				Assert.Equal(2, h.GetOldImage(1).LinesAdded);
				Assert.Equal(0, h.GetOldImage(1).LinesDeleted);

				Assert.Equal(163, h.NewStartLine);
				Assert.Equal(15, h.NewLineCount);

				Assert.Equal(10, h.LinesContext);
			}
		}

		[Fact]
		public void testParse_CcNewFile()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_CcNewFile.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.True(p.getErrors().isEmpty());

			var cfh = (CombinedFileHeader)p.getFiles()[0];

			Assert.Same(FileHeader.DEV_NULL, cfh.getOldName());
			Assert.Equal("d", cfh.getNewName());

			Assert.Equal(187, cfh.startOffset);

			Assert.Equal(2, cfh.getParentCount());
			Assert.Same(cfh.getOldId(0), cfh.getOldId());
			Assert.Equal("0000000", cfh.getOldId(0).name());
			Assert.Equal("0000000", cfh.getOldId(1).name());
			Assert.Equal("4bcfe98", cfh.getNewId().name());

			Assert.Same(cfh.getOldMode(0), cfh.getOldMode());
			Assert.Same(FileMode.Missing, cfh.getOldMode(0));
			Assert.Same(FileMode.Missing, cfh.getOldMode(1));
			Assert.Same(FileMode.RegularFile, cfh.getNewMode());
			Assert.Equal(FileHeader.ChangeType.ADD, cfh.getChangeType());
			Assert.Equal(FileHeader.PatchType.UNIFIED, cfh.getPatchType());

			Assert.Equal(1, cfh.getHunks().Count);
			{
				var h = (CombinedHunkHeader)cfh.getHunks()[0];

				Assert.Same(cfh, h.File);
				Assert.Equal(273, h.StartOffset);
				Assert.Equal(300, h.EndOffset);

				Assert.Same(h.GetOldImage(0), h.OldImage);
				Assert.Same(cfh.getOldId(0), h.GetOldImage(0).Id);
				Assert.Same(cfh.getOldId(1), h.GetOldImage(1).Id);

				Assert.Equal(1, h.GetOldImage(0).StartLine);
				Assert.Equal(0, h.GetOldImage(0).LineCount);
				Assert.Equal(1, h.GetOldImage(0).LinesAdded);
				Assert.Equal(0, h.GetOldImage(0).LinesDeleted);

				Assert.Equal(1, h.GetOldImage(1).StartLine);
				Assert.Equal(0, h.GetOldImage(1).LineCount);
				Assert.Equal(1, h.GetOldImage(1).LinesAdded);
				Assert.Equal(0, h.GetOldImage(1).LinesDeleted);

				Assert.Equal(1, h.NewStartLine);
				Assert.Equal(1, h.NewLineCount);

				Assert.Equal(0, h.LinesContext);
			}
		}

		[Fact]
		public void testParse_CcDeleteFile()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_CcDeleteFile.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.True(p.getErrors().isEmpty());

			var cfh = (CombinedFileHeader)p.getFiles()[0];

			Assert.Equal("a", cfh.getOldName());
			Assert.Same(FileHeader.DEV_NULL, cfh.getNewName());

			Assert.Equal(187, cfh.startOffset);

			Assert.Equal(2, cfh.getParentCount());
			Assert.Same(cfh.getOldId(0), cfh.getOldId());
			Assert.Equal("7898192", cfh.getOldId(0).name());
			Assert.Equal("2e65efe", cfh.getOldId(1).name());
			Assert.Equal("0000000", cfh.getNewId().name());

			Assert.Same(cfh.getOldMode(0), cfh.getOldMode());
			Assert.Same(FileMode.RegularFile, cfh.getOldMode(0));
			Assert.Same(FileMode.RegularFile, cfh.getOldMode(1));
			Assert.Same(FileMode.Missing, cfh.getNewMode());
			Assert.Equal(FileHeader.ChangeType.DELETE, cfh.getChangeType());
			Assert.Equal(FileHeader.PatchType.UNIFIED, cfh.getPatchType());

			Assert.True(cfh.getHunks().isEmpty());
		}
	}
}