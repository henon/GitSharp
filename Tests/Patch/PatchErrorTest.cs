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
	public class PatchErrorTest : BasePatchTest
	{
		[Fact]
		public void testError_DisconnectedHunk()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_DisconnectedHunk.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.Equal(1, p.getErrors().Count);

			FileHeader fh = p.getFiles()[0];
			Assert.Equal("org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java", fh.getNewName());
			Assert.Equal(1, fh.getHunks().Count);

			Assert.Equal(1, p.getErrors().Count);
			FormatError e = p.getErrors()[0];
			Assert.Equal(FormatError.Severity.ERROR, e.getSeverity());
			Assert.Equal("Hunk disconnected from file", e.getMessage());
			Assert.Equal(18, e.getOffset());
			Assert.True(e.getLineText().StartsWith("@@ -109,4 +109,11 @@ assert"));
		}

		[Fact]
		public void testError_TruncatedOld()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_TruncatedOld.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.Equal(1, p.getErrors().Count);

			FormatError e = p.getErrors()[0];
			Assert.Equal(FormatError.Severity.ERROR, e.getSeverity());
			Assert.Equal("Truncated hunk, at least 1 old lines is missing", e.getMessage());
			Assert.Equal(313, e.getOffset());
			Assert.True(e.getLineText().StartsWith("@@ -236,9 +236,9 @@ protected "));
		}

		[Fact]
		public void testError_TruncatedNew()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_TruncatedNew.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.Equal(1, p.getErrors().Count);

			FormatError e = p.getErrors()[0];
			Assert.Equal(FormatError.Severity.ERROR, e.getSeverity());
			Assert.Equal("Truncated hunk, at least 1 new lines is missing", e.getMessage());
			Assert.Equal(313, e.getOffset());
			Assert.True(e.getLineText().StartsWith("@@ -236,9 +236,9 @@ protected "));
		}

		[Fact]
		public void testError_BodyTooLong()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_BodyTooLong.patch");
			Assert.Equal(1, p.getFiles().Count);
			Assert.Equal(1, p.getErrors().Count);

			FormatError e = p.getErrors()[0];
			Assert.Equal(FormatError.Severity.WARNING, e.getSeverity());
			Assert.Equal("Hunk header 4:11 does not match body line count of 4:12", e.getMessage());
			Assert.Equal(349, e.getOffset());
			Assert.True(e.getLineText().StartsWith("@@ -109,4 +109,11 @@ assert"));
		}

		[Fact]
		public void testError_GarbageBetweenFiles()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_GarbageBetweenFiles.patch");
			Assert.Equal(2, p.getFiles().Count);

			FileHeader fh0 = p.getFiles()[0];
			Assert.Equal("org.spearce.jgit.test/tst/org/spearce/jgit/lib/RepositoryConfigTest.java", fh0.getNewName());
			Assert.Equal(1, fh0.getHunks().Count);

			FileHeader fh1 = p.getFiles()[1];
			Assert.Equal("org.spearce.jgit/src/org/spearce/jgit/lib/RepositoryConfig.java", fh1.getNewName());
			Assert.Equal(1, fh1.getHunks().Count);

			Assert.Equal(1, p.getErrors().Count);
			FormatError e = p.getErrors()[0];
			Assert.Equal(FormatError.Severity.WARNING, e.getSeverity());
			Assert.Equal("Unexpected hunk trailer", e.getMessage());
			Assert.Equal(926, e.getOffset());
			Assert.Equal("I AM NOT HERE\n", e.getLineText());
		}

		[Fact]
		public void testError_GitBinaryNoForwardHunk()
		{
			GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testError_GitBinaryNoForwardHunk.patch");
			Assert.Equal(2, p.getFiles().Count);

			FileHeader fh0 = p.getFiles()[0];
			Assert.Equal("org.spearce.egit.ui/icons/toolbar/fetchd.png", fh0.getNewName());
            Assert.Equal(FileHeader.PatchType.GIT_BINARY, fh0.getPatchType());
			Assert.True(fh0.getHunks().isEmpty());
			Assert.Null(fh0.getForwardBinaryHunk());

			FileHeader fh1 = p.getFiles()[1];
			Assert.Equal("org.spearce.egit.ui/icons/toolbar/fetche.png", fh1.getNewName());
            Assert.Equal(FileHeader.PatchType.UNIFIED, fh1.getPatchType());
			Assert.True(fh1.getHunks().isEmpty());
			Assert.Null(fh1.getForwardBinaryHunk());

			Assert.Equal(1, p.getErrors().Count);
			FormatError e = p.getErrors()[0];
            Assert.Equal(FormatError.Severity.ERROR, e.getSeverity());
			Assert.Equal("Missing forward-image in GIT binary patch", e.getMessage());
			Assert.Equal(297, e.getOffset());
			Assert.Equal("\n", e.getLineText());
		}
	}
}
