/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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

namespace GitSharp.Tests.Patch
{
    [TestFixture]
    public class FileHeaderTest : BasePatchTest
    {
        [Test]
	    public void testParseGitFileName_Empty()
        {
		    FileHeader fh = Data(string.Empty);
			Assert.AreEqual(-1, fh.parseGitFileName(0, fh.Buffer.Length));
		    Assert.IsNotNull(fh.Hunks);
		    Assert.IsTrue(fh.Hunks.Count == 0);
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_NoLF()
        {
		    FileHeader fh = Data("a/ b/");
			Assert.AreEqual(-1, fh.parseGitFileName(0, fh.Buffer.Length));
	    }

        [Test]
	    public void testParseGitFileName_NoSecondLine()
        {
		    FileHeader fh = Data("\n");
			Assert.AreEqual(-1, fh.parseGitFileName(0, fh.Buffer.Length));
	    }

        [Test]
	    public void testParseGitFileName_EmptyHeader()
        {
		    FileHeader fh = Data("\n\n");
			Assert.AreEqual(1, fh.parseGitFileName(0, fh.Buffer.Length));
	    }

        [Test]
	    public void testParseGitFileName_Foo()
        {
		    const string name = "foo";
		    FileHeader fh = Header(name);
			Assert.AreEqual(GitLine(name).Length, fh.parseGitFileName(0, fh.Buffer.Length));
		    Assert.AreEqual(name, fh.OldName);
		    Assert.AreSame(fh.OldName, fh.NewName);
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_FailFooBar()
        {
		    FileHeader fh = Data("a/foo b/bar\n-");
			Assert.IsTrue(fh.parseGitFileName(0, fh.Buffer.Length) > 0);
		    Assert.IsNull(fh.OldName);
		    Assert.IsNull(fh.NewName);
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_FooSpBar()
        {
		    const string name = "foo bar";
		    FileHeader fh = Header(name);
		    Assert.AreEqual(GitLine(name).Length, fh.parseGitFileName(0,
					fh.Buffer.Length));
		    Assert.AreEqual(name, fh.OldName);
		    Assert.AreSame(fh.OldName, fh.NewName);
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_DqFooTabBar()
        {
		    const string name = "foo\tbar";
		    const string dqName = "foo\\tbar";
		    FileHeader fh = DqHeader(dqName);
		    Assert.AreEqual(DqGitLine(dqName).Length, fh.parseGitFileName(0,
					fh.Buffer.Length));
		    Assert.AreEqual(name, fh.OldName);
		    Assert.AreSame(fh.OldName, fh.NewName);
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_DqFooSpLfNulBar()
        {
		    const string name = "foo \n\0bar";
		    const string dqName = "foo \\n\\0bar";
		    FileHeader fh = DqHeader(dqName);
		    Assert.AreEqual(DqGitLine(dqName).Length, fh.parseGitFileName(0,
					fh.Buffer.Length));
		    Assert.AreEqual(name, fh.OldName);
		    Assert.AreSame(fh.OldName, fh.NewName);
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_SrcFooC()
        {
		    const string name = "src/foo/bar/argh/code.c";
		    FileHeader fh = Header(name);
		    Assert.AreEqual(GitLine(name).Length, fh.parseGitFileName(0,
					fh.Buffer.Length));
		    Assert.AreEqual(name, fh.OldName);
		    Assert.AreSame(fh.OldName, fh.NewName);
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_SrcFooCNonStandardPrefix()
        {
		    const string name = "src/foo/bar/argh/code.c";
		    const string header = "project-v-1.0/" + name + " mydev/" + name + "\n";
		    FileHeader fh = Data(header + "-");
			Assert.AreEqual(header.Length, fh.parseGitFileName(0, fh.Buffer.Length));
		    Assert.AreEqual(name, fh.OldName);
		    Assert.AreSame(fh.OldName, fh.NewName);
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseUnicodeName_NewFile()
        {
		    FileHeader fh = Data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "new file mode 100644\n"
				    + "index 0000000..7898192\n"
				    + "--- /dev/null\n"
				    + "+++ \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "@@ -0,0 +1 @@\n" + "+a\n");
		    AssertParse(fh);

		    Assert.AreEqual("/dev/null", fh.OldName);
		    Assert.AreSame(FileHeader.DEV_NULL, fh.OldName);
		    Assert.AreEqual("\u00c5ngstr\u00f6m", fh.NewName);

            Assert.AreEqual(FileHeader.ChangeTypeEnum.ADD, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.AreSame(FileMode.Missing, fh.GetOldMode());
		    Assert.AreSame(FileMode.RegularFile, fh.NewMode);

		    Assert.AreEqual("0000000", fh.getOldId().name());
		    Assert.AreEqual("7898192", fh.getNewId().name());
		    Assert.AreEqual(0, fh.getScore());
	    }

        [Test]
	    public void testParseUnicodeName_DeleteFile()
        {
		    FileHeader fh = Data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "deleted file mode 100644\n"
				    + "index 7898192..0000000\n"
				    + "--- \"a/\\303\\205ngstr\\303\\266m\"\n"
				    + "+++ /dev/null\n"
				    + "@@ -1 +0,0 @@\n" + "-a\n");

		    AssertParse(fh);

		    Assert.AreEqual("\u00c5ngstr\u00f6m", fh.OldName);
		    Assert.AreEqual("/dev/null", fh.NewName);
		    Assert.AreSame(FileHeader.DEV_NULL, fh.NewName);

            Assert.AreEqual(FileHeader.ChangeTypeEnum.DELETE, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.AreSame(FileMode.RegularFile, fh.GetOldMode());
		    Assert.AreSame(FileMode.Missing, fh.NewMode);

		    Assert.AreEqual("7898192", fh.getOldId().name());
		    Assert.AreEqual("0000000", fh.getNewId().name());
		    Assert.AreEqual(0, fh.getScore());
	    }

        [Test]
	    public void testParseModeChange()
        {
		    FileHeader fh = Data("diff --git a/a b b/a b\n"
				    + "old mode 100644\n" + "new mode 100755\n");

		    AssertParse(fh);
		    Assert.AreEqual("a b", fh.OldName);
		    Assert.AreEqual("a b", fh.NewName);

		    Assert.AreEqual(FileHeader.ChangeTypeEnum.MODIFY, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.getOldId());
		    Assert.IsNull(fh.getNewId());

		    Assert.AreSame(FileMode.RegularFile, fh.GetOldMode());
		    Assert.AreSame(FileMode.ExecutableFile, fh.NewMode);
		    Assert.AreEqual(0, fh.getScore());
	    }

        [Test]
	    public void testParseRename100_NewStyle()
        {
		    FileHeader fh = Data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "rename from a\n"
				    + "rename to \" c/\\303\\205ngstr\\303\\266m\"\n");

			int ptr = fh.parseGitFileName(0, fh.Buffer.Length);
		    Assert.IsTrue(ptr > 0);
		    Assert.IsNull(fh.OldName); // can't parse names on a rename
		    Assert.IsNull(fh.NewName);

			ptr = fh.parseGitHeaders(ptr, fh.Buffer.Length);
		    Assert.IsTrue(ptr > 0);

		    Assert.AreEqual("a", fh.OldName);
		    Assert.AreEqual(" c/\u00c5ngstr\u00f6m", fh.NewName);

		    Assert.AreEqual(FileHeader.ChangeTypeEnum.RENAME, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.getOldId());
		    Assert.IsNull(fh.getNewId());

		    Assert.IsNull(fh.GetOldMode());
		    Assert.IsNull(fh.NewMode);

		    Assert.AreEqual(100, fh.getScore());
	    }

        [Test]
	    public void testParseRename100_OldStyle()
        {
		    FileHeader fh = Data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "rename old a\n"
				    + "rename new \" c/\\303\\205ngstr\\303\\266m\"\n");

			int ptr = fh.parseGitFileName(0, fh.Buffer.Length);
		    Assert.IsTrue(ptr > 0);
		    Assert.IsNull(fh.OldName); // can't parse names on a rename
		    Assert.IsNull(fh.NewName);

			ptr = fh.parseGitHeaders(ptr, fh.Buffer.Length);
		    Assert.IsTrue(ptr > 0);

		    Assert.AreEqual("a", fh.OldName);
		    Assert.AreEqual(" c/\u00c5ngstr\u00f6m", fh.NewName);

            Assert.AreEqual(FileHeader.ChangeTypeEnum.RENAME, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.getOldId());
		    Assert.IsNull(fh.getNewId());

		    Assert.IsNull(fh.GetOldMode());
		    Assert.IsNull(fh.NewMode);

		    Assert.AreEqual(100, fh.getScore());
	    }

        [Test]
	    public void testParseCopy100()
        {
		    FileHeader fh = Data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "copy from a\n"
				    + "copy to \" c/\\303\\205ngstr\\303\\266m\"\n");

			int ptr = fh.parseGitFileName(0, fh.Buffer.Length);
		    Assert.IsTrue(ptr > 0);
		    Assert.IsNull(fh.OldName); // can't parse names on a copy
		    Assert.IsNull(fh.NewName);

			ptr = fh.parseGitHeaders(ptr, fh.Buffer.Length);
		    Assert.IsTrue(ptr > 0);

		    Assert.AreEqual("a", fh.OldName);
		    Assert.AreEqual(" c/\u00c5ngstr\u00f6m", fh.NewName);

		    Assert.AreEqual(FileHeader.ChangeTypeEnum.COPY, fh.getChangeType());
		    Assert.AreEqual(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.getOldId());
		    Assert.IsNull(fh.getNewId());

		    Assert.IsNull(fh.GetOldMode());
		    Assert.IsNull(fh.NewMode);

		    Assert.AreEqual(100, fh.getScore());
	    }

        [Test]
	    public void testParseFullIndexLine_WithMode()
        {
		    const string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    const string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = Data("diff --git a/a b/a\n" + "index " + oid
				    + ".." + nid + " 100644\n" + "--- a/a\n" + "+++ b/a\n");

		    AssertParse(fh);

		    Assert.AreEqual("a", fh.OldName);
		    Assert.AreEqual("a", fh.NewName);

		    Assert.AreSame(FileMode.RegularFile, fh.GetOldMode());
		    Assert.AreSame(FileMode.RegularFile, fh.NewMode);
		    Assert.IsFalse(fh.hasMetaDataChanges());

		    Assert.IsNotNull(fh.getOldId());
		    Assert.IsNotNull(fh.getNewId());

		    Assert.IsTrue(fh.getOldId().isComplete());
		    Assert.IsTrue(fh.getNewId().isComplete());

		    Assert.AreEqual(ObjectId.FromString(oid), fh.getOldId().ToObjectId());
		    Assert.AreEqual(ObjectId.FromString(nid), fh.getNewId().ToObjectId());
	    }

	    public void testParseFullIndexLine_NoMode()
        {
		    const string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    const string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = Data("diff --git a/a b/a\n" + "index " + oid
				    + ".." + nid + "\n" + "--- a/a\n" + "+++ b/a\n");

		    AssertParse(fh);

		    Assert.AreEqual("a", fh.OldName);
		    Assert.AreEqual("a", fh.NewName);
		    Assert.IsFalse(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.GetOldMode());
		    Assert.IsNull(fh.NewMode);

		    Assert.IsNotNull(fh.getOldId());
		    Assert.IsNotNull(fh.getNewId());

		    Assert.IsTrue(fh.getOldId().isComplete());
		    Assert.IsTrue(fh.getNewId().isComplete());

		    Assert.AreEqual(ObjectId.FromString(oid), fh.getOldId().ToObjectId());
		    Assert.AreEqual(ObjectId.FromString(nid), fh.getNewId().ToObjectId());
	    }

        [Test]
	    public void testParseAbbrIndexLine_WithMode()
        {
		    const int a = 7;
		    const string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    const string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = Data("diff --git a/a b/a\n" + "index "
				    + oid.Substring(0, a - 1) + ".." + nid.Substring(0, a - 1)
				    + " 100644\n" + "--- a/a\n" + "+++ b/a\n");

		    AssertParse(fh);

		    Assert.AreEqual("a", fh.OldName);
		    Assert.AreEqual("a", fh.NewName);

		    Assert.AreSame(FileMode.RegularFile, fh.GetOldMode());
		    Assert.AreSame(FileMode.RegularFile, fh.NewMode);
		    Assert.IsFalse(fh.hasMetaDataChanges());

		    Assert.IsNotNull(fh.getOldId());
		    Assert.IsNotNull(fh.getNewId());

		    Assert.IsFalse(fh.getOldId().isComplete());
		    Assert.IsFalse(fh.getNewId().isComplete());

		    Assert.AreEqual(oid.Substring(0, a - 1), fh.getOldId().name());
		    Assert.AreEqual(nid.Substring(0, a - 1), fh.getNewId().name());

		    Assert.IsTrue(ObjectId.FromString(oid).startsWith(fh.getOldId()));
		    Assert.IsTrue(ObjectId.FromString(nid).startsWith(fh.getNewId()));
	    }

        [Test]
	    public void testParseAbbrIndexLine_NoMode()
        {
		    const int a = 7;
		    const string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    const string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = Data("diff --git a/a b/a\n" + "index "
				    + oid.Substring(0, a - 1) + ".." + nid.Substring(0, a - 1)
				    + "\n" + "--- a/a\n" + "+++ b/a\n");

		    AssertParse(fh);

		    Assert.AreEqual("a", fh.OldName);
		    Assert.AreEqual("a", fh.NewName);

		    Assert.IsNull(fh.GetOldMode());
		    Assert.IsNull(fh.NewMode);
		    Assert.IsFalse(fh.hasMetaDataChanges());

		    Assert.IsNotNull(fh.getOldId());
		    Assert.IsNotNull(fh.getNewId());

		    Assert.IsFalse(fh.getOldId().isComplete());
		    Assert.IsFalse(fh.getNewId().isComplete());

		    Assert.AreEqual(oid.Substring(0, a - 1), fh.getOldId().name());
		    Assert.AreEqual(nid.Substring(0, a - 1), fh.getNewId().name());

		    Assert.IsTrue(ObjectId.FromString(oid).startsWith(fh.getOldId()));
		    Assert.IsTrue(ObjectId.FromString(nid).startsWith(fh.getNewId()));
	    }

	    private static void AssertParse(FileHeader fh)
        {
			int ptr = fh.parseGitFileName(0, fh.Buffer.Length);
		    Assert.IsTrue(ptr > 0);
			ptr = fh.parseGitHeaders(ptr, fh.Buffer.Length);
		    Assert.IsTrue(ptr > 0);
	    }

	    private static FileHeader Data(string inStr)
        {
		    return new FileHeader(Constants.encodeASCII(inStr), 0);
	    }

	    private static FileHeader Header(string path)
        {
		    return Data(GitLine(path) + "--- " + path + "\n");
	    }

	    private static string GitLine(string path)
        {
		    return "a/" + path + " b/" + path + "\n";
	    }

	    private static FileHeader DqHeader(string path)
        {
		    return Data(DqGitLine(path) + "--- " + path + "\n");
	    }

	    private static string DqGitLine(string path)
        {
		    return "\"a/" + path + "\" \"b/" + path + "\"\n";
	    }
    }
}