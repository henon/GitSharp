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
using NUnit.Framework;

namespace GitSharp.Tests.Patch
{
    [TestFixture]
    public class FileHeaderTest : BasePatchTest
    {
        [Test]
	    public void testParseGitFileName_Empty()
        {
		    FileHeader fh = data(string.Empty);
		    Assert.AreEqual(-1, fh.parseGitFileName(0, fh.buf.Length));
		    Assert.IsNotNull(fh.getHunks());
		    Assert.IsTrue(fh.getHunks().Count == 0);
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_NoLF()
        {
		    FileHeader fh = data("a/ b/");
		    Assert.AreEqual(-1, fh.parseGitFileName(0, fh.buf.Length));
	    }

        [Test]
	    public void testParseGitFileName_NoSecondLine()
        {
		    FileHeader fh = data("\n");
		    Assert.AreEqual(-1, fh.parseGitFileName(0, fh.buf.Length));
	    }

        [Test]
	    public void testParseGitFileName_EmptyHeader()
        {
		    FileHeader fh = data("\n\n");
		    Assert.AreEqual(1, fh.parseGitFileName(0, fh.buf.Length));
	    }

        [Test]
	    public void testParseGitFileName_Foo()
        {
		    string name = "foo";
		    FileHeader fh = header(name);
		    Assert.AreEqual(gitLine(name).Length, fh.parseGitFileName(0, fh.buf.Length));
		    Assert.AreEqual(name, fh.getOldName());
		    Assert.AreSame(fh.getOldName(), fh.getNewName());
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_FailFooBar()
        {
		    FileHeader fh = data("a/foo b/bar\n-");
		    Assert.IsTrue(fh.parseGitFileName(0, fh.buf.Length) > 0);
		    Assert.IsNull(fh.getOldName());
		    Assert.IsNull(fh.getNewName());
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_FooSpBar()
        {
		    string name = "foo bar";
		    FileHeader fh = header(name);
		    Assert.AreEqual(gitLine(name).Length, fh.parseGitFileName(0,
				    fh.buf.Length));
		    Assert.AreEqual(name, fh.getOldName());
		    Assert.AreSame(fh.getOldName(), fh.getNewName());
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_DqFooTabBar()
        {
		    string name = "foo\tbar";
		    string dqName = "foo\\tbar";
		    FileHeader fh = dqHeader(dqName);
		    Assert.AreEqual(dqGitLine(dqName).Length, fh.parseGitFileName(0,
				    fh.buf.Length));
		    Assert.AreEqual(name, fh.getOldName());
		    Assert.AreSame(fh.getOldName(), fh.getNewName());
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_DqFooSpLfNulBar()
        {
		    string name = "foo \n\0bar";
		    string dqName = "foo \\n\\0bar";
		    FileHeader fh = dqHeader(dqName);
		    Assert.AreEqual(dqGitLine(dqName).Length, fh.parseGitFileName(0,
				    fh.buf.Length));
		    Assert.AreEqual(name, fh.getOldName());
		    Assert.AreSame(fh.getOldName(), fh.getNewName());
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_SrcFooC()
        {
		    string name = "src/foo/bar/argh/code.c";
		    FileHeader fh = header(name);
		    Assert.AreEqual(gitLine(name).Length, fh.parseGitFileName(0,
				    fh.buf.Length));
		    Assert.AreEqual(name, fh.getOldName());
		    Assert.AreSame(fh.getOldName(), fh.getNewName());
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseGitFileName_SrcFooCNonStandardPrefix()
        {
		    string name = "src/foo/bar/argh/code.c";
		    string header = "project-v-1.0/" + name + " mydev/" + name + "\n";
		    FileHeader fh = data(header + "-");
		    Assert.AreEqual(header.Length, fh.parseGitFileName(0, fh.buf.Length));
		    Assert.AreEqual(name, fh.getOldName());
		    Assert.AreSame(fh.getOldName(), fh.getNewName());
		    Assert.IsFalse(fh.hasMetaDataChanges());
	    }

        [Test]
	    public void testParseUnicodeName_NewFile()
        {
		    FileHeader fh = data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "new File mode 100644\n"
				    + "index 0000000..7898192\n"
				    + "--- /dev/null\n"
				    + "+++ \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "@@ -0,0 +1 @@\n" + "+a\n");
		    assertParse(fh);

		    Assert.AreEqual("/dev/null", fh.getOldName());
		    Assert.AreSame(FileHeader.DEV_NULL, fh.getOldName());
		    Assert.AreEqual("\u00c5ngstr\u00f6m", fh.getNewName());

            Assert.AreEqual(FileHeader.ChangeType.ADD, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.AreSame(FileMode.Missing, fh.getOldMode());
		    Assert.AreSame(FileMode.RegularFile, fh.getNewMode());

		    Assert.AreEqual("0000000", fh.getOldId().name());
		    Assert.AreEqual("7898192", fh.getNewId().name());
		    Assert.AreEqual(0, fh.getScore());
	    }

        [Test]
	    public void testParseUnicodeName_DeleteFile()
        {
		    FileHeader fh = data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				    + "deleted File mode 100644\n"
				    + "index 7898192..0000000\n"
				    + "--- \"a/\\303\\205ngstr\\303\\266m\"\n"
				    + "+++ /dev/null\n"
				    + "@@ -1 +0,0 @@\n" + "-a\n");
		    assertParse(fh);

		    Assert.AreEqual("\u00c5ngstr\u00f6m", fh.getOldName());
		    Assert.AreEqual("/dev/null", fh.getNewName());
		    Assert.AreSame(FileHeader.DEV_NULL, fh.getNewName());

            Assert.AreEqual(FileHeader.ChangeType.DELETE, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.AreSame(FileMode.RegularFile, fh.getOldMode());
		    Assert.AreSame(FileMode.Missing, fh.getNewMode());

		    Assert.AreEqual("7898192", fh.getOldId().name());
		    Assert.AreEqual("0000000", fh.getNewId().name());
		    Assert.AreEqual(0, fh.getScore());
	    }

        [Test]
	    public void testParseModeChange()
        {
		    FileHeader fh = data("diff --git a/a b b/a b\n"
				    + "old mode 100644\n" + "new mode 100755\n");
		    assertParse(fh);
		    Assert.AreEqual("a b", fh.getOldName());
		    Assert.AreEqual("a b", fh.getNewName());

		    Assert.AreEqual(FileHeader.ChangeType.MODIFY, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.getOldId());
		    Assert.IsNull(fh.getNewId());

		    Assert.AreSame(FileMode.RegularFile, fh.getOldMode());
		    Assert.AreSame(FileMode.ExecutableFile, fh.getNewMode());
		    Assert.AreEqual(0, fh.getScore());
	    }

        [Test]
	    public void testParseRename100_NewStyle()
        {
		    FileHeader fh = data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "rename from a\n"
				    + "rename to \" c/\\303\\205ngstr\\303\\266m\"\n");
		    int ptr = fh.parseGitFileName(0, fh.buf.Length);
		    Assert.IsTrue(ptr > 0);
		    Assert.IsNull(fh.getOldName()); // can't parse names on a rename
		    Assert.IsNull(fh.getNewName());

		    ptr = fh.parseGitHeaders(ptr, fh.buf.Length);
		    Assert.IsTrue(ptr > 0);

		    Assert.AreEqual("a", fh.getOldName());
		    Assert.AreEqual(" c/\u00c5ngstr\u00f6m", fh.getNewName());

		    Assert.AreEqual(FileHeader.ChangeType.RENAME, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.getOldId());
		    Assert.IsNull(fh.getNewId());

		    Assert.IsNull(fh.getOldMode());
		    Assert.IsNull(fh.getNewMode());

		    Assert.AreEqual(100, fh.getScore());
	    }

        [Test]
	    public void testParseRename100_OldStyle()
        {
		    FileHeader fh = data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "rename old a\n"
				    + "rename new \" c/\\303\\205ngstr\\303\\266m\"\n");
		    int ptr = fh.parseGitFileName(0, fh.buf.Length);
		    Assert.IsTrue(ptr > 0);
		    Assert.IsNull(fh.getOldName()); // can't parse names on a rename
		    Assert.IsNull(fh.getNewName());

		    ptr = fh.parseGitHeaders(ptr, fh.buf.Length);
		    Assert.IsTrue(ptr > 0);

		    Assert.AreEqual("a", fh.getOldName());
		    Assert.AreEqual(" c/\u00c5ngstr\u00f6m", fh.getNewName());

            Assert.AreEqual(FileHeader.ChangeType.RENAME, fh.getChangeType());
            Assert.AreEqual(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.getOldId());
		    Assert.IsNull(fh.getNewId());

		    Assert.IsNull(fh.getOldMode());
		    Assert.IsNull(fh.getNewMode());

		    Assert.AreEqual(100, fh.getScore());
	    }

        [Test]
	    public void testParseCopy100()
        {
		    FileHeader fh = data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				    + "similarity index 100%\n"
				    + "copy from a\n"
				    + "copy to \" c/\\303\\205ngstr\\303\\266m\"\n");
		    int ptr = fh.parseGitFileName(0, fh.buf.Length);
		    Assert.IsTrue(ptr > 0);
		    Assert.IsNull(fh.getOldName()); // can't parse names on a copy
		    Assert.IsNull(fh.getNewName());

		    ptr = fh.parseGitHeaders(ptr, fh.buf.Length);
		    Assert.IsTrue(ptr > 0);

		    Assert.AreEqual("a", fh.getOldName());
		    Assert.AreEqual(" c/\u00c5ngstr\u00f6m", fh.getNewName());

		    Assert.AreEqual(FileHeader.ChangeType.COPY, fh.getChangeType());
		    Assert.AreEqual(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		    Assert.IsTrue(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.getOldId());
		    Assert.IsNull(fh.getNewId());

		    Assert.IsNull(fh.getOldMode());
		    Assert.IsNull(fh.getNewMode());

		    Assert.AreEqual(100, fh.getScore());
	    }

        [Test]
	    public void testParseFullIndexLine_WithMode()
        {
		    string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = data("diff --git a/a b/a\n" + "index " + oid
				    + ".." + nid + " 100644\n" + "--- a/a\n" + "+++ b/a\n");
		    assertParse(fh);

		    Assert.AreEqual("a", fh.getOldName());
		    Assert.AreEqual("a", fh.getNewName());

		    Assert.AreSame(FileMode.RegularFile, fh.getOldMode());
		    Assert.AreSame(FileMode.RegularFile, fh.getNewMode());
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
		    string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = data("diff --git a/a b/a\n" + "index " + oid
				    + ".." + nid + "\n" + "--- a/a\n" + "+++ b/a\n");
		    assertParse(fh);

		    Assert.AreEqual("a", fh.getOldName());
		    Assert.AreEqual("a", fh.getNewName());
		    Assert.IsFalse(fh.hasMetaDataChanges());

		    Assert.IsNull(fh.getOldMode());
		    Assert.IsNull(fh.getNewMode());

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
		    int a = 7;
		    string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = data("diff --git a/a b/a\n" + "index "
				    + oid.Substring(0, a - 1) + ".." + nid.Substring(0, a - 1)
				    + " 100644\n" + "--- a/a\n" + "+++ b/a\n");
		    assertParse(fh);

		    Assert.AreEqual("a", fh.getOldName());
		    Assert.AreEqual("a", fh.getNewName());

		    Assert.AreSame(FileMode.RegularFile, fh.getOldMode());
		    Assert.AreSame(FileMode.RegularFile, fh.getNewMode());
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
		    int a = 7;
		    string oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		    string nid = "61780798228d17af2d34fce4cfbdf35556832472";
		    FileHeader fh = data("diff --git a/a b/a\n" + "index "
				    + oid.Substring(0, a - 1) + ".." + nid.Substring(0, a - 1)
				    + "\n" + "--- a/a\n" + "+++ b/a\n");
		    assertParse(fh);

		    Assert.AreEqual("a", fh.getOldName());
		    Assert.AreEqual("a", fh.getNewName());

		    Assert.IsNull(fh.getOldMode());
		    Assert.IsNull(fh.getNewMode());
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

	    private static void assertParse(FileHeader fh)
        {
		    int ptr = fh.parseGitFileName(0, fh.buf.Length);
		    Assert.IsTrue(ptr > 0);
		    ptr = fh.parseGitHeaders(ptr, fh.buf.Length);
		    Assert.IsTrue(ptr > 0);
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