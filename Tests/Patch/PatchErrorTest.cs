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
	public class PatchErrorTest : BasePatchTest
	{
		[Test]
		public void testError_DisconnectedHunk()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_DisconnectedHunk.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.AreEqual(1, p.getErrors().Count);

			FileHeader fh = p.getFiles()[0];
			Assert.AreEqual("org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java", fh.NewName);
			Assert.AreEqual(1, fh.Hunks.Count);

			Assert.AreEqual(1, p.getErrors().Count);
			FormatError e = p.getErrors()[0];
			Assert.AreEqual(FormatError.Severity.ERROR, e.getSeverity());
			Assert.AreEqual("Hunk disconnected from file", e.getMessage());
			Assert.AreEqual(18, e.getOffset());
			Assert.IsTrue(e.getLineText().StartsWith("@@ -109,4 +109,11 @@ assert"));
		}

		[Test]
		public void testError_TruncatedOld()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_TruncatedOld.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.AreEqual(1, p.getErrors().Count);

			FormatError e = p.getErrors()[0];
			Assert.AreEqual(FormatError.Severity.ERROR, e.getSeverity());
			Assert.AreEqual("Truncated hunk, at least 1 old lines is missing", e.getMessage());
			Assert.AreEqual(313, e.getOffset());
			Assert.IsTrue(e.getLineText().StartsWith("@@ -236,9 +236,9 @@ protected "));
		}

		[Test]
		public void testError_TruncatedNew()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_TruncatedNew.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.AreEqual(1, p.getErrors().Count);

			FormatError e = p.getErrors()[0];
			Assert.AreEqual(FormatError.Severity.ERROR, e.getSeverity());
			Assert.AreEqual("Truncated hunk, at least 1 new lines is missing", e.getMessage());
			Assert.AreEqual(313, e.getOffset());
			Assert.IsTrue(e.getLineText().StartsWith("@@ -236,9 +236,9 @@ protected "));
		}

		[Test]
		public void testError_BodyTooLong()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_BodyTooLong.patch");
			Assert.AreEqual(1, p.getFiles().Count);
			Assert.AreEqual(1, p.getErrors().Count);

			FormatError e = p.getErrors()[0];
			Assert.AreEqual(FormatError.Severity.WARNING, e.getSeverity());
			Assert.AreEqual("Hunk header 4:11 does not match body line count of 4:12", e.getMessage());
			Assert.AreEqual(349, e.getOffset());
			Assert.IsTrue(e.getLineText().StartsWith("@@ -109,4 +109,11 @@ assert"));
		}

		[Test]
		public void testError_GarbageBetweenFiles()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_GarbageBetweenFiles.patch");
			Assert.AreEqual(2, p.getFiles().Count);

			FileHeader fh0 = p.getFiles()[0];
			Assert.AreEqual("org.spearce.jgit.test/tst/org/spearce/jgit/lib/RepositoryConfigTest.java", fh0.NewName);
			Assert.AreEqual(1, fh0.Hunks.Count);

			FileHeader fh1 = p.getFiles()[1];
			Assert.AreEqual("org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java", fh1.NewName);
			Assert.AreEqual(1, fh1.Hunks.Count);

			Assert.AreEqual(1, p.getErrors().Count);
			FormatError e = p.getErrors()[0];
			Assert.AreEqual(FormatError.Severity.WARNING, e.getSeverity());
			Assert.AreEqual("Unexpected hunk trailer", e.getMessage());
			Assert.AreEqual(926, e.getOffset());
			Assert.AreEqual("I AM NOT HERE\n", e.getLineText());
		}

		[Test]
		public void testError_GitBinaryNoForwardHunk()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_GitBinaryNoForwardHunk.patch");
			Assert.AreEqual(2, p.getFiles().Count);

			FileHeader fh0 = p.getFiles()[0];
			Assert.AreEqual("org.spearce.egit.ui/icons/toolbar/fetchd.png", fh0.NewName);
            Assert.AreEqual(FileHeader.PatchTypeEnum.GIT_BINARY, fh0.getPatchType());
			Assert.IsTrue(fh0.Hunks.isEmpty());
			Assert.IsNull(fh0.getForwardBinaryHunk());

			FileHeader fh1 = p.getFiles()[1];
			Assert.AreEqual("org.spearce.egit.ui/icons/toolbar/fetche.png", fh1.NewName);
            Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fh1.getPatchType());
			Assert.IsTrue(fh1.Hunks.isEmpty());
			Assert.IsNull(fh1.getForwardBinaryHunk());

			Assert.AreEqual(1, p.getErrors().Count);
			FormatError e = p.getErrors()[0];
            Assert.AreEqual(FormatError.Severity.ERROR, e.getSeverity());
			Assert.AreEqual("Missing forward-image in GIT binary patch", e.getMessage());
			Assert.AreEqual(297, e.getOffset());
			Assert.AreEqual("\n", e.getLineText());
		}
	}
}
