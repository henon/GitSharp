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
		[StrictFactAttribute]
		public void testParse_OneFileCc()
		{
			GitSharp.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_OneFileCc.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.True(p.getErrors().isEmpty());

			var cfh = (CombinedFileHeader)p.getFiles()[0];

			Assert.Equal("org.spearce.egit.ui/src/org/spearce/egit/ui/UIText.java", cfh.NewName);
			Assert.Equal(cfh.NewName, cfh.OldName);

			Assert.Equal(98, cfh.StartOffset);

			Assert.Equal(2, cfh.ParentCount);
			Assert.Same(cfh.getOldId(0), cfh.getOldId());
			Assert.Equal("169356b", cfh.getOldId(0).name());
			Assert.Equal("dd8c317", cfh.getOldId(1).name());
			Assert.Equal("fd85931", cfh.getNewId().name());

			Assert.Equal(cfh.getOldMode(0), cfh.GetOldMode());
			Assert.Equal(FileMode.RegularFile, cfh.getOldMode(0));
			Assert.Equal(FileMode.RegularFile, cfh.getOldMode(1));
			Assert.Equal(FileMode.ExecutableFile, cfh.NewMode);
			Assert.Equal(FileHeader.ChangeTypeEnum.MODIFY, cfh.getChangeType());
			Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, cfh.getPatchType());

			Assert.Equal(1, cfh.Hunks.Count);
			{
				var h = (CombinedHunkHeader)cfh.Hunks[0];

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

		[StrictFactAttribute]
		public void testParse_CcNewFile()
		{
			GitSharp.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_CcNewFile.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.True(p.getErrors().isEmpty());

			var cfh = (CombinedFileHeader)p.getFiles()[0];

			Assert.Same(FileHeader.DEV_NULL, cfh.OldName);
			Assert.Equal("d", cfh.NewName);

			Assert.Equal(187, cfh.StartOffset);

			Assert.Equal(2, cfh.ParentCount);
			Assert.Same(cfh.getOldId(0), cfh.getOldId());
			Assert.Equal("0000000", cfh.getOldId(0).name());
			Assert.Equal("0000000", cfh.getOldId(1).name());
			Assert.Equal("4bcfe98", cfh.getNewId().name());

			Assert.Same(cfh.getOldMode(0), cfh.GetOldMode());
			Assert.Same(FileMode.Missing, cfh.getOldMode(0));
			Assert.Same(FileMode.Missing, cfh.getOldMode(1));
			Assert.Same(FileMode.RegularFile, cfh.NewMode);
			Assert.Equal(FileHeader.ChangeTypeEnum.ADD, cfh.getChangeType());
			Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, cfh.getPatchType());

			Assert.Equal(1, cfh.Hunks.Count);
			{
				var h = (CombinedHunkHeader)cfh.Hunks[0];

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

		[StrictFactAttribute]
		public void testParse_CcDeleteFile()
		{
			GitSharp.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_CcDeleteFile.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.True(p.getErrors().isEmpty());

			var cfh = (CombinedFileHeader)p.getFiles()[0];

			Assert.Equal("a", cfh.OldName);
			Assert.Same(FileHeader.DEV_NULL, cfh.NewName);

			Assert.Equal(187, cfh.StartOffset);

			Assert.Equal(2, cfh.ParentCount);
			Assert.Same(cfh.getOldId(0), cfh.getOldId());
			Assert.Equal("7898192", cfh.getOldId(0).name());
			Assert.Equal("2e65efe", cfh.getOldId(1).name());
			Assert.Equal("0000000", cfh.getNewId().name());

			Assert.Same(cfh.getOldMode(0), cfh.GetOldMode());
			Assert.Same(FileMode.RegularFile, cfh.getOldMode(0));
			Assert.Same(FileMode.RegularFile, cfh.getOldMode(1));
			Assert.Same(FileMode.Missing, cfh.NewMode);
			Assert.Equal(FileHeader.ChangeTypeEnum.DELETE, cfh.getChangeType());
			Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, cfh.getPatchType());

			Assert.True(cfh.Hunks.isEmpty());
		}
	}
}