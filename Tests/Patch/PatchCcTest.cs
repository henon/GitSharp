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
using NUnit.Framework;

namespace GitSharp.Tests.Patch
{
	[TestFixture]
	public class PatchCcTest : BasePatchTest
	{
		[Test]
		public void testParse_OneFileCc()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_OneFileCc.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().isEmpty());

			var cfh = (CombinedFileHeader)p.getFiles()[0];

			Assert.AreEqual("org.spearce.egit.ui/src/org/spearce/egit/ui/UIText.java", cfh.NewName);
			Assert.AreEqual(cfh.NewName, cfh.OldName);

			Assert.AreEqual(98, cfh.StartOffset);

			Assert.AreEqual(2, cfh.ParentCount);
			Assert.AreSame(cfh.getOldId(0), cfh.getOldId());
			Assert.AreEqual("169356b", cfh.getOldId(0).name());
			Assert.AreEqual("dd8c317", cfh.getOldId(1).name());
			Assert.AreEqual("fd85931", cfh.getNewId().name());

			Assert.AreEqual(cfh.getOldMode(0), cfh.GetOldMode());
			Assert.AreEqual(FileMode.RegularFile, cfh.getOldMode(0));
			Assert.AreEqual(FileMode.RegularFile, cfh.getOldMode(1));
			Assert.AreEqual(FileMode.ExecutableFile, cfh.NewMode);
			Assert.AreEqual(FileHeader.ChangeTypeEnum.MODIFY, cfh.getChangeType());
			Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, cfh.getPatchType());

			Assert.AreEqual(1, cfh.Hunks.Count);
			{
				var h = (CombinedHunkHeader)cfh.Hunks[0];

				Assert.AreSame(cfh, h.File);
				Assert.AreEqual(346, h.StartOffset);
				Assert.AreEqual(764, h.EndOffset);

				Assert.AreSame(h.GetOldImage(0), h.OldImage);
				Assert.AreSame(cfh.getOldId(0), h.GetOldImage(0).Id);
				Assert.AreSame(cfh.getOldId(1), h.GetOldImage(1).Id);

				Assert.AreEqual(55, h.GetOldImage(0).StartLine);
				Assert.AreEqual(12, h.GetOldImage(0).LineCount);
				Assert.AreEqual(3, h.GetOldImage(0).LinesAdded);
				Assert.AreEqual(0, h.GetOldImage(0).LinesDeleted);

				Assert.AreEqual(163, h.GetOldImage(1).StartLine);
				Assert.AreEqual(13, h.GetOldImage(1).LineCount);
				Assert.AreEqual(2, h.GetOldImage(1).LinesAdded);
				Assert.AreEqual(0, h.GetOldImage(1).LinesDeleted);

				Assert.AreEqual(163, h.NewStartLine);
				Assert.AreEqual(15, h.NewLineCount);

				Assert.AreEqual(10, h.LinesContext);
			}
		}

		[Test]
		public void testParse_CcNewFile()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_CcNewFile.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().isEmpty());

			var cfh = (CombinedFileHeader)p.getFiles()[0];

			Assert.AreSame(FileHeader.DEV_NULL, cfh.OldName);
			Assert.AreEqual("d", cfh.NewName);

			Assert.AreEqual(187, cfh.StartOffset);

			Assert.AreEqual(2, cfh.ParentCount);
			Assert.AreSame(cfh.getOldId(0), cfh.getOldId());
			Assert.AreEqual("0000000", cfh.getOldId(0).name());
			Assert.AreEqual("0000000", cfh.getOldId(1).name());
			Assert.AreEqual("4bcfe98", cfh.getNewId().name());

			Assert.AreSame(cfh.getOldMode(0), cfh.GetOldMode());
			Assert.AreSame(FileMode.Missing, cfh.getOldMode(0));
			Assert.AreSame(FileMode.Missing, cfh.getOldMode(1));
			Assert.AreSame(FileMode.RegularFile, cfh.NewMode);
			Assert.AreEqual(FileHeader.ChangeTypeEnum.ADD, cfh.getChangeType());
			Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, cfh.getPatchType());

			Assert.AreEqual(1, cfh.Hunks.Count);
			{
				var h = (CombinedHunkHeader)cfh.Hunks[0];

				Assert.AreSame(cfh, h.File);
				Assert.AreEqual(273, h.StartOffset);
				Assert.AreEqual(300, h.EndOffset);

				Assert.AreSame(h.GetOldImage(0), h.OldImage);
				Assert.AreSame(cfh.getOldId(0), h.GetOldImage(0).Id);
				Assert.AreSame(cfh.getOldId(1), h.GetOldImage(1).Id);

				Assert.AreEqual(1, h.GetOldImage(0).StartLine);
				Assert.AreEqual(0, h.GetOldImage(0).LineCount);
				Assert.AreEqual(1, h.GetOldImage(0).LinesAdded);
				Assert.AreEqual(0, h.GetOldImage(0).LinesDeleted);

				Assert.AreEqual(1, h.GetOldImage(1).StartLine);
				Assert.AreEqual(0, h.GetOldImage(1).LineCount);
				Assert.AreEqual(1, h.GetOldImage(1).LinesAdded);
				Assert.AreEqual(0, h.GetOldImage(1).LinesDeleted);

				Assert.AreEqual(1, h.NewStartLine);
				Assert.AreEqual(1, h.NewLineCount);

				Assert.AreEqual(0, h.LinesContext);
			}
		}

		[Test]
		public void testParse_CcDeleteFile()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_CcDeleteFile.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().isEmpty());

			var cfh = (CombinedFileHeader)p.getFiles()[0];

			Assert.AreEqual("a", cfh.OldName);
			Assert.AreSame(FileHeader.DEV_NULL, cfh.NewName);

			Assert.AreEqual(187, cfh.StartOffset);

			Assert.AreEqual(2, cfh.ParentCount);
			Assert.AreSame(cfh.getOldId(0), cfh.getOldId());
			Assert.AreEqual("7898192", cfh.getOldId(0).name());
			Assert.AreEqual("2e65efe", cfh.getOldId(1).name());
			Assert.AreEqual("0000000", cfh.getNewId().name());

			Assert.AreSame(cfh.getOldMode(0), cfh.GetOldMode());
			Assert.AreSame(FileMode.RegularFile, cfh.getOldMode(0));
			Assert.AreSame(FileMode.RegularFile, cfh.getOldMode(1));
			Assert.AreSame(FileMode.Missing, cfh.NewMode);
			Assert.AreEqual(FileHeader.ChangeTypeEnum.DELETE, cfh.getChangeType());
			Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, cfh.getPatchType());

			Assert.IsTrue(cfh.Hunks.isEmpty());
		}
	}
}