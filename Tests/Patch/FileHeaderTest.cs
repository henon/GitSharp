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
        [Fact]
	    public void testParseGitFileName_Empty()
        {
		    FileHeader fh = data("");
		    Assert.Equal(-1, fh.parseGitFileName(0, fh.buf.Length));
		    Assert.NotNull(fh.getHunks());
		    Assert.True(fh.getHunks().Count == 0);
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [Fact]
	    public void testParseGitFileName_NoLF()
        {
		    FileHeader fh = data("a/ b/");
		    Assert.Equal(-1, fh.parseGitFileName(0, fh.buf.Length));
	    }

        [Fact]
	    public void testParseGitFileName_NoSecondLine()
        {
		    FileHeader fh = data("\n");
		    Assert.Equal(-1, fh.parseGitFileName(0, fh.buf.Length));
	    }

        [Fact]
	    public void testParseGitFileName_EmptyHeader()
        {
		    FileHeader fh = data("\n\n");
		    Assert.Equal(1, fh.parseGitFileName(0, fh.buf.Length));
	    }

        [Fact]
	    public void testParseGitFileName_Foo()
        {
		    string name = "foo";
		    FileHeader fh = header(name);
		    Assert.Equal(gitLine(name).Length, fh.parseGitFileName(0, fh.buf.Length));
		    Assert.Equal(name, fh.getOldName());
		    Assert.Same(fh.getOldName(), fh.getNewName());
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [Fact]
	    public void testParseGitFileName_FailFooBar()
        {
		    FileHeader fh = data("a/foo b/bar\n-");
		    Assert.True(fh.parseGitFileName(0, fh.buf.Length) > 0);
		    Assert.Null(fh.getOldName());
		    Assert.Null(fh.getNewName());
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [Fact]
	    public void testParseGitFileName_FooSpBar()
        {
		    string name = "foo bar";
		    FileHeader fh = header(name);
		    Assert.Equal(gitLine(name).Length, fh.parseGitFileName(0,
				    fh.buf.Length));
		    Assert.Equal(name, fh.getOldName());
		    Assert.Same(fh.getOldName(), fh.getNewName());
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [Fact]
	    public void testParseGitFileName_DqFooTabBar()
        {
		    string name = "foo\tbar";
		    string dqName = "foo\\tbar";
		    FileHeader fh = dqHeader(dqName);
		    Assert.Equal(dqGitLine(dqName).Length, fh.parseGitFileName(0,
				    fh.buf.Length));
		    Assert.Equal(name, fh.getOldName());
		    Assert.Same(fh.getOldName(), fh.getNewName());
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [Fact]
	    public void testParseGitFileName_DqFooSpLfNulBar()
        {
		    string name = "foo \n\0bar";
		    string dqName = "foo \\n\\0bar";
		    FileHeader fh = dqHeader(dqName);
		    Assert.Equal(dqGitLine(dqName).Length, fh.parseGitFileName(0,
				    fh.buf.Length));
		    Assert.Equal(name, fh.getOldName());
		    Assert.Same(fh.getOldName(), fh.getNewName());
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [Fact]
	    public void testParseGitFileName_SrcFooC()
        {
		    string name = "src/foo/bar/argh/code.c";
		    FileHeader fh = header(name);
		    Assert.Equal(gitLine(name).Length, fh.parseGitFileName(0,
				    fh.buf.Length));
		    Assert.Equal(name, fh.getOldName());
		    Assert.Same(fh.getOldName(), fh.getNewName());
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [Fact]
	    public void testParseGitFileName_SrcFooCNonStandardPrefix()
        {
		    string name = "src/foo/bar/argh/code.c";
		    string header = "project-v-1.0/" + name + " mydev/" + name + "\n";
		    FileHeader fh = data(header + "-");
		    Assert.Equal(header.Length, fh.parseGitFileName(0, fh.buf.Length));
		    Assert.Equal(name, fh.getOldName());
		    Assert.Same(fh.getOldName(), fh.getNewName());
		    Assert.False(fh.hasMetaDataChanges());
	    }

        [Fact]
	    public void testParseUnicodeName_NewFile()
        {
		    FileHeader fh = data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "new file mode 100644\n"
				    + "index 0000000..7898192\n"
				    + "--- /dev/null\n"
				    + "+++ \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "@@ -0,0 +1 @@\n" + "+a\n");
		    assertParse(fh);

		    Assert.Equal("/dev/null", fh.getOldName());
		    Assert.Same(FileHeader.DEV_NULL, fh.getOldName());
		    Assert.Equal("\u00c5ngstr\u00f6m", fh.getNewName());

            Assert.Equal(FileHeader.ChangeType.ADD, fh.getChangeType());
            Assert.Equal(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Same(FileMode.Missing, fh.getOldMode());
		    Assert.Same(FileMode.RegularFile, fh.getNewMode());

		    Assert.Equal("0000000", fh.getOldId().name());
		    Assert.Equal("7898192", fh.getNewId().name());
		    Assert.Equal(0, fh.getScore());
	    }

        [Fact]
	    public void testParseUnicodeName_DeleteFile()
        {
		    FileHeader fh = data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "deleted file mode 100644\n"
				    + "index 7898192..0000000\n"
				    + "--- \"a/\\303\\205ngstr\\303\\266m\"\n"
				    + "+++ /dev/null\n"
				    + "@@ -1 +0,0 @@\n" + "-a\n");
		    assertParse(fh);

		    Assert.Equal("\u00c5ngstr\u00f6m", fh.getOldName());
		    Assert.Equal("/dev/null", fh.getNewName());
		    Assert.Same(FileHeader.DEV_NULL, fh.getNewName());

            Assert.Equal(FileHeader.ChangeType.DELETE, fh.getChangeType());
            Assert.Equal(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Same(FileMode.RegularFile, fh.getOldMode());
		    Assert.Same(FileMode.Missing, fh.getNewMode());

		    Assert.Equal("7898192", fh.getOldId().name());
		    Assert.Equal("0000000", fh.getNewId().name());
		    Assert.Equal(0, fh.getScore());
	    }

        [Fact]
	    public void testParseModeChange()
        {
		    FileHeader fh = data("diff --git a/a b b/a b\n"
				    + "old mode 100644\n" + "new mode 100755\n");
		    assertParse(fh);
		    Assert.Equal("a b", fh.getOldName());
		    Assert.Equal("a b", fh.getNewName());

		    Assert.Equal(FileHeader.ChangeType.MODIFY, fh.getChangeType());
            Assert.Equal(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Null(fh.getOldId());
		    Assert.Null(fh.getNewId());

		    Assert.Same(FileMode.RegularFile, fh.getOldMode());
		    Assert.Same(FileMode.ExecutableFile, fh.getNewMode());
		    Assert.Equal(0, fh.getScore());
	    }

        [Fact]
	    public void testParseRename100_NewStyle()
        {
		    FileHeader fh = data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "rename from a\n"
				    + "rename to \" c/\\303\\205ngstr\\303\\266m\"\n");
		    int ptr = fh.parseGitFileName(0, fh.buf.Length);
		    Assert.True(ptr > 0);
		    Assert.Null(fh.getOldName()); // can't parse names on a rename
		    Assert.Null(fh.getNewName());

		    ptr = fh.parseGitHeaders(ptr, fh.buf.Length);
		    Assert.True(ptr > 0);

		    Assert.Equal("a", fh.getOldName());
		    Assert.Equal(" c/\u00c5ngstr\u00f6m", fh.getNewName());

		    Assert.Equal(FileHeader.ChangeType.RENAME, fh.getChangeType());
            Assert.Equal(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Null(fh.getOldId());
		    Assert.Null(fh.getNewId());

		    Assert.Null(fh.getOldMode());
		    Assert.Null(fh.getNewMode());

		    Assert.Equal(100, fh.getScore());
	    }

        [Fact]
	    public void testParseRename100_OldStyle()
        {
		    FileHeader fh = data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "rename old a\n"
				    + "rename new \" c/\\303\\205ngstr\\303\\266m\"\n");
		    int ptr = fh.parseGitFileName(0, fh.buf.Length);
		    Assert.True(ptr > 0);
		    Assert.Null(fh.getOldName()); // can't parse names on a rename
		    Assert.Null(fh.getNewName());

		    ptr = fh.parseGitHeaders(ptr, fh.buf.Length);
		    Assert.True(ptr > 0);

		    Assert.Equal("a", fh.getOldName());
		    Assert.Equal(" c/\u00c5ngstr\u00f6m", fh.getNewName());

            Assert.Equal(FileHeader.ChangeType.RENAME, fh.getChangeType());
            Assert.Equal(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Null(fh.getOldId());
		    Assert.Null(fh.getNewId());

		    Assert.Null(fh.getOldMode());
		    Assert.Null(fh.getNewMode());

		    Assert.Equal(100, fh.getScore());
	    }

        [Fact]
	    public void testParseCopy100()
        {
		    FileHeader fh = data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "copy from a\n"
				    + "copy to \" c/\\303\\205ngstr\\303\\266m\"\n");
		    int ptr = fh.parseGitFileName(0, fh.buf.Length);
		    Assert.True(ptr > 0);
		    Assert.Null(fh.getOldName()); // can't parse names on a copy
		    Assert.Null(fh.getNewName());

		    ptr = fh.parseGitHeaders(ptr, fh.buf.Length);
		    Assert.True(ptr > 0);

		    Assert.Equal("a", fh.getOldName());
		    Assert.Equal(" c/\u00c5ngstr\u00f6m", fh.getNewName());

		    Assert.Equal(FileHeader.ChangeType.COPY, fh.getChangeType());
		    Assert.Equal(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.True(fh.hasMetaDataChanges());

		    Assert.Null(fh.getOldId());
		    Assert.Null(fh.getNewId());

		    Assert.Null(fh.getOldMode());
		    Assert.Null(fh.getNewMode());

		    Assert.Equal(100, fh.getScore());
	    }

        [Fact]
	    public void testParseFullIndexLine_WithMode()
        {
		    string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = data("diff --git a/a b/a\n" + "index " + oid
				    + ".." + nid + " 100644\n" + "--- a/a\n" + "+++ b/a\n");
		    assertParse(fh);

		    Assert.Equal("a", fh.getOldName());
		    Assert.Equal("a", fh.getNewName());

		    Assert.Same(FileMode.RegularFile, fh.getOldMode());
		    Assert.Same(FileMode.RegularFile, fh.getNewMode());
		    Assert.False(fh.hasMetaDataChanges());

		    Assert.NotNull(fh.getOldId());
		    Assert.NotNull(fh.getNewId());

		    Assert.True(fh.getOldId().isComplete());
		    Assert.True(fh.getNewId().isComplete());

		    Assert.Equal(ObjectId.FromString(oid), fh.getOldId().ToObjectId());
		    Assert.Equal(ObjectId.FromString(nid), fh.getNewId().ToObjectId());
	    }

	    public void testParseFullIndexLine_NoMode()
        {
		    string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = data("diff --git a/a b/a\n" + "index " + oid
				    + ".." + nid + "\n" + "--- a/a\n" + "+++ b/a\n");
		    assertParse(fh);

		    Assert.Equal("a", fh.getOldName());
		    Assert.Equal("a", fh.getNewName());
		    Assert.False(fh.hasMetaDataChanges());

		    Assert.Null(fh.getOldMode());
		    Assert.Null(fh.getNewMode());

		    Assert.NotNull(fh.getOldId());
		    Assert.NotNull(fh.getNewId());

		    Assert.True(fh.getOldId().isComplete());
		    Assert.True(fh.getNewId().isComplete());

		    Assert.Equal(ObjectId.FromString(oid), fh.getOldId().ToObjectId());
		    Assert.Equal(ObjectId.FromString(nid), fh.getNewId().ToObjectId());
	    }

        [Fact]
	    public void testParseAbbrIndexLine_WithMode()
        {
		    int a = 7;
		    string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = data("diff --git a/a b/a\n" + "index "
				    + oid.Substring(0, a - 1) + ".." + nid.Substring(0, a - 1)
				    + " 100644\n" + "--- a/a\n" + "+++ b/a\n");
		    assertParse(fh);

		    Assert.Equal("a", fh.getOldName());
		    Assert.Equal("a", fh.getNewName());

		    Assert.Same(FileMode.RegularFile, fh.getOldMode());
		    Assert.Same(FileMode.RegularFile, fh.getNewMode());
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

        [Fact]
	    public void testParseAbbrIndexLine_NoMode()
        {
		    int a = 7;
		    string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = data("diff --git a/a b/a\n" + "index "
				    + oid.Substring(0, a - 1) + ".." + nid.Substring(0, a - 1)
				    + "\n" + "--- a/a\n" + "+++ b/a\n");
		    assertParse(fh);

		    Assert.Equal("a", fh.getOldName());
		    Assert.Equal("a", fh.getNewName());

		    Assert.Null(fh.getOldMode());
		    Assert.Null(fh.getNewMode());
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

	    private static void assertParse(FileHeader fh)
        {
		    int ptr = fh.parseGitFileName(0, fh.buf.Length);
		    Assert.True(ptr > 0);
		    ptr = fh.parseGitHeaders(ptr, fh.buf.Length);
		    Assert.True(ptr > 0);
	    }

	    private static FileHeader data(string inStr)
        {
		    return new FileHeader(Constants.encodeASCII(inStr), 0);
	    }

	    private static FileHeader header(string path)
        {
		    return data(gitLine(path) + "--- " + path + "\n");
	    }

	    private static string gitLine(string path)
        {
		    return "a/" + path + " b/" + path + "\n";
	    }

	    private static FileHeader dqHeader(string path)
        {
		    return data(dqGitLine(path) + "--- " + path + "\n");
	    }

	    private static string dqGitLine(string path)
        {
		    return "\"a/" + path + "\" \"b/" + path + "\"\n";
	    }
    }
}