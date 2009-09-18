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

using GitSharp.Patch;
using Xunit;

namespace GitSharp.Tests.Patch
{
    public class FileHeaderTest : BasePatchTest
    {
        [StrictFactAttribute]
	    public void testParseGitFileName_Empty()
        {
		    FileHeader fh = Data(string.Empty);
			Assert.Equal(-1, fh.parseGitFileName(0, fh.Buffer.Length));
		    Assert.NotNull(fh.Hunks);
		    Assert.True(fh.Hunks.Count == 0);
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_NoLF()
        {
		    FileHeader fh = Data("a/ b/");
			Assert.Equal(-1, fh.parseGitFileName(0, fh.Buffer.Length));
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_NoSecondLine()
        {
		    FileHeader fh = Data("\n");
			Assert.Equal(-1, fh.parseGitFileName(0, fh.Buffer.Length));
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_EmptyHeader()
        {
		    FileHeader fh = Data("\n\n");
			Assert.Equal(1, fh.parseGitFileName(0, fh.Buffer.Length));
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_Foo()
        {
		    const string name = "foo";
		    FileHeader fh = Header(name);
			Assert.Equal(GitLine(name).Length, fh.parseGitFileName(0, fh.Buffer.Length));
		    Assert.Equal(name, fh.OldName);
		    Assert.Same(fh.OldName, fh.NewName);
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_FailFooBar()
        {
		    FileHeader fh = Data("a/foo b/bar\n-");
			Assert.True(fh.parseGitFileName(0, fh.Buffer.Length) > 0);
		    Assert.Null(fh.OldName);
		    Assert.Null(fh.NewName);
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_FooSpBar()
        {
		    const string name = "foo bar";
		    FileHeader fh = Header(name);
		    Assert.Equal(GitLine(name).Length, fh.parseGitFileName(0,
					fh.Buffer.Length));
		    Assert.Equal(name, fh.OldName);
		    Assert.Same(fh.OldName, fh.NewName);
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_DqFooTabBar()
        {
		    const string name = "foo\tbar";
		    const string dqName = "foo\\tbar";
		    FileHeader fh = DqHeader(dqName);
		    Assert.Equal(DqGitLine(dqName).Length, fh.parseGitFileName(0,
					fh.Buffer.Length));
		    Assert.Equal(name, fh.OldName);
		    Assert.Same(fh.OldName, fh.NewName);
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_DqFooSpLfNulBar()
        {
		    const string name = "foo \n\0bar";
		    const string dqName = "foo \\n\\0bar";
		    FileHeader fh = DqHeader(dqName);
		    Assert.Equal(DqGitLine(dqName).Length, fh.parseGitFileName(0,
					fh.Buffer.Length));
		    Assert.Equal(name, fh.OldName);
		    Assert.Same(fh.OldName, fh.NewName);
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_SrcFooC()
        {
		    const string name = "src/foo/bar/argh/code.c";
		    FileHeader fh = Header(name);
		    Assert.Equal(GitLine(name).Length, fh.parseGitFileName(0,
					fh.Buffer.Length));
		    Assert.Equal(name, fh.OldName);
		    Assert.Same(fh.OldName, fh.NewName);
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [StrictFactAttribute]
	    public void testParseGitFileName_SrcFooCNonStandardPrefix()
        {
		    const string name = "src/foo/bar/argh/code.c";
		    const string header = "project-v-1.0/" + name + " mydev/" + name + "\n";
		    FileHeader fh = Data(header + "-");
			Assert.Equal(header.Length, fh.parseGitFileName(0, fh.Buffer.Length));
		    Assert.Equal(name, fh.OldName);
		    Assert.Same(fh.OldName, fh.NewName);
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [StrictFactAttribute]
	    public void testParseUnicodeName_NewFile()
        {
		    FileHeader fh = Data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "new file mode 100644\n"
				    + "index 0000000..7898192\n"
				    + "--- /dev/null\n"
				    + "+++ \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "@@ -0,0 +1 @@\n" + "+a\n");
		    AssertParse(fh);

		    Assert.Equal("/dev/null", fh.OldName);
		    Assert.Same(FileHeader.DEV_NULL, fh.OldName);
		    Assert.Equal("\u00c5ngstr\u00f6m", fh.NewName);

            Assert.Equal(FileHeader.ChangeTypeEnum.ADD, fh.getChangeType());
            Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Same(FileMode.Missing, fh.GetOldMode());
		    Assert.Same(FileMode.RegularFile, fh.NewMode);

		    Assert.Equal("0000000", fh.getOldId().name());
		    Assert.Equal("7898192", fh.getNewId().name());
		    Assert.Equal(0, fh.getScore());
	    }

        [StrictFactAttribute]
	    public void testParseUnicodeName_DeleteFile()
        {
		    FileHeader fh = Data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "deleted file mode 100644\n"
				    + "index 7898192..0000000\n"
				    + "--- \"a/\\303\\205ngstr\\303\\266m\"\n"
				    + "+++ /dev/null\n"
				    + "@@ -1 +0,0 @@\n" + "-a\n");

		    AssertParse(fh);

		    Assert.Equal("\u00c5ngstr\u00f6m", fh.OldName);
		    Assert.Equal("/dev/null", fh.NewName);
		    Assert.Same(FileHeader.DEV_NULL, fh.NewName);

            Assert.Equal(FileHeader.ChangeTypeEnum.DELETE, fh.getChangeType());
            Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Same(FileMode.RegularFile, fh.GetOldMode());
		    Assert.Same(FileMode.Missing, fh.NewMode);

		    Assert.Equal("7898192", fh.getOldId().name());
		    Assert.Equal("0000000", fh.getNewId().name());
		    Assert.Equal(0, fh.getScore());
	    }

        [StrictFactAttribute]
	    public void testParseModeChange()
        {
		    FileHeader fh = Data("diff --git a/a b b/a b\n"
				    + "old mode 100644\n" + "new mode 100755\n");

		    AssertParse(fh);
		    Assert.Equal("a b", fh.OldName);
		    Assert.Equal("a b", fh.NewName);

		    Assert.Equal(FileHeader.ChangeTypeEnum.MODIFY, fh.getChangeType());
            Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Null(fh.getOldId());
		    Assert.Null(fh.getNewId());

		    Assert.Same(FileMode.RegularFile, fh.GetOldMode());
		    Assert.Same(FileMode.ExecutableFile, fh.NewMode);
		    Assert.Equal(0, fh.getScore());
	    }

        [StrictFactAttribute]
	    public void testParseRename100_NewStyle()
        {
		    FileHeader fh = Data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "rename from a\n"
				    + "rename to \" c/\\303\\205ngstr\\303\\266m\"\n");

			int ptr = fh.parseGitFileName(0, fh.Buffer.Length);
		    Assert.True(ptr > 0);
		    Assert.Null(fh.OldName); // can't parse names on a rename
		    Assert.Null(fh.NewName);

			ptr = fh.parseGitHeaders(ptr, fh.Buffer.Length);
		    Assert.True(ptr > 0);

		    Assert.Equal("a", fh.OldName);
		    Assert.Equal(" c/\u00c5ngstr\u00f6m", fh.NewName);

		    Assert.Equal(FileHeader.ChangeTypeEnum.RENAME, fh.getChangeType());
            Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Null(fh.getOldId());
		    Assert.Null(fh.getNewId());

		    Assert.Null(fh.GetOldMode());
		    Assert.Null(fh.NewMode);

		    Assert.Equal(100, fh.getScore());
	    }

        [StrictFactAttribute]
	    public void testParseRename100_OldStyle()
        {
		    FileHeader fh = Data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "rename old a\n"
				    + "rename new \" c/\\303\\205ngstr\\303\\266m\"\n");

			int ptr = fh.parseGitFileName(0, fh.Buffer.Length);
		    Assert.True(ptr > 0);
		    Assert.Null(fh.OldName); // can't parse names on a rename
		    Assert.Null(fh.NewName);

			ptr = fh.parseGitHeaders(ptr, fh.Buffer.Length);
		    Assert.True(ptr > 0);

		    Assert.Equal("a", fh.OldName);
		    Assert.Equal(" c/\u00c5ngstr\u00f6m", fh.NewName);

            Assert.Equal(FileHeader.ChangeTypeEnum.RENAME, fh.getChangeType());
            Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Null(fh.getOldId());
		    Assert.Null(fh.getNewId());

		    Assert.Null(fh.GetOldMode());
		    Assert.Null(fh.NewMode);

		    Assert.Equal(100, fh.getScore());
	    }

        [StrictFactAttribute]
	    public void testParseCopy100()
        {
		    FileHeader fh = Data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "copy from a\n"
				    + "copy to \" c/\\303\\205ngstr\\303\\266m\"\n");

			int ptr = fh.parseGitFileName(0, fh.Buffer.Length);
		    Assert.True(ptr > 0);
		    Assert.Null(fh.OldName); // can't parse names on a copy
		    Assert.Null(fh.NewName);

			ptr = fh.parseGitHeaders(ptr, fh.Buffer.Length);
		    Assert.True(ptr > 0);

		    Assert.Equal("a", fh.OldName);
		    Assert.Equal(" c/\u00c5ngstr\u00f6m", fh.NewName);

		    Assert.Equal(FileHeader.ChangeTypeEnum.COPY, fh.getChangeType());
		    Assert.Equal(FileHeader.PatchTypeEnum.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Null(fh.getOldId());
		    Assert.Null(fh.getNewId());

		    Assert.Null(fh.GetOldMode());
		    Assert.Null(fh.NewMode);

		    Assert.Equal(100, fh.getScore());
	    }

        [StrictFactAttribute]
	    public void testParseFullIndexLine_WithMode()
        {
		    const string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    const string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = Data("diff --git a/a b/a\n" + "index " + oid
				    + ".." + nid + " 100644\n" + "--- a/a\n" + "+++ b/a\n");

		    AssertParse(fh);

		    Assert.Equal("a", fh.OldName);
		    Assert.Equal("a", fh.NewName);

		    Assert.Same(FileMode.RegularFile, fh.GetOldMode());
		    Assert.Same(FileMode.RegularFile, fh.NewMode);
		    Assert.False(fh.hasMetaDataChanges());

		    Assert.NotNull(fh.getOldId());
		    Assert.NotNull(fh.getNewId());

		    Assert.True(fh.getOldId().isComplete());
		    Assert.True(fh.getNewId().isComplete());

		    Assert.Equal(ObjectId.FromString(oid), fh.getOldId().ToObjectId());
		    Assert.Equal(ObjectId.FromString(nid), fh.getNewId().ToObjectId());
	    }

		[StrictFactAttribute]
	    public void testParseFullIndexLine_NoMode()
        {
		    const string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    const string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = Data("diff --git a/a b/a\n" + "index " + oid
				    + ".." + nid + "\n" + "--- a/a\n" + "+++ b/a\n");

		    AssertParse(fh);

		    Assert.Equal("a", fh.OldName);
		    Assert.Equal("a", fh.NewName);
		    Assert.False(fh.hasMetaDataChanges());

		    Assert.Null(fh.GetOldMode());
		    Assert.Null(fh.NewMode);

		    Assert.NotNull(fh.getOldId());
		    Assert.NotNull(fh.getNewId());

		    Assert.True(fh.getOldId().isComplete());
		    Assert.True(fh.getNewId().isComplete());

		    Assert.Equal(ObjectId.FromString(oid), fh.getOldId().ToObjectId());
		    Assert.Equal(ObjectId.FromString(nid), fh.getNewId().ToObjectId());
	    }

        [StrictFactAttribute]
	    public void testParseAbbrIndexLine_WithMode()
        {
		    const int a = 7;
		    const string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    const string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = Data("diff --git a/a b/a\n" + "index "
				    + oid.Substring(0, a - 1) + ".." + nid.Substring(0, a - 1)
				    + " 100644\n" + "--- a/a\n" + "+++ b/a\n");

		    AssertParse(fh);

		    Assert.Equal("a", fh.OldName);
		    Assert.Equal("a", fh.NewName);

		    Assert.Same(FileMode.RegularFile, fh.GetOldMode());
		    Assert.Same(FileMode.RegularFile, fh.NewMode);
		    Assert.False(fh.hasMetaDataChanges());

		    Assert.NotNull(fh.getOldId());
		    Assert.NotNull(fh.getNewId());

		    Assert.False(fh.getOldId().isComplete());
		    Assert.False(fh.getNewId().isComplete());

		    Assert.Equal(oid.Substring(0, a - 1), fh.getOldId().name());
		    Assert.Equal(nid.Substring(0, a - 1), fh.getNewId().name());

		    Assert.True(ObjectId.FromString(oid).startsWith(fh.getOldId()));
		    Assert.True(ObjectId.FromString(nid).startsWith(fh.getNewId()));
	    }

        [StrictFactAttribute]
	    public void testParseAbbrIndexLine_NoMode()
        {
		    const int a = 7;
		    const string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    const string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = Data("diff --git a/a b/a\n" + "index "
				    + oid.Substring(0, a - 1) + ".." + nid.Substring(0, a - 1)
				    + "\n" + "--- a/a\n" + "+++ b/a\n");

		    AssertParse(fh);

		    Assert.Equal("a", fh.OldName);
		    Assert.Equal("a", fh.NewName);

		    Assert.Null(fh.GetOldMode());
		    Assert.Null(fh.NewMode);
		    Assert.False(fh.hasMetaDataChanges());

		    Assert.NotNull(fh.getOldId());
		    Assert.NotNull(fh.getNewId());

		    Assert.False(fh.getOldId().isComplete());
		    Assert.False(fh.getNewId().isComplete());

		    Assert.Equal(oid.Substring(0, a - 1), fh.getOldId().name());
		    Assert.Equal(nid.Substring(0, a - 1), fh.getNewId().name());

		    Assert.True(ObjectId.FromString(oid).startsWith(fh.getOldId()));
		    Assert.True(ObjectId.FromString(nid).startsWith(fh.getNewId()));
	    }

	    private static void AssertParse(FileHeader fh)
        {
			int ptr = fh.parseGitFileName(0, fh.Buffer.Length);
		    Assert.True(ptr > 0);
			ptr = fh.parseGitHeaders(ptr, fh.Buffer.Length);
		    Assert.True(ptr > 0);
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