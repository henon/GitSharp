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
using Xunit;

namespace GitSharp.Tests.Patch
{
	public class PatchTest : BasePatchTest
	{
		[Fact]
		public void testEmpty()
		{
			var patch = new GitSharp.Patch.Patch();
			Assert.True(patch.getFiles().Count == 0);
			Assert.True(patch.getErrors().Count == 0);
		}

		[Fact]
		public void testParse_ConfigCaseInsensitive()
		{
			GitSharp.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_ConfigCaseInsensitive.patch");
			Assert.Equal(2, p.getFiles().Count);
			Assert.True(p.getErrors().Count == 0);

			FileHeader fRepositoryConfigTest = p.getFiles()[0];
			FileHeader fRepositoryConfig = p.getFiles()[1];

			Assert.Equal(
					"org.spearce.jgit.test/tst/org/spearce/jgit/lib/RepositoryConfigTest.java",
					fRepositoryConfigTest.NewName);

			Assert.Equal(
					"org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java",
					fRepositoryConfig.NewName);

			Assert.Equal(572, fRepositoryConfigTest.StartOffset);
			Assert.Equal(1490, fRepositoryConfig.StartOffset);

			Assert.Equal("da7e704", fRepositoryConfigTest.getOldId().name());
			Assert.Equal("34ce04a", fRepositoryConfigTest.getNewId().name());
			Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fRepositoryConfigTest.getPatchType());
			Assert.Equal(FileMode.RegularFile, fRepositoryConfigTest.GetOldMode());
			Assert.Equal(FileMode.RegularFile, fRepositoryConfigTest.NewMode);
			Assert.Equal(1, fRepositoryConfigTest.Hunks.Count);

			HunkHeader h = fRepositoryConfigTest.Hunks[0];
			Assert.Equal(fRepositoryConfigTest, h.File);
			Assert.Equal(921, h.StartOffset);
			Assert.Equal(109, h.OldImage.StartLine);
			Assert.Equal(4, h.OldImage.LineCount);
			Assert.Equal(109, h.NewStartLine);
			Assert.Equal(11, h.NewLineCount);

			Assert.Equal(4, h.LinesContext);
			Assert.Equal(7, h.OldImage.LinesAdded);
			Assert.Equal(0, h.OldImage.LinesDeleted);
			Assert.Equal(fRepositoryConfigTest.getOldId(), h.OldImage.Id);

			Assert.Equal(1490, h.EndOffset);

			Assert.Equal("45c2f8a", fRepositoryConfig.getOldId().name());
			Assert.Equal("3291bba", fRepositoryConfig.getNewId().name());
			Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fRepositoryConfig
					.getPatchType());
			Assert.Equal(FileMode.RegularFile, fRepositoryConfig.GetOldMode());
			Assert.Equal(FileMode.RegularFile, fRepositoryConfig.NewMode);
			Assert.Equal(3, fRepositoryConfig.Hunks.Count);

			h = fRepositoryConfig.Hunks[0];
			Assert.Equal(fRepositoryConfig, h.File);
			Assert.Equal(1803, h.StartOffset);
			Assert.Equal(236, h.OldImage.StartLine);
			Assert.Equal(9, h.OldImage.LineCount);
			Assert.Equal(236, h.NewStartLine);
			Assert.Equal(9, h.NewLineCount);

			Assert.Equal(7, h.LinesContext);
			Assert.Equal(2, h.OldImage.LinesAdded);
			Assert.Equal(2, h.OldImage.LinesDeleted);
			Assert.Equal(fRepositoryConfig.getOldId(), h.OldImage.Id);

			Assert.Equal(2434, h.EndOffset);

			h = fRepositoryConfig.Hunks[1];
			Assert.Equal(2434, h.StartOffset);
			Assert.Equal(300, h.OldImage.StartLine);
			Assert.Equal(7, h.OldImage.LineCount);
			Assert.Equal(300, h.NewStartLine);
			Assert.Equal(7, h.NewLineCount);

			Assert.Equal(6, h.LinesContext);
			Assert.Equal(1, h.OldImage.LinesAdded);
			Assert.Equal(1, h.OldImage.LinesDeleted);

			Assert.Equal(2816, h.EndOffset);

			h = fRepositoryConfig.Hunks[2];
			Assert.Equal(2816, h.StartOffset);
			Assert.Equal(954, h.OldImage.StartLine);
			Assert.Equal(7, h.OldImage.LineCount);
			Assert.Equal(954, h.NewStartLine);
			Assert.Equal(7, h.NewLineCount);

			Assert.Equal(6, h.LinesContext);
			Assert.Equal(1, h.OldImage.LinesAdded);
			Assert.Equal(1, h.OldImage.LinesDeleted);

			Assert.Equal(3035, h.EndOffset);
		}

		[Fact]
		public void testParse_NoBinary()
		{
			GitSharp.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_NoBinary.patch");
			Assert.Equal(5, p.getFiles().Count);
			Assert.True(p.getErrors().Count == 0);

			for (int i = 0; i < 4; i++)
			{
				FileHeader fh = p.getFiles()[i];
				Assert.Equal(FileHeader.ChangeTypeEnum.ADD, fh.getChangeType());
				Assert.NotNull(fh.getOldId());
				Assert.NotNull(fh.getNewId());
				Assert.Equal("0000000", fh.getOldId().name());
				Assert.Equal(FileMode.Missing, fh.GetOldMode());
				Assert.Equal(FileMode.RegularFile, fh.NewMode);
				Assert.True(fh.NewName.StartsWith(
						"org.spearce.egit.ui/icons/toolbar/"));
				Assert.Equal(FileHeader.PatchTypeEnum.BINARY, fh.getPatchType());
				Assert.True(fh.Hunks.Count == 0);
				Assert.True(fh.hasMetaDataChanges());

				Assert.Null(fh.getForwardBinaryHunk());
				Assert.Null(fh.getReverseBinaryHunk());
			}

			FileHeader fh2 = p.getFiles()[4];
			Assert.Equal("org.spearce.egit.ui/plugin.xml", fh2.NewName);
			Assert.Equal(FileHeader.ChangeTypeEnum.MODIFY, fh2.getChangeType());
			Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fh2.getPatchType());
			Assert.False(fh2.hasMetaDataChanges());
			Assert.Equal("ee8a5a0", fh2.getNewId().name());
			Assert.Null(fh2.getForwardBinaryHunk());
			Assert.Null(fh2.getReverseBinaryHunk());
			Assert.Equal(1, fh2.Hunks.Count);
			Assert.Equal(272, fh2.Hunks[0].OldImage.StartLine);
		}

		[Fact]
		public void testParse_GitBinaryLiteral()
		{
			GitSharp.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_GitBinaryLiteral.patch");
			int[] binsizes = { 359, 393, 372, 404 };
			Assert.Equal(5, p.getFiles().Count);
			Assert.True(p.getErrors().Count == 0);

			for (int i = 0; i < 4; i++)
			{
				FileHeader fh = p.getFiles()[i];
				Assert.Equal(FileHeader.ChangeTypeEnum.ADD, fh.getChangeType());
				Assert.NotNull(fh.getOldId());
				Assert.NotNull(fh.getNewId());
				Assert.Equal(ObjectId.ZeroId.Name, fh.getOldId().name());
				Assert.Equal(FileMode.RegularFile, fh.NewMode);
				Assert.True(fh.NewName.StartsWith(
						"org.spearce.egit.ui/icons/toolbar/"));
				Assert.Equal(FileHeader.PatchTypeEnum.GIT_BINARY, fh.getPatchType());
				Assert.True(fh.Hunks.Count == 0);
				Assert.True(fh.hasMetaDataChanges());

				BinaryHunk fwd = fh.getForwardBinaryHunk();
				BinaryHunk rev = fh.getReverseBinaryHunk();
				Assert.NotNull(fwd);
				Assert.NotNull(rev);
				Assert.Equal(binsizes[i], fwd.getSize());
				Assert.Equal(0, rev.getSize());

				Assert.Equal(fh, fwd.getFileHeader());
				Assert.Equal(fh, rev.getFileHeader());

				Assert.Equal(BinaryHunk.Type.LITERAL_DEFLATED, fwd.getType());
				Assert.Equal(BinaryHunk.Type.LITERAL_DEFLATED, rev.getType());
			}

			FileHeader fh2 = p.getFiles()[4];
			Assert.Equal("org.spearce.egit.ui/plugin.xml", fh2.NewName);
			Assert.Equal(FileHeader.ChangeTypeEnum.MODIFY, fh2.getChangeType());
			Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fh2.getPatchType());
			Assert.False(fh2.hasMetaDataChanges());
			Assert.Equal("ee8a5a0", fh2.getNewId().name());
			Assert.Null(fh2.getForwardBinaryHunk());
			Assert.Null(fh2.getReverseBinaryHunk());
			Assert.Equal(1, fh2.Hunks.Count);
			Assert.Equal(272, fh2.Hunks[0].OldImage.StartLine);
		}

		[Fact]
		public void testParse_GitBinaryDelta()
		{
			GitSharp.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_GitBinaryDelta.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.True(p.getErrors().Count == 0);

			FileHeader fh = p.getFiles()[0];
			Assert.True(fh.NewName.StartsWith("zero.bin"));
			Assert.Equal(FileHeader.ChangeTypeEnum.MODIFY, fh.getChangeType());
			Assert.Equal(FileHeader.PatchTypeEnum.GIT_BINARY, fh.getPatchType());
			Assert.Equal(FileMode.RegularFile, fh.NewMode);

			Assert.NotNull(fh.getOldId());
			Assert.NotNull(fh.getNewId());
			Assert.Equal("08e7df176454f3ee5eeda13efa0adaa54828dfd8", fh.getOldId()
					.name());
			Assert.Equal("d70d8710b6d32ff844af0ee7c247e4b4b051867f", fh.getNewId()
					.name());

			Assert.True(fh.Hunks.Count == 0);
			Assert.False(fh.hasMetaDataChanges());

			BinaryHunk fwd = fh.getForwardBinaryHunk();
			BinaryHunk rev = fh.getReverseBinaryHunk();
			Assert.NotNull(fwd);
			Assert.NotNull(rev);
			Assert.Equal(12, fwd.getSize());
			Assert.Equal(11, rev.getSize());

			Assert.Equal(fh, fwd.getFileHeader());
			Assert.Equal(fh, rev.getFileHeader());

			Assert.Equal(BinaryHunk.Type.DELTA_DEFLATED, fwd.getType());
			Assert.Equal(BinaryHunk.Type.DELTA_DEFLATED, rev.getType());

			Assert.Equal(496, fh.EndOffset);
		}

		[Fact]
		public void testParse_FixNoNewline()
		{
			GitSharp.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_FixNoNewline.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.True(p.getErrors().Count == 0);

			FileHeader f = p.getFiles()[0];

			Assert.Equal("a", f.NewName);
			Assert.Equal(252, f.StartOffset);

			Assert.Equal("2e65efe", f.getOldId().name());
			Assert.Equal("f2ad6c7", f.getNewId().name());
			Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, f.getPatchType());
			Assert.Equal(FileMode.RegularFile, f.GetOldMode());
			Assert.Equal(FileMode.RegularFile, f.NewMode);
			Assert.Equal(1, f.Hunks.Count);

			HunkHeader h = f.Hunks[0];
			Assert.Equal(f, h.File);
			Assert.Equal(317, h.StartOffset);
			Assert.Equal(1, h.OldImage.StartLine);
			Assert.Equal(1, h.OldImage.LineCount);
			Assert.Equal(1, h.NewStartLine);
			Assert.Equal(1, h.NewLineCount);

			Assert.Equal(0, h.LinesContext);
			Assert.Equal(1, h.OldImage.LinesAdded);
			Assert.Equal(1, h.OldImage.LinesDeleted);
			Assert.Equal(f.getOldId(), h.OldImage.Id);

			Assert.Equal(363, h.EndOffset);
		}

		[Fact]
		public void testParse_AddNoNewline()
		{
			GitSharp.Patch.Patch p = ParseTestPatchFile(PatchsDir + "testParse_AddNoNewline.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.True(p.getErrors().Count == 0, GetAllErrorsFromPatch(p));

			FileHeader f = p.getFiles()[0];

			Assert.Equal("a", f.NewName);
			Assert.Equal(256, f.StartOffset);

			Assert.Equal("f2ad6c7", f.getOldId().name());
			Assert.Equal("c59d9b6", f.getNewId().name());
			Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, f.getPatchType());
			Assert.Equal(FileMode.RegularFile, f.GetOldMode());
			Assert.Equal(FileMode.RegularFile, f.NewMode);
			Assert.Equal(1, f.Hunks.Count);

			HunkHeader h = f.Hunks[0];
			Assert.Equal(f, h.File);
			Assert.Equal(321, h.StartOffset);
			Assert.Equal(1, h.OldImage.StartLine);
			Assert.Equal(1, h.OldImage.LineCount);
			Assert.Equal(1, h.NewStartLine);
			Assert.Equal(1, h.NewLineCount);

			Assert.Equal(0, h.LinesContext);
			Assert.Equal(1, h.OldImage.LinesAdded);
			Assert.Equal(1, h.OldImage.LinesDeleted);
			Assert.Equal(f.getOldId(), h.OldImage.Id);

			Assert.Equal(367, h.EndOffset);
		}
	}
}