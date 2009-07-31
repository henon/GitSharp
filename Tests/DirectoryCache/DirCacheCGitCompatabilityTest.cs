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

namespace GitSharp.Tests.DirectoryCache
{
    using NUnit.Framework;
    [TestFixture]
    public class DirCacheCGitCompatabilityTest : RepositoryTestCase
    {
#if false
	private final File index = pathOf("gitgit.index");

	public void testReadIndex_LsFiles() throws Exception {
		final Map<String, CGitIndexRecord> ls = readLsFiles();
		final DirCache dc = new DirCache(index);
		assertEquals(0, dc.getEntryCount());
		dc.read();
		assertEquals(ls.size(), dc.getEntryCount());
		{
			final Iterator<CGitIndexRecord> rItr = ls.values().iterator();
			for (int i = 0; rItr.hasNext(); i++)
				assertEqual(rItr.next(), dc.getEntry(i));
		}
	}

	public void testTreeWalk_LsFiles() throws Exception {
		final Map<String, CGitIndexRecord> ls = readLsFiles();
		final DirCache dc = new DirCache(index);
		assertEquals(0, dc.getEntryCount());
		dc.read();
		assertEquals(ls.size(), dc.getEntryCount());
		{
			final Iterator<CGitIndexRecord> rItr = ls.values().iterator();
			final TreeWalk tw = new TreeWalk(db);
			tw.reset();
			tw.setRecursive(true);
			tw.addTree(new DirCacheIterator(dc));
			while (rItr.hasNext()) {
				final DirCacheIterator dcItr;

				assertTrue(tw.next());
				dcItr = tw.getTree(0, DirCacheIterator.class);
				assertNotNull(dcItr);

				assertEqual(rItr.next(), dcItr.getDirCacheEntry());
			}
		}
	}

	private static void assertEqual(final CGitIndexRecord c,
			final DirCacheEntry j) {
		assertNotNull(c);
		assertNotNull(j);

		assertEquals(c.path, j.getPathString());
		assertEquals(c.id, j.getObjectId());
		assertEquals(c.mode, j.getRawMode());
		assertEquals(c.stage, j.getStage());
	}

	public void testReadIndex_DirCacheTree() throws Exception {
		final Map<String, CGitIndexRecord> cList = readLsFiles();
		final Map<String, CGitLsTreeRecord> cTree = readLsTree();
		final DirCache dc = new DirCache(index);
		assertEquals(0, dc.getEntryCount());
		dc.read();
		assertEquals(cList.size(), dc.getEntryCount());

		final DirCacheTree jTree = dc.getCacheTree(false);
		assertNotNull(jTree);
		assertEquals("", jTree.getNameString());
		assertEquals("", jTree.getPathString());
		assertTrue(jTree.isValid());
		assertEquals(ObjectId
				.fromString("698dd0b8d0c299f080559a1cffc7fe029479a408"), jTree
				.getObjectId());
		assertEquals(cList.size(), jTree.getEntrySpan());

		final ArrayList<CGitLsTreeRecord> subtrees = new ArrayList<CGitLsTreeRecord>();
		for (final CGitLsTreeRecord r : cTree.values()) {
			if (FileMode.TREE.equals(r.mode))
				subtrees.add(r);
		}
		assertEquals(subtrees.size(), jTree.getChildCount());

		for (int i = 0; i < jTree.getChildCount(); i++) {
			final DirCacheTree sj = jTree.getChild(i);
			final CGitLsTreeRecord sc = subtrees.get(i);
			assertEquals(sc.path, sj.getNameString());
			assertEquals(sc.path + "/", sj.getPathString());
			assertTrue(sj.isValid());
			assertEquals(sc.id, sj.getObjectId());
		}
	}

	private File pathOf(final String name) {
		return JGitTestUtil.getTestResourceFile(name);
	}

	private Map<String, CGitIndexRecord> readLsFiles() throws Exception {
		final LinkedHashMap<String, CGitIndexRecord> r = new LinkedHashMap<String, CGitIndexRecord>();
		final BufferedReader br = new BufferedReader(new InputStreamReader(
				new FileInputStream(pathOf("gitgit.lsfiles")), "UTF-8"));
		try {
			String line;
			while ((line = br.readLine()) != null) {
				final CGitIndexRecord cr = new CGitIndexRecord(line);
				r.put(cr.path, cr);
			}
		} finally {
			br.close();
		}
		return r;
	}

	private Map<String, CGitLsTreeRecord> readLsTree() throws Exception {
		final LinkedHashMap<String, CGitLsTreeRecord> r = new LinkedHashMap<String, CGitLsTreeRecord>();
		final BufferedReader br = new BufferedReader(new InputStreamReader(
				new FileInputStream(pathOf("gitgit.lstree")), "UTF-8"));
		try {
			String line;
			while ((line = br.readLine()) != null) {
				final CGitLsTreeRecord cr = new CGitLsTreeRecord(line);
				r.put(cr.path, cr);
			}
		} finally {
			br.close();
		}
		return r;
	}

	private static class CGitIndexRecord {
		final int mode;

		final ObjectId id;

		final int stage;

		final String path;

		CGitIndexRecord(final String line) {
			final int tab = line.indexOf('\t');
			final int sp1 = line.indexOf(' ');
			final int sp2 = line.indexOf(' ', sp1 + 1);
			mode = Integer.parseInt(line.substring(0, sp1), 8);
			id = ObjectId.fromString(line.substring(sp1 + 1, sp2));
			stage = Integer.parseInt(line.substring(sp2 + 1, tab));
			path = line.substring(tab + 1);
		}
	}

	private static class CGitLsTreeRecord {
		final int mode;

		final ObjectId id;

		final String path;

		CGitLsTreeRecord(final String line) {
			final int tab = line.indexOf('\t');
			final int sp1 = line.indexOf(' ');
			final int sp2 = line.indexOf(' ', sp1 + 1);
			mode = Integer.parseInt(line.substring(0, sp1), 8);
			id = ObjectId.fromString(line.substring(sp2 + 1, tab));
			path = line.substring(tab + 1);
		}
	}
#endif
    }
}
