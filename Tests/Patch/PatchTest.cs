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
using NUnit.Framework;

namespace GitSharp.Tests.Patch
{
	[TestFixture]
	public class PatchTest : BasePatchTest
	{
		[Test]
		public void testEmpty()
		{
			GitSharp.Patch.Patch p = new GitSharp.Patch.Patch();
			Assert.IsTrue(p.getFiles().Count == 0);
			Assert.IsTrue(p.getErrors().Count == 0);
		}

		[Test]
		public void testParse_ConfigCaseInsensitive()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_ConfigCaseInsensitive.patch");
			Assert.AreEqual(2, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			FileHeader fRepositoryConfigTest = p.getFiles()[0];
			FileHeader fRepositoryConfig = p.getFiles()[1];

			Assert.AreEqual(
					"org.spearce.jgit.test/tst/org/spearce/jgit/lib/RepositoryConfigTest.java",
					fRepositoryConfigTest.getNewName());

			Assert.AreEqual(
					"org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java",
					fRepositoryConfig.getNewName());

			Assert.AreEqual(572, fRepositoryConfigTest.startOffset);
			Assert.AreEqual(1490, fRepositoryConfig.startOffset);

			Assert.AreEqual("da7e704", fRepositoryConfigTest.getOldId().name());
			Assert.AreEqual("34ce04a", fRepositoryConfigTest.getNewId().name());
			Assert.AreEqual(FileHeader.PatchType.UNIFIED, fRepositoryConfigTest.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, fRepositoryConfigTest.getOldMode());
			Assert.AreEqual(FileMode.RegularFile, fRepositoryConfigTest.getNewMode());
			Assert.AreEqual(1, fRepositoryConfigTest.getHunks().Count);
			{
				HunkHeader h = fRepositoryConfigTest.getHunks()[0];
				Assert.AreEqual(fRepositoryConfigTest, h.getFileHeader());
				Assert.AreEqual(921, h.startOffset);
				Assert.AreEqual(109, h.getOldImage().getStartLine());
				Assert.AreEqual(4, h.getOldImage().getLineCount());
				Assert.AreEqual(109, h.getNewStartLine());
				Assert.AreEqual(11, h.getNewLineCount());

				Assert.AreEqual(4, h.getLinesContext());
				Assert.AreEqual(7, h.getOldImage().getLinesAdded());
				Assert.AreEqual(0, h.getOldImage().getLinesDeleted());
				Assert.AreEqual(fRepositoryConfigTest.getOldId(), h.getOldImage().getId());

				Assert.AreEqual(1490, h.endOffset);
			}

			Assert.AreEqual("45c2f8a", fRepositoryConfig.getOldId().name());
			Assert.AreEqual("3291bba", fRepositoryConfig.getNewId().name());
			Assert.AreEqual(FileHeader.PatchType.UNIFIED, fRepositoryConfig
					.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, fRepositoryConfig.getOldMode());
			Assert.AreEqual(FileMode.RegularFile, fRepositoryConfig.getNewMode());
			Assert.AreEqual(3, fRepositoryConfig.getHunks().Count);
			{
				HunkHeader h = fRepositoryConfig.getHunks()[0];
				Assert.AreEqual(fRepositoryConfig, h.getFileHeader());
				Assert.AreEqual(1803, h.startOffset);
				Assert.AreEqual(236, h.getOldImage().getStartLine());
				Assert.AreEqual(9, h.getOldImage().getLineCount());
				Assert.AreEqual(236, h.getNewStartLine());
				Assert.AreEqual(9, h.getNewLineCount());

				Assert.AreEqual(7, h.getLinesContext());
				Assert.AreEqual(2, h.getOldImage().getLinesAdded());
				Assert.AreEqual(2, h.getOldImage().getLinesDeleted());
				Assert.AreEqual(fRepositoryConfig.getOldId(), h.getOldImage().getId());

				Assert.AreEqual(2434, h.endOffset);
			}
			{
				HunkHeader h = fRepositoryConfig.getHunks()[1];
				Assert.AreEqual(2434, h.startOffset);
				Assert.AreEqual(300, h.getOldImage().getStartLine());
				Assert.AreEqual(7, h.getOldImage().getLineCount());
				Assert.AreEqual(300, h.getNewStartLine());
				Assert.AreEqual(7, h.getNewLineCount());

				Assert.AreEqual(6, h.getLinesContext());
				Assert.AreEqual(1, h.getOldImage().getLinesAdded());
				Assert.AreEqual(1, h.getOldImage().getLinesDeleted());

				Assert.AreEqual(2816, h.endOffset);
			}
			{
				HunkHeader h = fRepositoryConfig.getHunks()[2];
				Assert.AreEqual(2816, h.startOffset);
				Assert.AreEqual(954, h.getOldImage().getStartLine());
				Assert.AreEqual(7, h.getOldImage().getLineCount());
				Assert.AreEqual(954, h.getNewStartLine());
				Assert.AreEqual(7, h.getNewLineCount());

				Assert.AreEqual(6, h.getLinesContext());
				Assert.AreEqual(1, h.getOldImage().getLinesAdded());
				Assert.AreEqual(1, h.getOldImage().getLinesDeleted());

				Assert.AreEqual(3035, h.endOffset);
			}
		}

		[Test]
		public void testParse_NoBinary()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_NoBinary.patch");
			Assert.AreEqual(5, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			for (int i = 0; i < 4; i++)
			{
				FileHeader fh = p.getFiles()[i];
				Assert.AreEqual(FileHeader.ChangeType.ADD, fh.getChangeType());
				Assert.IsNotNull(fh.getOldId());
				Assert.IsNotNull(fh.getNewId());
				Assert.AreEqual("0000000", fh.getOldId().name());
				Assert.AreEqual(FileMode.Missing, fh.getOldMode());
				Assert.AreEqual(FileMode.RegularFile, fh.getNewMode());
				Assert.IsTrue(fh.getNewName().StartsWith(
						"org.spearce.egit.ui/icons/toolbar/"));
				Assert.AreEqual(FileHeader.PatchType.BINARY, fh.getPatchType());
				Assert.IsTrue(fh.getHunks().Count == 0);
				Assert.IsTrue(fh.hasMetaDataChanges());

				Assert.IsNull(fh.getForwardBinaryHunk());
				Assert.IsNull(fh.getReverseBinaryHunk());
			}

			{
				FileHeader fh = p.getFiles()[4];
				Assert.AreEqual("org.spearce.egit.ui/plugin.xml", fh.getNewName());
				Assert.AreEqual(FileHeader.ChangeType.MODIFY, fh.getChangeType());
				Assert.AreEqual(FileHeader.PatchType.UNIFIED, fh.getPatchType());
				Assert.IsFalse(fh.hasMetaDataChanges());
				Assert.AreEqual("ee8a5a0", fh.getNewId().name());
				Assert.IsNull(fh.getForwardBinaryHunk());
				Assert.IsNull(fh.getReverseBinaryHunk());
				Assert.AreEqual(1, fh.getHunks().Count);
				Assert.AreEqual(272, fh.getHunks()[0].getOldImage().getStartLine());
			}
		}

		[Test]
		public void testParse_GitBinaryLiteral()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_GitBinaryLiteral.patch");
			int[] binsizes = { 359, 393, 372, 404 };
			Assert.AreEqual(5, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			for (int i = 0; i < 4; i++)
			{
				FileHeader fh = p.getFiles()[i];
				Assert.AreEqual(FileHeader.ChangeType.ADD, fh.getChangeType());
				Assert.IsNotNull(fh.getOldId());
				Assert.IsNotNull(fh.getNewId());
				Assert.AreEqual(ObjectId.ZeroId.Name, fh.getOldId().name());
				Assert.AreEqual(FileMode.RegularFile, fh.getNewMode());
				Assert.IsTrue(fh.getNewName().StartsWith(
						"org.spearce.egit.ui/icons/toolbar/"));
				Assert.AreEqual(FileHeader.PatchType.GIT_BINARY, fh.getPatchType());
				Assert.IsTrue(fh.getHunks().Count == 0);
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
				Assert.AreEqual("org.spearce.egit.ui/plugin.xml", fh.getNewName());
				Assert.AreEqual(FileHeader.ChangeType.MODIFY, fh.getChangeType());
				Assert.AreEqual(FileHeader.PatchType.UNIFIED, fh.getPatchType());
				Assert.IsFalse(fh.hasMetaDataChanges());
				Assert.AreEqual("ee8a5a0", fh.getNewId().name());
				Assert.IsNull(fh.getForwardBinaryHunk());
				Assert.IsNull(fh.getReverseBinaryHunk());
				Assert.AreEqual(1, fh.getHunks().Count);
				Assert.AreEqual(272, fh.getHunks()[0].getOldImage().getStartLine());
			}
		}

		[Test]
		public void testParse_GitBinaryDelta()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_GitBinaryDelta.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			FileHeader fh = p.getFiles()[0];
			Assert.IsTrue(fh.getNewName().StartsWith("zero.bin"));
			Assert.AreEqual(FileHeader.ChangeType.MODIFY, fh.getChangeType());
			Assert.AreEqual(FileHeader.PatchType.GIT_BINARY, fh.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, fh.getNewMode());

			Assert.IsNotNull(fh.getOldId());
			Assert.IsNotNull(fh.getNewId());
			Assert.AreEqual("08e7df176454f3ee5eeda13efa0adaa54828dfd8", fh.getOldId()
					.name());
			Assert.AreEqual("d70d8710b6d32ff844af0ee7c247e4b4b051867f", fh.getNewId()
					.name());

			Assert.IsTrue(fh.getHunks().Count == 0);
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

			Assert.AreEqual(496, fh.endOffset);
		}

		[Test]
		public void testParse_FixNoNewline()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_FixNoNewline.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0);

			FileHeader f = p.getFiles()[0];

			Assert.AreEqual("a", f.getNewName());
			Assert.AreEqual(252, f.startOffset);

			Assert.AreEqual("2e65efe", f.getOldId().name());
			Assert.AreEqual("f2ad6c7", f.getNewId().name());
			Assert.AreEqual(FileHeader.PatchType.UNIFIED, f.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, f.getOldMode());
			Assert.AreEqual(FileMode.RegularFile, f.getNewMode());
			Assert.AreEqual(1, f.getHunks().Count);
			{
				HunkHeader h = f.getHunks()[0];
				Assert.AreEqual(f, h.getFileHeader());
				Assert.AreEqual(317, h.startOffset);
				Assert.AreEqual(1, h.getOldImage().getStartLine());
				Assert.AreEqual(1, h.getOldImage().getLineCount());
				Assert.AreEqual(1, h.getNewStartLine());
				Assert.AreEqual(1, h.getNewLineCount());

				Assert.AreEqual(0, h.getLinesContext());
				Assert.AreEqual(1, h.getOldImage().getLinesAdded());
				Assert.AreEqual(1, h.getOldImage().getLinesDeleted());
				Assert.AreEqual(f.getOldId(), h.getOldImage().getId());

				Assert.AreEqual(363, h.endOffset);
			}
		}

		[Test]
		public void testParse_AddNoNewline()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testParse_AddNoNewline.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.IsTrue(p.getErrors().Count == 0, GetAllErrorsFromPatch(p));

			FileHeader f = p.getFiles()[0];

			Assert.AreEqual("a", f.getNewName());
			Assert.AreEqual(256, f.startOffset);

			Assert.AreEqual("f2ad6c7", f.getOldId().name());
			Assert.AreEqual("c59d9b6", f.getNewId().name());
			Assert.AreEqual(FileHeader.PatchType.UNIFIED, f.getPatchType());
			Assert.AreEqual(FileMode.RegularFile, f.getOldMode());
			Assert.AreEqual(FileMode.RegularFile, f.getNewMode());
			Assert.AreEqual(1, f.getHunks().Count);
			{
				HunkHeader h = f.getHunks()[0];
				Assert.AreEqual(f, h.getFileHeader());
				Assert.AreEqual(321, h.startOffset);
				Assert.AreEqual(1, h.getOldImage().getStartLine());
				Assert.AreEqual(1, h.getOldImage().getLineCount());
				Assert.AreEqual(1, h.getNewStartLine());
				Assert.AreEqual(1, h.getNewLineCount());

				Assert.AreEqual(0, h.getLinesContext());
				Assert.AreEqual(1, h.getOldImage().getLinesAdded());
				Assert.AreEqual(1, h.getOldImage().getLinesDeleted());
				Assert.AreEqual(f.getOldId(), h.getOldImage().getId());

				Assert.AreEqual(367, h.endOffset);
			}
		}
	}
}