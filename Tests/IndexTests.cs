/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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
    public class T0007_Index : RepositoryTestCase
    {
#if false
	static boolean canrungitstatus;
	static {
		try {
			canrungitstatus = system(new File("."),"git --version") == 0;
		} catch (IOException e) {
			System.out.println("Warning: cannot invoke native git to validate index");
		} catch (InterruptedException e) {
			e.printStackTrace();
		}
	}

	private static int system(File dir, String cmd) throws IOException,
			InterruptedException {
		final Process process = Runtime.getRuntime().exec(cmd, null, dir);
		new Thread() {
			public void run() {
				try {
					InputStream s = process.getErrorStream();
					for (int c = s.read(); c != -1; c = s.read()) {
						System.err.print((char) c);
					}
					s.close();
				} catch (IOException e1) {
					// TODO Auto-generated catch block
					e1.printStackTrace();
				}
			}
		}.start();
		final Thread t2 = new Thread() {
			public void run() {
				synchronized (this) {
					try {
						InputStream e = process.getInputStream();
						for (int c = e.read(); c != -1; c = e.read()) {
							System.out.print((char) c);
						}
						e.close();
					} catch (IOException e1) {
						// TODO Auto-generated catch block
						e1.printStackTrace();
					}
				}
			}
		};
		t2.start();
		process.getOutputStream().close();
		int ret = process.waitFor();
		synchronized (t2) {
			return ret;
		}
	}

	public void testCreateEmptyIndex() throws Exception {
		GitIndex index = new GitIndex(db);
		index.write();
// native git doesn't like an empty index
// assertEquals(0,system(trash,"git status"));

		GitIndex indexr = new GitIndex(db);
		indexr.read();
		assertEquals(0, indexr.getMembers().length);
	}

	public void testReadWithNoIndex() throws Exception {
		GitIndex index = new GitIndex(db);
		index.read();
		assertEquals(0, index.getMembers().length);
	}

	public void testCreateSimpleSortTestIndex() throws Exception {
		GitIndex index = new GitIndex(db);
		writeTrashFile("a/b", "data:a/b");
		writeTrashFile("a:b", "data:a:b");
		writeTrashFile("a.b", "data:a.b");
		index.add(trash, new File(trash, "a/b"));
		index.add(trash, new File(trash, "a:b"));
		index.add(trash, new File(trash, "a.b"));
		index.write();

		assertEquals("a/b", index.getEntry("a/b").getName());
		assertEquals("a:b", index.getEntry("a:b").getName());
		assertEquals("a.b", index.getEntry("a.b").getName());
		assertNull(index.getEntry("a*b"));

		// Repeat test for re-read index
		GitIndex indexr = new GitIndex(db);
		indexr.read();
		assertEquals("a/b", indexr.getEntry("a/b").getName());
		assertEquals("a:b", indexr.getEntry("a:b").getName());
		assertEquals("a.b", indexr.getEntry("a.b").getName());
		assertNull(indexr.getEntry("a*b"));

		if (canrungitstatus)
			assertEquals(0, system(trash, "git status"));
	}

	public void testUpdateSimpleSortTestIndex() throws Exception {
		GitIndex index = new GitIndex(db);
		writeTrashFile("a/b", "data:a/b");
		writeTrashFile("a:b", "data:a:b");
		writeTrashFile("a.b", "data:a.b");
		index.add(trash, new File(trash, "a/b"));
		index.add(trash, new File(trash, "a:b"));
		index.add(trash, new File(trash, "a.b"));
		writeTrashFile("a/b", "data:a/b modified");
		index.add(trash, new File(trash, "a/b"));
		index.write();
		if (canrungitstatus)
			assertEquals(0, system(trash, "git status"));
	}

	public void testWriteTree() throws Exception {
		GitIndex index = new GitIndex(db);
		writeTrashFile("a/b", "data:a/b");
		writeTrashFile("a:b", "data:a:b");
		writeTrashFile("a.b", "data:a.b");
		index.add(trash, new File(trash, "a/b"));
		index.add(trash, new File(trash, "a:b"));
		index.add(trash, new File(trash, "a.b"));
		index.write();

		ObjectId id = index.writeTree();
		assertEquals("c696abc3ab8e091c665f49d00eb8919690b3aec3", id.name());
		
		writeTrashFile("a/b", "data:a/b");
		index.add(trash, new File(trash, "a/b"));

		if (canrungitstatus)
			assertEquals(0, system(trash, "git status"));
	}

	public void testReadTree() throws Exception {
		// Prepare tree
		GitIndex index = new GitIndex(db);
		writeTrashFile("a/b", "data:a/b");
		writeTrashFile("a:b", "data:a:b");
		writeTrashFile("a.b", "data:a.b");
		index.add(trash, new File(trash, "a/b"));
		index.add(trash, new File(trash, "a:b"));
		index.add(trash, new File(trash, "a.b"));
		index.write();

		ObjectId id = index.writeTree();
		System.out.println("wrote id " + id);
		assertEquals("c696abc3ab8e091c665f49d00eb8919690b3aec3", id.name());
		GitIndex index2 = new GitIndex(db);

		index2.readTree(db.mapTree(ObjectId.fromString(
				"c696abc3ab8e091c665f49d00eb8919690b3aec3")));
		Entry[] members = index2.getMembers();
		assertEquals(3, members.length);
		assertEquals("a.b", members[0].getName());
		assertEquals("a/b", members[1].getName());
		assertEquals("a:b", members[2].getName());
		assertEquals(3, members.length);

		GitIndex indexr = new GitIndex(db);
		indexr.read();
		Entry[] membersr = indexr.getMembers();
		assertEquals(3, membersr.length);
		assertEquals("a.b", membersr[0].getName());
		assertEquals("a/b", membersr[1].getName());
		assertEquals("a:b", membersr[2].getName());
		assertEquals(3, membersr.length);

		if (canrungitstatus)
			assertEquals(0, system(trash, "git status"));
	}

	public void testReadTree2() throws Exception {
		// Prepare a larger tree to test some odd cases in tree writing
		GitIndex index = new GitIndex(db);
		File f1 = writeTrashFile("a/a/a/a", "data:a/a/a/a");
		File f2 = writeTrashFile("a/c/c", "data:a/c/c");
		File f3 = writeTrashFile("a/b", "data:a/b");
		File f4 = writeTrashFile("a:b", "data:a:b");
		File f5 = writeTrashFile("a/d", "data:a/d");
		File f6 = writeTrashFile("a.b", "data:a.b");
		index.add(trash, f1);
		index.add(trash, f2);
		index.add(trash, f3);
		index.add(trash, f4);
		index.add(trash, f5);
		index.add(trash, f6);
		index.write();
		ObjectId id = index.writeTree();
		System.out.println("wrote id " + id);
		assertEquals("ba78e065e2c261d4f7b8f42107588051e87e18e9", id.name());
		GitIndex index2 = new GitIndex(db);

		index2.readTree(db.mapTree(ObjectId.fromString(
				"ba78e065e2c261d4f7b8f42107588051e87e18e9")));
		Entry[] members = index2.getMembers();
		assertEquals(6, members.length);
		assertEquals("a.b", members[0].getName());
		assertEquals("a/a/a/a", members[1].getName());
		assertEquals("a/b", members[2].getName());
		assertEquals("a/c/c", members[3].getName());
		assertEquals("a/d", members[4].getName());
		assertEquals("a:b", members[5].getName());

		// reread and test
		GitIndex indexr = new GitIndex(db);
		indexr.read();
		Entry[] membersr = indexr.getMembers();
		assertEquals(6, membersr.length);
		assertEquals("a.b", membersr[0].getName());
		assertEquals("a/a/a/a", membersr[1].getName());
		assertEquals("a/b", membersr[2].getName());
		assertEquals("a/c/c", membersr[3].getName());
		assertEquals("a/d", membersr[4].getName());
		assertEquals("a:b", membersr[5].getName());
	}

	public void testDelete() throws Exception {
		GitIndex index = new GitIndex(db);
		writeTrashFile("a/b", "data:a/b");
		writeTrashFile("a:b", "data:a:b");
		writeTrashFile("a.b", "data:a.b");
		index.add(trash, new File(trash, "a/b"));
		index.add(trash, new File(trash, "a:b"));
		index.add(trash, new File(trash, "a.b"));
		index.write();
		index.writeTree();
		index.remove(trash, new File(trash, "a:b"));
		index.write();
		assertEquals("a.b", index.getMembers()[0].getName());
		assertEquals("a/b", index.getMembers()[1].getName());

		GitIndex indexr = new GitIndex(db);
		indexr.read();
		assertEquals("a.b", indexr.getMembers()[0].getName());
		assertEquals("a/b", indexr.getMembers()[1].getName());

		if (canrungitstatus)
			assertEquals(0, system(trash, "git status"));
	}

	public void testCheckout() throws Exception {
		// Prepare tree, remote it and checkout
		GitIndex index = new GitIndex(db);
		File aslashb = writeTrashFile("a/b", "data:a/b");
		File acolonb = writeTrashFile("a:b", "data:a:b");
		File adotb = writeTrashFile("a.b", "data:a.b");
		index.add(trash, aslashb);
		index.add(trash, acolonb);
		index.add(trash, adotb);
		index.write();
		index.writeTree();
		delete(aslashb);
		delete(acolonb);
		delete(adotb);
		delete(aslashb.getParentFile());

		GitIndex index2 = new GitIndex(db);
		assertEquals(0, index2.getMembers().length);

		index2.readTree(db.mapTree(ObjectId.fromString(
				"c696abc3ab8e091c665f49d00eb8919690b3aec3")));

		index2.checkout(trash);
		assertEquals("data:a/b", content(aslashb));
		assertEquals("data:a:b", content(acolonb));
		assertEquals("data:a.b", content(adotb));

		if (canrungitstatus)
			assertEquals(0, system(trash, "git status"));
	}

	public void test030_executeBit_coreModeTrue() throws IllegalArgumentException, IllegalAccessException, InvocationTargetException, Error, Exception {
		if (!FS.INSTANCE.supportsExecute()) {
			System.err.println("Test ignored since platform FS does not support the execute permission");
			return;
		}
		try {
			// coremode true is the default, typically set to false
			// by git init (but not jgit!)
			Method canExecute = File.class.getMethod("canExecute", (Class[])null);
			Method setExecute = File.class.getMethod("setExecutable", new Class[] { Boolean.TYPE });
			File execFile = writeTrashFile("exec","exec");
			if (!((Boolean)setExecute.invoke(execFile, new Object[] { Boolean.TRUE })).booleanValue())
				throw new Error("could not set execute bit on "+execFile.getAbsolutePath()+"for test");
			File nonexecFile = writeTrashFile("nonexec","nonexec");
			if (!((Boolean)setExecute.invoke(nonexecFile, new Object[] { Boolean.FALSE })).booleanValue())
				throw new Error("could not clear execute bit on "+nonexecFile.getAbsolutePath()+"for test");

			GitIndex index = new GitIndex(db);
			index.filemode = Boolean.TRUE; // TODO: we need a way to set this using config
			index.add(trash, execFile);
			index.add(trash, nonexecFile);
			Tree tree = db.mapTree(index.writeTree());
			assertEquals(FileMode.EXECUTABLE_FILE, tree.findBlobMember(execFile.getName()).getMode());
			assertEquals(FileMode.REGULAR_FILE, tree.findBlobMember(nonexecFile.getName()).getMode());

			index.write();

			if (!execFile.delete())
				throw new Error("Problem in test, cannot delete test file "+execFile.getAbsolutePath());
			if (!nonexecFile.delete())
				throw new Error("Problem in test, cannot delete test file "+nonexecFile.getAbsolutePath());
			GitIndex index2 = new GitIndex(db);
			index2.filemode = Boolean.TRUE; // TODO: we need a way to set this using config
			index2.read();
			index2.checkout(trash);
			assertTrue(((Boolean)canExecute.invoke(execFile,(Object[])null)).booleanValue());
			assertFalse(((Boolean)canExecute.invoke(nonexecFile,(Object[])null)).booleanValue());

			assertFalse(index2.getEntry(execFile.getName()).isModified(trash));
			assertFalse(index2.getEntry(nonexecFile.getName()).isModified(trash));

			if (!((Boolean)setExecute.invoke(execFile, new Object[] { Boolean.FALSE })).booleanValue())
				throw new Error("could not clear set execute bit on "+execFile.getAbsolutePath()+"for test");
			if (!((Boolean)setExecute.invoke(nonexecFile, new Object[] { Boolean.TRUE })).booleanValue())
				throw new Error("could set execute bit on "+nonexecFile.getAbsolutePath()+"for test");

			assertTrue(index2.getEntry(execFile.getName()).isModified(trash));
			assertTrue(index2.getEntry(nonexecFile.getName()).isModified(trash));

		} catch (NoSuchMethodException e) {
			System.err.println("Test ignored when running under JDK < 1.6");
			return;
		}
	}

	public void test031_executeBit_coreModeFalse() throws IllegalArgumentException, IllegalAccessException, InvocationTargetException, Error, Exception {
		if (!FS.INSTANCE.supportsExecute()) {
			System.err.println("Test ignored since platform FS does not support the execute permission");
			return;
		}
		try {
			// coremode true is the default, typically set to false
			// by git init (but not jgit!)
			Method canExecute = File.class.getMethod("canExecute", (Class[])null);
			Method setExecute = File.class.getMethod("setExecutable", new Class[] { Boolean.TYPE });
			File execFile = writeTrashFile("exec","exec");
			if (!((Boolean)setExecute.invoke(execFile, new Object[] { Boolean.TRUE })).booleanValue())
				throw new Error("could not set execute bit on "+execFile.getAbsolutePath()+"for test");
			File nonexecFile = writeTrashFile("nonexec","nonexec");
			if (!((Boolean)setExecute.invoke(nonexecFile, new Object[] { Boolean.FALSE })).booleanValue())
				throw new Error("could not clear execute bit on "+nonexecFile.getAbsolutePath()+"for test");

			GitIndex index = new GitIndex(db);
			index.filemode = Boolean.FALSE; // TODO: we need a way to set this using config
			index.add(trash, execFile);
			index.add(trash, nonexecFile);
			Tree tree = db.mapTree(index.writeTree());
			assertEquals(FileMode.REGULAR_FILE, tree.findBlobMember(execFile.getName()).getMode());
			assertEquals(FileMode.REGULAR_FILE, tree.findBlobMember(nonexecFile.getName()).getMode());

			index.write();

			if (!execFile.delete())
				throw new Error("Problem in test, cannot delete test file "+execFile.getAbsolutePath());
			if (!nonexecFile.delete())
				throw new Error("Problem in test, cannot delete test file "+nonexecFile.getAbsolutePath());
			GitIndex index2 = new GitIndex(db);
			index2.filemode = Boolean.FALSE; // TODO: we need a way to set this using config
			index2.read();
			index2.checkout(trash);
			assertFalse(((Boolean)canExecute.invoke(execFile,(Object[])null)).booleanValue());
			assertFalse(((Boolean)canExecute.invoke(nonexecFile,(Object[])null)).booleanValue());

			assertFalse(index2.getEntry(execFile.getName()).isModified(trash));
			assertFalse(index2.getEntry(nonexecFile.getName()).isModified(trash));

			if (!((Boolean)setExecute.invoke(execFile, new Object[] { Boolean.FALSE })).booleanValue())
				throw new Error("could not clear set execute bit on "+execFile.getAbsolutePath()+"for test");
			if (!((Boolean)setExecute.invoke(nonexecFile, new Object[] { Boolean.TRUE })).booleanValue())
				throw new Error("could set execute bit on "+nonexecFile.getAbsolutePath()+"for test");

			// no change since we ignore the execute bit
			assertFalse(index2.getEntry(execFile.getName()).isModified(trash));
			assertFalse(index2.getEntry(nonexecFile.getName()).isModified(trash));

		} catch (NoSuchMethodException e) {
			System.err.println("Test ignored when running under JDK < 1.6");
			return;
		}
	}

	private String content(File f) throws IOException {
		byte[] buf = new byte[(int) f.length()];
		FileInputStream is = new FileInputStream(f);
		try {
			int read = is.read(buf);
			assertEquals(f.length(), read);
			return new String(buf, 0);
		} finally {
			is.close();
		}
	}

	private void delete(File f) throws IOException {
		if (!f.delete())
			throw new IOException("Failed to delete f");
	}
#endif
    }

}
