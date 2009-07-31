/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
    public class ReadTreeTest : RepositoryTestCase
    {
#if false
	private Tree theHead;
	private Tree theMerge;
	private GitIndex theIndex;
	private WorkDirCheckout theReadTree;
	// Each of these rules are from the read-tree manpage
	// go there to see what they mean.
	// Rule 0 is left out for obvious reasons :)
	public void testRules1thru3_NoIndexEntry() throws IOException {
		GitIndex index = new GitIndex(db);
		
		Tree head = new Tree(db);
		FileTreeEntry headFile = head.addFile("foo");
		ObjectId objectId = ObjectId.fromString("ba78e065e2c261d4f7b8f42107588051e87e18e9");
		headFile.setId(objectId);
		Tree merge = new Tree(db);
		
		WorkDirCheckout readTree = new WorkDirCheckout(db, trash, head, index, merge);
		readTree.prescanTwoTrees();
		
		assertTrue(readTree.removed.contains("foo"));
		
		readTree = new WorkDirCheckout(db, trash, merge, index, head);
		readTree.prescanTwoTrees();
		
		assertEquals(objectId, readTree.updated.get("foo"));
		
		ObjectId anotherId = ObjectId.fromString("ba78e065e2c261d4f7b8f42107588051e87e18ee");
		merge.addFile("foo").setId(anotherId);
		
		readTree = new WorkDirCheckout(db, trash, head, index, merge);
		readTree.prescanTwoTrees();
		
		assertEquals(anotherId, readTree.updated.get("foo"));
	}

	void setupCase(HashMap<String, String> headEntries, 
			HashMap<String, String> mergeEntries, 
			HashMap<String, String> indexEntries) throws IOException {
		theHead = buildTree(headEntries);
		theMerge = buildTree(mergeEntries);
		theIndex = buildIndex(indexEntries);
	}
	
	private GitIndex buildIndex(HashMap<String, String> indexEntries) throws IOException {
		GitIndex index = new GitIndex(db);
		
		if (indexEntries == null)
			return index;
		for (java.util.Map.Entry<String,String> e : indexEntries.entrySet()) {
			index.add(trash, writeTrashFile(e.getKey(), e.getValue())).forceRecheck();
		}
		
		return index;
	}

	private Tree buildTree(HashMap<String, String> headEntries) throws IOException {
		Tree tree = new Tree(db);
		
		if (headEntries == null) 
			return tree;
		for (java.util.Map.Entry<String,String> e : headEntries.entrySet()) {
			tree.addFile(e.getKey()).setId(genSha1(e.getValue()));
		}
		
		return tree;
	}
	
	ObjectId genSha1(String data) {
		InputStream is = new ByteArrayInputStream(data.getBytes());
		ObjectWriter objectWriter = new ObjectWriter(db);
		try {
			return objectWriter.writeObject(Constants.OBJ_BLOB, data
					.getBytes().length, is, true);
		} catch (IOException e) {
			fail(e.toString());
		}
		return null;
	}
	
	private WorkDirCheckout go() throws IOException {
		theReadTree = new WorkDirCheckout(db, trash, theHead, theIndex, theMerge);
		theReadTree.prescanTwoTrees();
		return theReadTree;
	}

    // for these rules, they all have clean yes/no options
	// but it doesn't matter if the entry is clean or not
	// so we can just ignore the state in the filesystem entirely
	public void testRules4thru13_IndexEntryNotInHead() throws IOException {
		// rules 4 and 5
		HashMap<String, String> idxMap;

		idxMap = new HashMap<String, String>();
		idxMap.put("foo", "foo");
		setupCase(null, null, idxMap);
		theReadTree = go();
		
		assertTrue(theReadTree.updated.isEmpty());
		assertTrue(theReadTree.removed.isEmpty());
		assertTrue(theReadTree.conflicts.isEmpty());
		
		// rules 6 and 7
		idxMap = new HashMap<String, String>();
		idxMap.put("foo", "foo");
		setupCase(null, idxMap, idxMap);
		theReadTree = go();

		assertAllEmpty();
		
		// rules 8 and 9
		HashMap<String, String> mergeMap;
		mergeMap = new HashMap<String, String>();
		
		mergeMap.put("foo", "merge");
		setupCase(null, mergeMap, idxMap);
		go();
		
		assertTrue(theReadTree.updated.isEmpty());
		assertTrue(theReadTree.removed.isEmpty());
		assertTrue(theReadTree.conflicts.contains("foo"));
		
		// rule 10
		
		HashMap<String, String> headMap = new HashMap<String, String>();
		headMap.put("foo", "foo");
		setupCase(headMap, null, idxMap);
		go();
		
		assertTrue(theReadTree.removed.contains("foo"));
		assertTrue(theReadTree.updated.isEmpty());
		assertTrue(theReadTree.conflicts.isEmpty());
		
		// rule 11
		setupCase(headMap, null, idxMap);
		new File(trash, "foo").delete();
		writeTrashFile("foo", "bar");
		theIndex.getMembers()[0].forceRecheck();
		go();
		
		assertTrue(theReadTree.removed.isEmpty());
		assertTrue(theReadTree.updated.isEmpty());
		assertTrue(theReadTree.conflicts.contains("foo"));
		
		// rule 12 & 13
		headMap.put("foo", "head");
		setupCase(headMap, null, idxMap);
		go();
		
		assertTrue(theReadTree.removed.isEmpty());
		assertTrue(theReadTree.updated.isEmpty());
		assertTrue(theReadTree.conflicts.contains("foo"));
		
		// rules 14 & 15
		setupCase(headMap, headMap, idxMap);
		go();
		
		assertAllEmpty();
		
		// rules 16 & 17
		setupCase(headMap, mergeMap, idxMap); go();
		assertTrue(theReadTree.conflicts.contains("foo"));
		
		// rules 18 & 19
		setupCase(headMap, idxMap, idxMap); go();
		assertAllEmpty();
		
		// rule 20
		setupCase(idxMap, mergeMap, idxMap); go();
		assertTrue(theReadTree.updated.containsKey("foo"));
		
		// rules 21
		setupCase(idxMap, mergeMap, idxMap); 
		new File(trash, "foo").delete();
		writeTrashFile("foo", "bar");
		theIndex.getMembers()[0].forceRecheck();
		go();
		assertTrue(theReadTree.conflicts.contains("foo"));
	}

	private void assertAllEmpty() {
		assertTrue(theReadTree.removed.isEmpty());
		assertTrue(theReadTree.updated.isEmpty());
		assertTrue(theReadTree.conflicts.isEmpty());
	}
	
	public void testDirectoryFileSimple() throws IOException {
		theIndex = new GitIndex(db);
		theIndex.add(trash, writeTrashFile("DF", "DF"));
		Tree treeDF = db.mapTree(theIndex.writeTree());
		
		recursiveDelete(new File(trash, "DF"));
		theIndex = new GitIndex(db);
		theIndex.add(trash, writeTrashFile("DF/DF", "DF/DF"));
		Tree treeDFDF = db.mapTree(theIndex.writeTree());
		
		theIndex = new GitIndex(db);
		recursiveDelete(new File(trash, "DF"));
		
		theIndex.add(trash, writeTrashFile("DF", "DF"));
		theReadTree = new WorkDirCheckout(db, trash, treeDF, theIndex, treeDFDF);
		theReadTree.prescanTwoTrees();
		
		assertTrue(theReadTree.removed.contains("DF"));
		assertTrue(theReadTree.updated.containsKey("DF/DF"));
		
		recursiveDelete(new File(trash, "DF"));
		theIndex = new GitIndex(db);
		theIndex.add(trash, writeTrashFile("DF/DF", "DF/DF"));
		
		theReadTree = new WorkDirCheckout(db, trash, treeDFDF, theIndex, treeDF);
		theReadTree.prescanTwoTrees();
		assertTrue(theReadTree.removed.contains("DF/DF"));
		assertTrue(theReadTree.updated.containsKey("DF"));
	}
	
	/*
	 * Directory/File Conflict cases:
	 * It's entirely possible that in practice a number of these may be equivalent
	 * to the cases described in git-read-tree.txt. As long as it does the right thing,
	 * that's all I care about. These are basically reverse-engineered from
	 * what git currently does. If there are tests for these in git, it's kind of
	 * hard to track them all down...
	 * 
	 *     H        I       M     Clean     H==M     H==I    I==M         Result
	 *     ------------------------------------------------------------------
	 *1    D        D       F       Y         N       Y       N           Update
	 *2    D        D       F       N         N       Y       N           Conflict
	 *3    D        F       D                 Y       N       N           Update
	 *4    D        F       D                 N       N       N           Update
	 *5    D        F       F       Y         N       N       Y           Keep
	 *6    D        F       F       N         N       N       Y           Keep
	 *7    F        D       F       Y         Y       N       N           Update
	 *8    F        D       F       N         Y       N       N           Conflict
	 *9    F        D       F       Y         N       N       N           Update
	 *10   F        D       D                 N       N       Y           Keep
	 *11   F		D		D				  N		  N		  N			  Conflict			
	 *12   F		F		D		Y		  N		  Y		  N			  Update
	 *13   F		F		D		N		  N		  Y		  N			  Conflict
	 *14   F		F		D				  N		  N		  N			  Conflict
	 *15   0		F		D				  N		  N		  N			  Conflict
	 *16   0		D		F		Y		  N		  N		  N			  Update
	 *17   0		D		F		 		  N		  N		  N			  Conflict
	 *18   F        0       D    										  Update
	 *19   D	    0       F											  Update
	 */
	
	public void testDirectoryFileConflicts_1() throws Exception {
		// 1
		doit(mk("DF/DF"), mk("DF"), mk("DF/DF"));
		assertNoConflicts();
		assertUpdated("DF");
		assertRemoved("DF/DF");
	}

	public void testDirectoryFileConflicts_2() throws Exception {
		// 2
		setupCase(mk("DF/DF"), mk("DF"), mk("DF/DF"));
		writeTrashFile("DF/DF", "different");
		go();
		assertConflict("DF/DF");
		
	}

	public void testDirectoryFileConflicts_3() throws Exception {
		// 3 - the first to break!
		doit(mk("DF/DF"), mk("DF/DF"), mk("DF"));
		assertUpdated("DF/DF");
		assertRemoved("DF");
	}

	public void testDirectoryFileConflicts_4() throws Exception {
		// 4 (basically same as 3, just with H and M different)
		doit(mk("DF/DF"), mkmap("DF/DF", "foo"), mk("DF"));
		assertUpdated("DF/DF");
		assertRemoved("DF");
		
	}

	public void testDirectoryFileConflicts_5() throws Exception {
		// 5
		doit(mk("DF/DF"), mk("DF"), mk("DF"));
		assertRemoved("DF/DF");
		
	}

	public void testDirectoryFileConflicts_6() throws Exception {
		// 6
		setupCase(mk("DF/DF"), mk("DF"), mk("DF"));
		writeTrashFile("DF", "different");
		go();
		assertRemoved("DF/DF");
	}

	public void testDirectoryFileConflicts_7() throws Exception {
		// 7
		doit(mk("DF"), mk("DF"), mk("DF/DF"));
		assertUpdated("DF");
		assertRemoved("DF/DF");

		cleanUpDF();
		setupCase(mk("DF/DF"), mk("DF/DF"), mk("DF/DF/DF/DF/DF"));
		go();
		assertRemoved("DF/DF/DF/DF/DF");
		assertUpdated("DF/DF");
		
		cleanUpDF();
		setupCase(mk("DF/DF"), mk("DF/DF"), mk("DF/DF/DF/DF/DF"));
		writeTrashFile("DF/DF/DF/DF/DF", "diff");
		go();
		assertConflict("DF/DF/DF/DF/DF");
		assertUpdated("DF/DF");

	}

	// 8 ?

	public void testDirectoryFileConflicts_9() throws Exception {
		// 9
		doit(mk("DF"), mkmap("DF", "QP"), mk("DF/DF"));
		assertRemoved("DF/DF");
		assertUpdated("DF");
	}

	public void testDirectoryFileConflicts_10() throws Exception {
		// 10
		cleanUpDF();
		doit(mk("DF"), mk("DF/DF"), mk("DF/DF"));
		assertNoConflicts();
		
	}

	public void testDirectoryFileConflicts_11() throws Exception {
		// 11
		doit(mk("DF"), mk("DF/DF"), mkmap("DF/DF", "asdf"));
		assertConflict("DF/DF");
	}

	public void testDirectoryFileConflicts_12() throws Exception {
		// 12
		cleanUpDF();
		doit(mk("DF"), mk("DF/DF"), mk("DF"));
		assertRemoved("DF");
		assertUpdated("DF/DF");
	}

	public void testDirectoryFileConflicts_13() throws Exception {
		// 13
		cleanUpDF();
		setupCase(mk("DF"), mk("DF/DF"), mk("DF"));
		writeTrashFile("DF", "asdfsdf");
		go();
		assertConflict("DF");
		assertUpdated("DF/DF");
	}

	public void testDirectoryFileConflicts_14() throws Exception {
		// 14
		cleanUpDF();
		doit(mk("DF"), mk("DF/DF"), mkmap("DF", "Foo"));
		assertConflict("DF");
		assertUpdated("DF/DF");
	}

	public void testDirectoryFileConflicts_15() throws Exception {
		// 15
		doit(mkmap(), mk("DF/DF"), mk("DF"));
		assertRemoved("DF");
		assertUpdated("DF/DF");
	}

	public void testDirectoryFileConflicts_15b() throws Exception {
		// 15, take 2, just to check multi-leveled
		doit(mkmap(), mk("DF/DF/DF/DF"), mk("DF"));
		assertRemoved("DF");
		assertUpdated("DF/DF/DF/DF");
	}

	public void testDirectoryFileConflicts_16() throws Exception {
		// 16
		cleanUpDF();
		doit(mkmap(), mk("DF"), mk("DF/DF/DF"));
		assertRemoved("DF/DF/DF");
		assertUpdated("DF");
	}

	public void testDirectoryFileConflicts_17() throws Exception {
		// 17
		cleanUpDF();
		setupCase(mkmap(), mk("DF"), mk("DF/DF/DF"));
		writeTrashFile("DF/DF/DF", "asdf");
		go();
		assertConflict("DF/DF/DF");
		assertUpdated("DF");
	}

	public void testDirectoryFileConflicts_18() throws Exception {
		// 18
		cleanUpDF();
		doit(mk("DF/DF"), mk("DF/DF/DF/DF"), null);
		assertRemoved("DF/DF");
		assertUpdated("DF/DF/DF/DF");
	}

	public void testDirectoryFileConflicts_19() throws Exception {
		// 19
		cleanUpDF();
		doit(mk("DF/DF/DF/DF"), mk("DF/DF/DF"), null);
		assertRemoved("DF/DF/DF/DF");
		assertUpdated("DF/DF/DF");
	}

	private void cleanUpDF() throws Exception {
		tearDown();
		setUp();
		recursiveDelete(new File(trash, "DF"));
	}
	
	private void assertConflict(String s) {
		assertTrue(theReadTree.conflicts.contains(s));
	}
	
	private void assertUpdated(String s) {
		assertTrue(theReadTree.updated.containsKey(s));
	}
	
	private void assertRemoved(String s) {
		assertTrue(theReadTree.removed.contains(s));
	}
	
	private void assertNoConflicts() {
		assertTrue(theReadTree.conflicts.isEmpty());
	}

	private void doit(HashMap<String, String> h, HashMap<String, String>m,
			HashMap<String, String> i) throws IOException {
		setupCase(h, m, i);
		go();
	}
	
	private static HashMap<String, String> mk(String a) {
		return mkmap(a, a);
	}
	
	private static HashMap<String, String> mkmap(String... args) {
		if ((args.length % 2) > 0) 
			throw new IllegalArgumentException("needs to be pairs");
		
		HashMap<String, String> map = new HashMap<String, String>();
		for (int i = 0; i < args.length; i += 2) {
			map.put(args[i], args[i+1]);
		}
		
		return map;
	}
	
	public void testUntrackedConflicts() throws IOException { 
		setupCase(null, mk("foo"), null);
		writeTrashFile("foo", "foo");
		go();
		
		assertConflict("foo");
		
		recursiveDelete(new File(trash, "foo"));
		setupCase(null, mk("foo"), null);
		writeTrashFile("foo/bar/baz", "");
		writeTrashFile("foo/blahblah", "");
		go();

		assertConflict("foo/bar/baz");
		assertConflict("foo/blahblah");
		
		recursiveDelete(new File(trash, "foo"));
		
		setupCase(mkmap("foo/bar", "", "foo/baz", ""), 
				mk("foo"), mkmap("foo/bar", "", "foo/baz", ""));
		assertTrue(new File(trash, "foo/bar").exists());
		go();
		
		assertNoConflicts();
	}

	public void testCloseNameConflictsX0() throws IOException {
		setupCase(mkmap("a/a", "a/a-c"), mkmap("a/a","a/a", "b.b/b.b","b.b/b.bs"), mkmap("a/a", "a/a-c") );
		checkout();
		go();
		assertNoConflicts();
	}

	public void testCloseNameConflicts1() throws IOException {
		setupCase(mkmap("a/a", "a/a-c"), mkmap("a/a","a/a", "a.a/a.a","a.a/a.a"), mkmap("a/a", "a/a-c") );
		checkout();
		go();
		assertNoConflicts();
	}

	private void checkout() throws IOException {
		theReadTree = new WorkDirCheckout(db, trash, theHead, theIndex, theMerge);
		theReadTree.checkout();
	}
	
	public void testCheckoutOutChanges() throws IOException {
		setupCase(mk("foo"), mk("foo/bar"), mk("foo"));
		checkout();
		
		assertFalse(new File(trash, "foo").isFile());
		assertTrue(new File(trash, "foo/bar").isFile());
		recursiveDelete(new File(trash, "foo"));

		setupCase(mk("foo/bar"), mk("foo"), mk("foo/bar"));
		checkout();
		
		assertFalse(new File(trash, "foo/bar").isFile());
		assertTrue(new File(trash, "foo").isFile());
		
		setupCase(mk("foo"), mkmap("foo", "qux"), mkmap("foo", "bar"));
		
		try {
			checkout();
			fail("did not throw exception");
		} catch (CheckoutConflictException e) {
			// should have thrown
		}
	}
#endif
    }
}
