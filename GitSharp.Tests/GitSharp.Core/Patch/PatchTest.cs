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

using GitSharp.Core;
using GitSharp.Core.Patch;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Patch
{
	[TestFixture]
	public class PatchTest : BasePatchTest
	{
		[Test]
		public void testEmpty()
		{
			var patch = new GitSharp.Core.Patch.Patch();
			Assert.IsTrue(patch.getFiles().Count == 0);
			Assert.IsTrue(patch.getErrors().Count == 0);
		}

		[Test]
		public void testParse_ConfigCaseInsensitive()
		{
			GitSharp.Core.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_ConfigCaseInsensitive.patch");
			Assert.AreEqual(2, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			FileHeader fRepositoryConfigTest = p.getFiles()[0];
			FileHeader fRepositoryConfig = p.getFiles()[1];

			Assert.AreEqual(
					"org.spearce.jgit.test/tst/org/spearce/jgit/lib/RepositoryConfigTest.java",
					fRepositoryConfigTest.NewName);

			Assert.AreEqual(
					"org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java",
					fRepositoryConfig.NewName);

			Assert.AreEqual(572, fRepositoryConfigTest.StartOffset);
			Assert.AreEqual(1490, fRepositoryConfig.StartOffset);

			Assert.AreEqual("da7e704", fRepositoryConfigTest.getOldId().name());
			Assert.AreEqual("34ce04a", fRepositoryConfigTest.getNewId().name());
			Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fRepositoryConfigTest.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, fRepositoryConfigTest.GetOldMode());
			Assert.AreEqual(FileMode.RegularFile, fRepositoryConfigTest.NewMode);
			Assert.AreEqual(1, fRepositoryConfigTest.Hunks.Count);
			{
				HunkHeader h = fRepositoryConfigTest.Hunks[0];
				Assert.AreEqual(fRepositoryConfigTest, h.File);
				Assert.AreEqual(921, h.StartOffset);
				Assert.AreEqual(109, h.OldImage.StartLine);
				Assert.AreEqual(4, h.OldImage.LineCount);
				Assert.AreEqual(109, h.NewStartLine);
				Assert.AreEqual(11, h.NewLineCount);

				Assert.AreEqual(4, h.LinesContext);
				Assert.AreEqual(7, h.OldImage.LinesAdded);
				Assert.AreEqual(0, h.OldImage.LinesDeleted);
				Assert.AreEqual(fRepositoryConfigTest.getOldId(), h.OldImage.Id);

				Assert.AreEqual(1490, h.EndOffset);
			}

			Assert.AreEqual("45c2f8a", fRepositoryConfig.getOldId().name());
			Assert.AreEqual("3291bba", fRepositoryConfig.getNewId().name());
			Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fRepositoryConfig
					.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, fRepositoryConfig.GetOldMode());
			Assert.AreEqual(FileMode.RegularFile, fRepositoryConfig.NewMode);
			Assert.AreEqual(3, fRepositoryConfig.Hunks.Count);
			{
				HunkHeader h = fRepositoryConfig.Hunks[0];
				Assert.AreEqual(fRepositoryConfig, h.File);
				Assert.AreEqual(1803, h.StartOffset);
				Assert.AreEqual(236, h.OldImage.StartLine);
				Assert.AreEqual(9, h.OldImage.LineCount);
				Assert.AreEqual(236, h.NewStartLine);
				Assert.AreEqual(9, h.NewLineCount);

				Assert.AreEqual(7, h.LinesContext);
				Assert.AreEqual(2, h.OldImage.LinesAdded);
				Assert.AreEqual(2, h.OldImage.LinesDeleted);
				Assert.AreEqual(fRepositoryConfig.getOldId(), h.OldImage.Id);

				Assert.AreEqual(2434, h.EndOffset);
			}
			{
				HunkHeader h = fRepositoryConfig.Hunks[1];
				Assert.AreEqual(2434, h.StartOffset);
				Assert.AreEqual(300, h.OldImage.StartLine);
				Assert.AreEqual(7, h.OldImage.LineCount);
				Assert.AreEqual(300, h.NewStartLine);
				Assert.AreEqual(7, h.NewLineCount);

				Assert.AreEqual(6, h.LinesContext);
				Assert.AreEqual(1, h.OldImage.LinesAdded);
				Assert.AreEqual(1, h.OldImage.LinesDeleted);

				Assert.AreEqual(2816, h.EndOffset);
			}
			{
				HunkHeader h = fRepositoryConfig.Hunks[2];
				Assert.AreEqual(2816, h.StartOffset);
				Assert.AreEqual(954, h.OldImage.StartLine);
				Assert.AreEqual(7, h.OldImage.LineCount);
				Assert.AreEqual(954, h.NewStartLine);
				Assert.AreEqual(7, h.NewLineCount);

				Assert.AreEqual(6, h.LinesContext);
				Assert.AreEqual(1, h.OldImage.LinesAdded);
				Assert.AreEqual(1, h.OldImage.LinesDeleted);

				Assert.AreEqual(3035, h.EndOffset);
			}
		}

		[Test]
		public void testParse_NoBinary()
		{
			GitSharp.Core.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_NoBinary.patch");
			Assert.AreEqual(5, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			for (int i = 0; i < 4; i++)
			{
				FileHeader fh = p.getFiles()[i];
				Assert.AreEqual(FileHeader.ChangeTypeEnum.ADD, fh.getChangeType());
				Assert.IsNotNull(fh.getOldId());
				Assert.IsNotNull(fh.getNewId());
				Assert.AreEqual("0000000", fh.getOldId().name());
				Assert.AreEqual(FileMode.Missing, fh.GetOldMode());
				Assert.AreEqual(FileMode.RegularFile, fh.NewMode);
				Assert.IsTrue(fh.NewName.StartsWith(
						"org.spearce.egit.ui/icons/toolbar/"));
				Assert.AreEqual(FileHeader.PatchTypeEnum.BINARY, fh.getPatchType());
				Assert.IsTrue(fh.Hunks.Count == 0);
				Assert.IsTrue(fh.hasMetaDataChanges());

				Assert.IsNull(fh.getForwardBinaryHunk());
				Assert.IsNull(fh.getReverseBinaryHunk());
			}

			{
				FileHeader fh = p.getFiles()[4];
				Assert.AreEqual("org.spearce.egit.ui/plugin.xml", fh.NewName);
				Assert.AreEqual(FileHeader.ChangeTypeEnum.MODIFY, fh.getChangeType());
				Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
				Assert.IsFalse(fh.hasMetaDataChanges());
				Assert.AreEqual("ee8a5a0", fh.getNewId().name());
				Assert.IsNull(fh.getForwardBinaryHunk());
				Assert.IsNull(fh.getReverseBinaryHunk());
				Assert.AreEqual(1, fh.Hunks.Count);
				Assert.AreEqual(272, fh.Hunks[0].OldImage.StartLine);
			}
		}

		[Test]
		public void testParse_GitBinaryLiteral()
		{
			GitSharp.Core.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_GitBinaryLiteral.patch");
			int[] binsizes = { 359, 393, 372, 404 };
			Assert.AreEqual(5, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			for (int i = 0; i < 4; i++)
			{
				FileHeader fh = p.getFiles()[i];
				Assert.AreEqual(FileHeader.ChangeTypeEnum.ADD, fh.getChangeType());
				Assert.IsNotNull(fh.getOldId());
				Assert.IsNotNull(fh.getNewId());
				Assert.AreEqual(ObjectId.ZeroId.Name, fh.getOldId().name());
				Assert.AreEqual(FileMode.RegularFile, fh.NewMode);
				Assert.IsTrue(fh.NewName.StartsWith(
						"org.spearce.egit.ui/icons/toolbar/"));
				Assert.AreEqual(FileHeader.PatchTypeEnum.GIT_BINARY, fh.getPatchType());
				Assert.IsTrue(fh.Hunks.Count == 0);
				Assert.IsTrue(fh.hasMetaDataChanges());

				BinaryHunk fwd = fh.getForwardBinaryHunk();
				BinaryHunk rev = fh.getReverseBinaryHunk();
				Assert.IsNotNull(fwd);
				Assert.IsNotNull(rev);
				Assert.AreEqual(binsizes[i], fwd.getSize());
				Assert.AreEqual(0, rev.getSize());

				Assert.AreEqual(fh, fwd.getFileHeader());
				Assert.AreEqual(fh, rev.getFileHeader());

				Assert.AreEqual(BinaryHunk.Type.LITERAL_DEFLATED, fwd.getType());
				Assert.AreEqual(BinaryHunk.Type.LITERAL_DEFLATED, rev.getType());
			}

			{
				FileHeader fh = p.getFiles()[4];
				Assert.AreEqual("org.spearce.egit.ui/plugin.xml", fh.NewName);
				Assert.AreEqual(FileHeader.ChangeTypeEnum.MODIFY, fh.getChangeType());
				Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
				Assert.IsFalse(fh.hasMetaDataChanges());
				Assert.AreEqual("ee8a5a0", fh.getNewId().name());
				Assert.IsNull(fh.getForwardBinaryHunk());
				Assert.IsNull(fh.getReverseBinaryHunk());
				Assert.AreEqual(1, fh.Hunks.Count);
				Assert.AreEqual(272, fh.Hunks[0].OldImage.StartLine);
			}
		}

		[Test]
		public void testParse_GitBinaryDelta()
		{
			GitSharp.Core.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_GitBinaryDelta.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			FileHeader fh = p.getFiles()[0];
			Assert.IsTrue(fh.NewName.StartsWith("zero.bin"));
			Assert.AreEqual(FileHeader.ChangeTypeEnum.MODIFY, fh.getChangeType());
			Assert.AreEqual(FileHeader.PatchTypeEnum.GIT_BINARY, fh.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, fh.NewMode);

			Assert.IsNotNull(fh.getOldId());
			Assert.IsNotNull(fh.getNewId());
			Assert.AreEqual("08e7df176454f3ee5eeda13efa0adaa54828dfd8", fh.getOldId()
					.name());
			Assert.AreEqual("d70d8710b6d32ff844af0ee7c247e4b4b051867f", fh.getNewId()
					.name());

			Assert.IsTrue(fh.Hunks.Count == 0);
			Assert.IsFalse(fh.hasMetaDataChanges());

			BinaryHunk fwd = fh.getForwardBinaryHunk();
			BinaryHunk rev = fh.getReverseBinaryHunk();
			Assert.IsNotNull(fwd);
			Assert.IsNotNull(rev);
			Assert.AreEqual(12, fwd.getSize());
			Assert.AreEqual(11, rev.getSize());

			Assert.AreEqual(fh, fwd.getFileHeader());
			Assert.AreEqual(fh, rev.getFileHeader());

			Assert.AreEqual(BinaryHunk.Type.DELTA_DEFLATED, fwd.getType());
			Assert.AreEqual(BinaryHunk.Type.DELTA_DEFLATED, rev.getType());

			Assert.AreEqual(496, fh.EndOffset);
		}

		[Test]
		public void testParse_FixNoNewline()
		{
			GitSharp.Core.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_FixNoNewline.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			FileHeader f = p.getFiles()[0];

			Assert.AreEqual("a", f.NewName);
			Assert.AreEqual(252, f.StartOffset);

			Assert.AreEqual("2e65efe", f.getOldId().name());
			Assert.AreEqual("f2ad6c7", f.getNewId().name());
			Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, f.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, f.GetOldMode());
			Assert.AreEqual(FileMode.RegularFile, f.NewMode);
			Assert.AreEqual(1, f.Hunks.Count);
			{
				HunkHeader h = f.Hunks[0];
				Assert.AreEqual(f, h.File);
				Assert.AreEqual(317, h.StartOffset);
				Assert.AreEqual(1, h.OldImage.StartLine);
				Assert.AreEqual(1, h.OldImage.LineCount);
				Assert.AreEqual(1, h.NewStartLine);
				Assert.AreEqual(1, h.NewLineCount);

				Assert.AreEqual(0, h.LinesContext);
				Assert.AreEqual(1, h.OldImage.LinesAdded);
				Assert.AreEqual(1, h.OldImage.LinesDeleted);
				Assert.AreEqual(f.getOldId(), h.OldImage.Id);

				Assert.AreEqual(363, h.EndOffset);
			}
		}

		[Test]
		public void testParse_AddNoNewline()
		{
			GitSharp.Core.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_AddNoNewline.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0, GetAllErrorsFromPatch(p));

			FileHeader f = p.getFiles()[0];

			Assert.AreEqual("a", f.NewName);
			Assert.AreEqual(256, f.StartOffset);

			Assert.AreEqual("f2ad6c7", f.getOldId().name());
			Assert.AreEqual("c59d9b6", f.getNewId().name());
			Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, f.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, f.GetOldMode());
			Assert.AreEqual(FileMode.RegularFile, f.NewMode);
			Assert.AreEqual(1, f.Hunks.Count);
			{
				HunkHeader h = f.Hunks[0];
				Assert.AreEqual(f, h.File);
				Assert.AreEqual(321, h.StartOffset);
				Assert.AreEqual(1, h.OldImage.StartLine);
				Assert.AreEqual(1, h.OldImage.LineCount);
				Assert.AreEqual(1, h.NewStartLine);
				Assert.AreEqual(1, h.NewLineCount);

				Assert.AreEqual(0, h.LinesContext);
				Assert.AreEqual(1, h.OldImage.LinesAdded);
				Assert.AreEqual(1, h.OldImage.LinesDeleted);
				Assert.AreEqual(f.getOldId(), h.OldImage.Id);

				Assert.AreEqual(367, h.EndOffset);
			}
		}
	}
}