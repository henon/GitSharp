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
using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests
{
    [TestFixture]
    public class FileHeaderTest
    {
#if false
	public void testParseGitFileName_Empty() {
		final FileHeader fh = data("");
		assertEquals(-1, fh.parseGitFileName(0, fh.buf.length));
		assertNotNull(fh.getHunks());
		assertTrue(fh.getHunks().isEmpty());
		assertFalse(fh.hasMetaDataChanges());
	}

	public void testParseGitFileName_NoLF() {
		final FileHeader fh = data("a/ b/");
		assertEquals(-1, fh.parseGitFileName(0, fh.buf.length));
	}

	public void testParseGitFileName_NoSecondLine() {
		final FileHeader fh = data("\n");
		assertEquals(-1, fh.parseGitFileName(0, fh.buf.length));
	}

	public void testParseGitFileName_EmptyHeader() {
		final FileHeader fh = data("\n\n");
		assertEquals(1, fh.parseGitFileName(0, fh.buf.length));
	}

	public void testParseGitFileName_Foo() {
		final String name = "foo";
		final FileHeader fh = header(name);
		assertEquals(gitLine(name).length(), fh.parseGitFileName(0,
				fh.buf.length));
		assertEquals(name, fh.getOldName());
		assertSame(fh.getOldName(), fh.getNewName());
		assertFalse(fh.hasMetaDataChanges());
	}

	public void testParseGitFileName_FailFooBar() {
		final FileHeader fh = data("a/foo b/bar\n-");
		assertTrue(fh.parseGitFileName(0, fh.buf.length) > 0);
		assertNull(fh.getOldName());
		assertNull(fh.getNewName());
		assertFalse(fh.hasMetaDataChanges());
	}

	public void testParseGitFileName_FooSpBar() {
		final String name = "foo bar";
		final FileHeader fh = header(name);
		assertEquals(gitLine(name).length(), fh.parseGitFileName(0,
				fh.buf.length));
		assertEquals(name, fh.getOldName());
		assertSame(fh.getOldName(), fh.getNewName());
		assertFalse(fh.hasMetaDataChanges());
	}

	public void testParseGitFileName_DqFooTabBar() {
		final String name = "foo\tbar";
		final String dqName = "foo\\tbar";
		final FileHeader fh = dqHeader(dqName);
		assertEquals(dqGitLine(dqName).length(), fh.parseGitFileName(0,
				fh.buf.length));
		assertEquals(name, fh.getOldName());
		assertSame(fh.getOldName(), fh.getNewName());
		assertFalse(fh.hasMetaDataChanges());
	}

	public void testParseGitFileName_DqFooSpLfNulBar() {
		final String name = "foo \n\0bar";
		final String dqName = "foo \\n\\0bar";
		final FileHeader fh = dqHeader(dqName);
		assertEquals(dqGitLine(dqName).length(), fh.parseGitFileName(0,
				fh.buf.length));
		assertEquals(name, fh.getOldName());
		assertSame(fh.getOldName(), fh.getNewName());
		assertFalse(fh.hasMetaDataChanges());
	}

	public void testParseGitFileName_SrcFooC() {
		final String name = "src/foo/bar/argh/code.c";
		final FileHeader fh = header(name);
		assertEquals(gitLine(name).length(), fh.parseGitFileName(0,
				fh.buf.length));
		assertEquals(name, fh.getOldName());
		assertSame(fh.getOldName(), fh.getNewName());
		assertFalse(fh.hasMetaDataChanges());
	}

	public void testParseGitFileName_SrcFooCNonStandardPrefix() {
		final String name = "src/foo/bar/argh/code.c";
		final String header = "project-v-1.0/" + name + " mydev/" + name + "\n";
		final FileHeader fh = data(header + "-");
		assertEquals(header.length(), fh.parseGitFileName(0, fh.buf.length));
		assertEquals(name, fh.getOldName());
		assertSame(fh.getOldName(), fh.getNewName());
		assertFalse(fh.hasMetaDataChanges());
	}

	public void testParseUnicodeName_NewFile() {
		final FileHeader fh = data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				+ "new file mode 100644\n"
				+ "index 0000000..7898192\n"
				+ "--- /dev/null\n"
				+ "+++ \"b/\\303\\205ngstr\\303\\266m\"\n"
				+ "@@ -0,0 +1 @@\n" + "+a\n");
		assertParse(fh);

		assertEquals("/dev/null", fh.getOldName());
		assertSame(FileHeader.DEV_NULL, fh.getOldName());
		assertEquals("\u00c5ngstr\u00f6m", fh.getNewName());

		assertSame(FileHeader.ChangeType.ADD, fh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		assertTrue(fh.hasMetaDataChanges());

		assertSame(FileMode.MISSING, fh.getOldMode());
		assertSame(FileMode.REGULAR_FILE, fh.getNewMode());

		assertEquals("0000000", fh.getOldId().name());
		assertEquals("7898192", fh.getNewId().name());
		assertEquals(0, fh.getScore());
	}

	public void testParseUnicodeName_DeleteFile() {
		final FileHeader fh = data("diff --git \"a/\\303\\205ngstr\\303\\266m\" \"b/\\303\\205ngstr\\303\\266m\"\n"
				+ "deleted file mode 100644\n"
				+ "index 7898192..0000000\n"
				+ "--- \"a/\\303\\205ngstr\\303\\266m\"\n"
				+ "+++ /dev/null\n"
				+ "@@ -1 +0,0 @@\n" + "-a\n");
		assertParse(fh);

		assertEquals("\u00c5ngstr\u00f6m", fh.getOldName());
		assertEquals("/dev/null", fh.getNewName());
		assertSame(FileHeader.DEV_NULL, fh.getNewName());

		assertSame(FileHeader.ChangeType.DELETE, fh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		assertTrue(fh.hasMetaDataChanges());

		assertSame(FileMode.REGULAR_FILE, fh.getOldMode());
		assertSame(FileMode.MISSING, fh.getNewMode());

		assertEquals("7898192", fh.getOldId().name());
		assertEquals("0000000", fh.getNewId().name());
		assertEquals(0, fh.getScore());
	}

	public void testParseModeChange() {
		final FileHeader fh = data("diff --git a/a b b/a b\n"
				+ "old mode 100644\n" + "new mode 100755\n");
		assertParse(fh);
		assertEquals("a b", fh.getOldName());
		assertEquals("a b", fh.getNewName());

		assertSame(FileHeader.ChangeType.MODIFY, fh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		assertTrue(fh.hasMetaDataChanges());

		assertNull(fh.getOldId());
		assertNull(fh.getNewId());

		assertSame(FileMode.REGULAR_FILE, fh.getOldMode());
		assertSame(FileMode.EXECUTABLE_FILE, fh.getNewMode());
		assertEquals(0, fh.getScore());
	}

	public void testParseRename100_NewStyle() {
		final FileHeader fh = data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				+ "similarity index 100%\n"
				+ "rename from a\n"
				+ "rename to \" c/\\303\\205ngstr\\303\\266m\"\n");
		int ptr = fh.parseGitFileName(0, fh.buf.length);
		assertTrue(ptr > 0);
		assertNull(fh.getOldName()); // can't parse names on a rename
		assertNull(fh.getNewName());

		ptr = fh.parseGitHeaders(ptr, fh.buf.length);
		assertTrue(ptr > 0);

		assertEquals("a", fh.getOldName());
		assertEquals(" c/\u00c5ngstr\u00f6m", fh.getNewName());

		assertSame(FileHeader.ChangeType.RENAME, fh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		assertTrue(fh.hasMetaDataChanges());

		assertNull(fh.getOldId());
		assertNull(fh.getNewId());

		assertNull(fh.getOldMode());
		assertNull(fh.getNewMode());

		assertEquals(100, fh.getScore());
	}

	public void testParseRename100_OldStyle() {
		final FileHeader fh = data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				+ "similarity index 100%\n"
				+ "rename old a\n"
				+ "rename new \" c/\\303\\205ngstr\\303\\266m\"\n");
		int ptr = fh.parseGitFileName(0, fh.buf.length);
		assertTrue(ptr > 0);
		assertNull(fh.getOldName()); // can't parse names on a rename
		assertNull(fh.getNewName());

		ptr = fh.parseGitHeaders(ptr, fh.buf.length);
		assertTrue(ptr > 0);

		assertEquals("a", fh.getOldName());
		assertEquals(" c/\u00c5ngstr\u00f6m", fh.getNewName());

		assertSame(FileHeader.ChangeType.RENAME, fh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		assertTrue(fh.hasMetaDataChanges());

		assertNull(fh.getOldId());
		assertNull(fh.getNewId());

		assertNull(fh.getOldMode());
		assertNull(fh.getNewMode());

		assertEquals(100, fh.getScore());
	}

	public void testParseCopy100() {
		final FileHeader fh = data("diff --git a/a b/ c/\\303\\205ngstr\\303\\266m\n"
				+ "similarity index 100%\n"
				+ "copy from a\n"
				+ "copy to \" c/\\303\\205ngstr\\303\\266m\"\n");
		int ptr = fh.parseGitFileName(0, fh.buf.length);
		assertTrue(ptr > 0);
		assertNull(fh.getOldName()); // can't parse names on a copy
		assertNull(fh.getNewName());

		ptr = fh.parseGitHeaders(ptr, fh.buf.length);
		assertTrue(ptr > 0);

		assertEquals("a", fh.getOldName());
		assertEquals(" c/\u00c5ngstr\u00f6m", fh.getNewName());

		assertSame(FileHeader.ChangeType.COPY, fh.getChangeType());
		assertSame(FileHeader.PatchType.UNIFIED, fh.getPatchType());
		assertTrue(fh.hasMetaDataChanges());

		assertNull(fh.getOldId());
		assertNull(fh.getNewId());

		assertNull(fh.getOldMode());
		assertNull(fh.getNewMode());

		assertEquals(100, fh.getScore());
	}

	public void testParseFullIndexLine_WithMode() {
		final String oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		final String nid = "61780798228d17af2d34fce4cfbdf35556832472";
		final FileHeader fh = data("diff --git a/a b/a\n" + "index " + oid
				+ ".." + nid + " 100644\n" + "--- a/a\n" + "+++ b/a\n");
		assertParse(fh);

		assertEquals("a", fh.getOldName());
		assertEquals("a", fh.getNewName());

		assertSame(FileMode.REGULAR_FILE, fh.getOldMode());
		assertSame(FileMode.REGULAR_FILE, fh.getNewMode());
		assertFalse(fh.hasMetaDataChanges());

		assertNotNull(fh.getOldId());
		assertNotNull(fh.getNewId());

		assertTrue(fh.getOldId().isComplete());
		assertTrue(fh.getNewId().isComplete());

		assertEquals(ObjectId.fromString(oid), fh.getOldId().toObjectId());
		assertEquals(ObjectId.fromString(nid), fh.getNewId().toObjectId());
	}

	public void testParseFullIndexLine_NoMode() {
		final String oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		final String nid = "61780798228d17af2d34fce4cfbdf35556832472";
		final FileHeader fh = data("diff --git a/a b/a\n" + "index " + oid
				+ ".." + nid + "\n" + "--- a/a\n" + "+++ b/a\n");
		assertParse(fh);

		assertEquals("a", fh.getOldName());
		assertEquals("a", fh.getNewName());
		assertFalse(fh.hasMetaDataChanges());

		assertNull(fh.getOldMode());
		assertNull(fh.getNewMode());

		assertNotNull(fh.getOldId());
		assertNotNull(fh.getNewId());

		assertTrue(fh.getOldId().isComplete());
		assertTrue(fh.getNewId().isComplete());

		assertEquals(ObjectId.fromString(oid), fh.getOldId().toObjectId());
		assertEquals(ObjectId.fromString(nid), fh.getNewId().toObjectId());
	}

	public void testParseAbbrIndexLine_WithMode() {
		final int a = 7;
		final String oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		final String nid = "61780798228d17af2d34fce4cfbdf35556832472";
		final FileHeader fh = data("diff --git a/a b/a\n" + "index "
				+ oid.substring(0, a - 1) + ".." + nid.substring(0, a - 1)
				+ " 100644\n" + "--- a/a\n" + "+++ b/a\n");
		assertParse(fh);

		assertEquals("a", fh.getOldName());
		assertEquals("a", fh.getNewName());

		assertSame(FileMode.REGULAR_FILE, fh.getOldMode());
		assertSame(FileMode.REGULAR_FILE, fh.getNewMode());
		assertFalse(fh.hasMetaDataChanges());

		assertNotNull(fh.getOldId());
		assertNotNull(fh.getNewId());

		assertFalse(fh.getOldId().isComplete());
		assertFalse(fh.getNewId().isComplete());

		assertEquals(oid.substring(0, a - 1), fh.getOldId().name());
		assertEquals(nid.substring(0, a - 1), fh.getNewId().name());

		assertTrue(ObjectId.fromString(oid).startsWith(fh.getOldId()));
		assertTrue(ObjectId.fromString(nid).startsWith(fh.getNewId()));
	}

	public void testParseAbbrIndexLine_NoMode() {
		final int a = 7;
		final String oid = "78981922613b2afb6025042ff6bd878ac1994e85";
		final String nid = "61780798228d17af2d34fce4cfbdf35556832472";
		final FileHeader fh = data("diff --git a/a b/a\n" + "index "
				+ oid.substring(0, a - 1) + ".." + nid.substring(0, a - 1)
				+ "\n" + "--- a/a\n" + "+++ b/a\n");
		assertParse(fh);

		assertEquals("a", fh.getOldName());
		assertEquals("a", fh.getNewName());

		assertNull(fh.getOldMode());
		assertNull(fh.getNewMode());
		assertFalse(fh.hasMetaDataChanges());

		assertNotNull(fh.getOldId());
		assertNotNull(fh.getNewId());

		assertFalse(fh.getOldId().isComplete());
		assertFalse(fh.getNewId().isComplete());

		assertEquals(oid.substring(0, a - 1), fh.getOldId().name());
		assertEquals(nid.substring(0, a - 1), fh.getNewId().name());

		assertTrue(ObjectId.fromString(oid).startsWith(fh.getOldId()));
		assertTrue(ObjectId.fromString(nid).startsWith(fh.getNewId()));
	}

	private static void assertParse(final FileHeader fh) {
		int ptr = fh.parseGitFileName(0, fh.buf.length);
		assertTrue(ptr > 0);
		ptr = fh.parseGitHeaders(ptr, fh.buf.length);
		assertTrue(ptr > 0);
	}

	private static FileHeader data(final String in) {
		return new FileHeader(Constants.encodeASCII(in), 0);
	}

	private static FileHeader header(final String path) {
		return data(gitLine(path) + "--- " + path + "\n");
	}

	private static String gitLine(final String path) {
		return "a/" + path + " b/" + path + "\n";
	}

	private static FileHeader dqHeader(final String path) {
		return data(dqGitLine(path) + "--- " + path + "\n");
	}

	private static String dqGitLine(final String path) {
		return "\"a/" + path + "\" \"b/" + path + "\"\n";
	}
#endif
    }
}
