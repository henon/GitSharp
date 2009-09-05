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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GitSharp.Exceptions;
using NUnit.Framework;

namespace GitSharp.Tests
{
	[TestFixture]
	public class T0007_Index : RepositoryTestCase
	{
		private static bool CanRunGitStatus;

		static T0007_Index()
		{
			try
			{
				CanRunGitStatus = System(new DirectoryInfo("."), "git --version") == 0;
			}
			catch (IOException)
			{
				Console.WriteLine("Warning: cannot invoke native git to validate index");
			}
			catch (Exception e)
			{
				e.printStackTrace();
			}
		}

		private static string Content(FileSystemInfo f)
		{
			using (var sr = new StreamReader(f.FullName))
			{
				return sr.ReadToEnd();
			}
		}

		private static void Delete(FileSystemInfo f)
		{
			f.Delete();
		}

		private static int System(FileSystemInfo dir, string cmd)
		{
			int exitCode = -1;

			if (dir == null || !dir.Exists || !Directory.Exists(dir.FullName) || string.IsNullOrEmpty(cmd))
			{
				return exitCode;
			}

			var commandAndArgument = cmd.Split(new[] { ' ' });

			var psi = new ProcessStartInfo
						{
							FileName = commandAndArgument[0],
							Arguments = commandAndArgument.Length > 1 ? commandAndArgument[1] : null,
							WorkingDirectory = dir.FullName,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							UseShellExecute = false
						};

			var process = Process.Start(psi);
			if (process == null) return exitCode;

			process.WaitForExit();
			exitCode = process.ExitCode;

			var outputText = process.StandardOutput.ReadToEnd();
			var errorText = process.StandardError.ReadToEnd();

			if (!string.IsNullOrEmpty(outputText))
			{
				Console.WriteLine(outputText);
			}

			if (!string.IsNullOrEmpty(errorText))
			{
				Console.Error.WriteLine(errorText);
			}

			return exitCode;

			/*
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
			 */
		}

		[Test, Ignore("Not ported yet")]
		public void test030_executeBit_coreModeTrue()
		{
			/*
		if (!FS.INSTANCE.supportsExecute())
		{
			System.err.println("Test ignored since platform FS does not support the execute permission");
			return;
		}
		try
		{
			// coremode true is the default, typically set to false
			// by git init (but not jgit!)
			var canExecute = typeof(File).GetMethod("canExecute", (Class[])null);
			Method setExecute = typeof(File).getMethod("setExecutable", new Class[] { bool.TYPE });
			File execFile = writeTrashFile("exec","exec");
			if (!((bool)setExecute.invoke(execFile, new object[] { Convert.true })).booleanValue())
				throw new Error("could not set execute bit on "+execFile.getAbsolutePath()+"for test");
			File nonexecFile = writeTrashFile("nonexec","nonexec");
			if (!((bool)setExecute.invoke(nonexecFile, new object[] { Convert.false })).booleanValue())
				throw new Error("could not clear execute bit on "+nonexecFile.getAbsolutePath()+"for test");

			GitIndex index = new GitIndex(db);
			index.filemode = Convert.true; // TODO: we need a way to set this using config
			index.Add(trash, execFile);
			index.Add(trash, nonexecFile);
			Tree tree = db.mapTree(index.writeTree());
			Assert.AreEqual(FileMode.EXECUTABLE_FILE, tree.findBlobMember(execFile.Name).getMode());
			Assert.AreEqual(FileMode.REGULAR_FILE, tree.findBlobMember(nonexecFile.Name).getMode());

			index.write();

			if (!execFile.delete())
				throw new Error("Problem in test, cannot delete test file "+execFile.getAbsolutePath());
			if (!nonexecFile.delete())
				throw new Error("Problem in test, cannot delete test file "+nonexecFile.getAbsolutePath());
			GitIndex index2 = new GitIndex(db);
			index2.filemode = Convert.true; // TODO: we need a way to set this using config
			index2.read();
			index2.checkout(trash);
			assertTrue(((bool)canExecute.invoke(execFile,(object[])null)).booleanValue());
			assertFalse(((bool)canExecute.invoke(nonexecFile,(object[])null)).booleanValue());

			assertFalse(index2.getEntry(execFile.Name).isModified(trash));
			assertFalse(index2.getEntry(nonexecFile.Name).isModified(trash));

			if (!((bool)setExecute.invoke(execFile, new object[] { Convert.false })).booleanValue())
				throw new Error("could not clear set execute bit on "+execFile.getAbsolutePath()+"for test");
			if (!((bool)setExecute.invoke(nonexecFile, new object[] { Convert.true })).booleanValue())
				throw new Error("could set execute bit on "+nonexecFile.getAbsolutePath()+"for test");

			assertTrue(index2.getEntry(execFile.Name).isModified(trash));
			assertTrue(index2.getEntry(nonexecFile.Name).isModified(trash));

		}
		catch (NoSuchMethodException e)
		{
			System.err.println("Test ignored when running under JDK < 1.6");
			return;
		}
			*/
		}

		[Test, Ignore("Not ported yet")]
		public void test031_executeBit_coreModeFalse()
		{
			/*
		if (!FS.INSTANCE.supportsExecute())
		{
			System.err.println("Test ignored since platform FS does not support the execute permission");
			return;
		}
		try
		{
			// coremode true is the default, typically set to false
			// by git init (but not jgit!)
			Method canExecute = typeof(File).getMethod("canExecute", (Class[])null);
			Method setExecute = typeof(File).getMethod("setExecutable", new Class[] { bool.TYPE });
			File execFile = writeTrashFile("exec","exec");
			if (!((bool)setExecute.invoke(execFile, new object[] { Convert.true })).booleanValue())
				throw new Error("could not set execute bit on "+execFile.getAbsolutePath()+"for test");
			File nonexecFile = writeTrashFile("nonexec","nonexec");
			if (!((bool)setExecute.invoke(nonexecFile, new object[] { Convert.false })).booleanValue())
				throw new Error("could not clear execute bit on "+nonexecFile.getAbsolutePath()+"for test");

			GitIndex index = new GitIndex(db);
			index.filemode = Convert.false; // TODO: we need a way to set this using config
			index.Add(trash, execFile);
			index.Add(trash, nonexecFile);
			Tree tree = db.mapTree(index.writeTree());
			Assert.AreEqual(FileMode.REGULAR_FILE, tree.findBlobMember(execFile.Name).getMode());
			Assert.AreEqual(FileMode.REGULAR_FILE, tree.findBlobMember(nonexecFile.Name).getMode());

			index.write();

			if (!execFile.delete())
				throw new Error("Problem in test, cannot delete test file "+execFile.getAbsolutePath());
			if (!nonexecFile.delete())
				throw new Error("Problem in test, cannot delete test file "+nonexecFile.getAbsolutePath());
			GitIndex index2 = new GitIndex(db);
			index2.filemode = Convert.false; // TODO: we need a way to set this using config
			index2.read();
			index2.checkout(trash);
			assertFalse(((bool)canExecute.invoke(execFile,(object[])null)).booleanValue());
			assertFalse(((bool)canExecute.invoke(nonexecFile,(object[])null)).booleanValue());

			assertFalse(index2.getEntry(execFile.Name).isModified(trash));
			assertFalse(index2.getEntry(nonexecFile.Name).isModified(trash));

			if (!((bool)setExecute.invoke(execFile, new object[] { Convert.false })).booleanValue())
				throw new Error("could not clear set execute bit on "+execFile.getAbsolutePath()+"for test");
			if (!((bool)setExecute.invoke(nonexecFile, new object[] { Convert.true })).booleanValue())
				throw new Error("could set execute bit on "+nonexecFile.getAbsolutePath()+"for test");

			// no change since we ignore the execute bit
			assertFalse(index2.getEntry(execFile.Name).isModified(trash));
			assertFalse(index2.getEntry(nonexecFile.Name).isModified(trash));

		}
		catch (NoSuchMethodException e)
		{
			System.err.println("Test ignored when running under JDK < 1.6");
			return;
		}
			 * */
		}

		[Test]
		public void testCheckout()
		{
			// Prepare tree, remote it and checkout
			var index = new GitIndex(db);
			FileInfo aslashb = writeTrashFile("a/b", "data:a/b");
			FileInfo acolonb = writeTrashFile("a:b", "data:a:b");
			FileInfo adotb = writeTrashFile("a.b", "data:a.b");
			index.add(trash, aslashb);
			index.add(trash, acolonb);
			index.add(trash, adotb);
			index.write();
			index.writeTree();
			Delete(aslashb);
			Delete(acolonb);
			Delete(adotb);
			Delete(Directory.GetParent(aslashb.FullName));

			var index2 = new GitIndex(db);
			Assert.AreEqual(0, index2.Members.Length);

			index2.ReadTree(db.MapTree(ObjectId.FromString("c696abc3ab8e091c665f49d00eb8919690b3aec3")));

			index2.checkout(trash);
			Assert.AreEqual("data:a/b", Content(aslashb));
			Assert.AreEqual("data:a:b", Content(acolonb));
			Assert.AreEqual("data:a.b", Content(adotb));

			if (CanRunGitStatus)
			{
				Assert.AreEqual(0, System(trash, "git status"));
			}
		}

		[Test]
		public void testCreateEmptyIndex()
		{
			var index = new GitIndex(db);
			index.write();
			// native git doesn't like an empty index
			// Assert.AreEqual(0,System(trash,"git status"));

			var indexr = new GitIndex(db);
			indexr.Read();
			Assert.AreEqual(0, indexr.Members.Length);
		}

		[Test]
		public void testCreateSimpleSortTestIndex()
		{
			var index = new GitIndex(db);
			writeTrashFile("a/b", "data:a/b");
			writeTrashFile("a:b", "data:a:b");
			writeTrashFile("a.b", "data:a.b");
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a/b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a:b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a.b")));
			index.write();

			Assert.AreEqual("a/b", index.GetEntry("a/b").Name);
			Assert.AreEqual("a:b", index.GetEntry("a:b").Name);
			Assert.AreEqual("a.b", index.GetEntry("a.b").Name);
			Assert.IsNull(index.GetEntry("a*b"));

			// Repeat test for re-read index
			var indexr = new GitIndex(db);
			indexr.Read();
			Assert.AreEqual("a/b", indexr.GetEntry("a/b").Name);
			Assert.AreEqual("a:b", indexr.GetEntry("a:b").Name);
			Assert.AreEqual("a.b", indexr.GetEntry("a.b").Name);
			Assert.IsNull(indexr.GetEntry("a*b"));

			if (CanRunGitStatus)
			{
				Assert.AreEqual(0, System(trash, "git status"));
			}
		}

		[Test]
		public void testDelete()
		{
			var index = new GitIndex(db);
			writeTrashFile("a/b", "data:a/b");
			writeTrashFile("a:b", "data:a:b");
			writeTrashFile("a.b", "data:a.b");
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a/b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a:b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a.b")));
			index.write();
			index.writeTree();
			index.remove(trash, new FileInfo(Path.Combine(trash.FullName, "a:b")));
			index.write();
			Assert.AreEqual("a.b", index.Members[0].Name);
			Assert.AreEqual("a/b", index.Members[1].Name);

			var indexr = new GitIndex(db);
			indexr.Read();
			Assert.AreEqual("a.b", indexr.Members[0].Name);
			Assert.AreEqual("a/b", indexr.Members[1].Name);

			if (CanRunGitStatus)
			{
				Assert.AreEqual(0, System(trash, "git status"));
			}
		}

		[Test]
		public void testReadTree()
		{
			// Prepare tree
			var index = new GitIndex(db);
			writeTrashFile("a/b", "data:a/b");
			writeTrashFile("a:b", "data:a:b");
			writeTrashFile("a.b", "data:a.b");
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a/b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a:b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a.b")));
			index.write();

			ObjectId id = index.writeTree();
			Console.WriteLine("wrote id " + id);
			Assert.AreEqual("c696abc3ab8e091c665f49d00eb8919690b3aec3", id.Name);
			var index2 = new GitIndex(db);

			index2.ReadTree(db.MapTree(ObjectId.FromString("c696abc3ab8e091c665f49d00eb8919690b3aec3")));
			GitIndex.Entry[] members = index2.Members;
			Assert.AreEqual(3, members.Length);
			Assert.AreEqual("a.b", members[0].Name);
			Assert.AreEqual("a/b", members[1].Name);
			Assert.AreEqual("a:b", members[2].Name);
			Assert.AreEqual(3, members.Length);

			var indexr = new GitIndex(db);
			indexr.Read();
			GitIndex.Entry[] membersr = indexr.Members;
			Assert.AreEqual(3, membersr.Length);
			Assert.AreEqual("a.b", membersr[0].Name);
			Assert.AreEqual("a/b", membersr[1].Name);
			Assert.AreEqual("a:b", membersr[2].Name);
			Assert.AreEqual(3, membersr.Length);

			if (CanRunGitStatus)
			{
				Assert.AreEqual(0, System(trash, "git status"));
			}
		}

		[Test]
		public void testReadTree2()
		{
			// Prepare a larger tree to test some odd cases in tree writing
			var index = new GitIndex(db);
			FileInfo f1 = writeTrashFile("a/a/a/a", "data:a/a/a/a");
			FileInfo f2 = writeTrashFile("a/c/c", "data:a/c/c");
			FileInfo f3 = writeTrashFile("a/b", "data:a/b");
			FileInfo f4 = writeTrashFile("a:b", "data:a:b");
			FileInfo f5 = writeTrashFile("a/d", "data:a/d");
			FileInfo f6 = writeTrashFile("a.b", "data:a.b");
			index.add(trash, f1);
			index.add(trash, f2);
			index.add(trash, f3);
			index.add(trash, f4);
			index.add(trash, f5);
			index.add(trash, f6);
			index.write();
			ObjectId id = index.writeTree();
			Console.WriteLine("wrote id " + id);
			Assert.AreEqual("ba78e065e2c261d4f7b8f42107588051e87e18e9", id.Name);
			var index2 = new GitIndex(db);

			index2.ReadTree(db.MapTree(ObjectId.FromString("ba78e065e2c261d4f7b8f42107588051e87e18e9")));
			GitIndex.Entry[] members = index2.Members;
			Assert.AreEqual(6, members.Length);
			Assert.AreEqual("a.b", members[0].Name);
			Assert.AreEqual("a/a/a/a", members[1].Name);
			Assert.AreEqual("a/b", members[2].Name);
			Assert.AreEqual("a/c/c", members[3].Name);
			Assert.AreEqual("a/d", members[4].Name);
			Assert.AreEqual("a:b", members[5].Name);

			// reread and test
			var indexr = new GitIndex(db);
			indexr.Read();
			GitIndex.Entry[] membersr = indexr.Members;
			Assert.AreEqual(6, membersr.Length);
			Assert.AreEqual("a.b", membersr[0].Name);
			Assert.AreEqual("a/a/a/a", membersr[1].Name);
			Assert.AreEqual("a/b", membersr[2].Name);
			Assert.AreEqual("a/c/c", membersr[3].Name);
			Assert.AreEqual("a/d", membersr[4].Name);
			Assert.AreEqual("a:b", membersr[5].Name);
		}

		[Test]
		public void testReadWithNoIndex()
		{
			var index = new GitIndex(db);
			index.Read();
			Assert.AreEqual(0, index.Members.Length);
		}

		[Test]
		public void testUpdateSimpleSortTestIndex()
		{
			var index = new GitIndex(db);
			writeTrashFile("a/b", "data:a/b");
			writeTrashFile("a:b", "data:a:b");
			writeTrashFile("a.b", "data:a.b");
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a/b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a:b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a.b")));
			writeTrashFile("a/b", "data:a/b modified");
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a/b")));
			index.write();

			if (CanRunGitStatus)
			{
				Assert.AreEqual(0, System(trash, "git status"));
			}
		}

		[Test]
		public void testWriteTree()
		{
			var index = new GitIndex(db);
			writeTrashFile("a/b", "data:a/b");
			writeTrashFile("a:b", "data:a:b");
			writeTrashFile("a.b", "data:a.b");
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a/b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a:b")));
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a.b")));
			index.write();

			ObjectId id = index.writeTree();
			Assert.AreEqual("c696abc3ab8e091c665f49d00eb8919690b3aec3", id.Name);

			writeTrashFile("a/b", "data:a/b");
			index.add(trash, new FileInfo(Path.Combine(trash.FullName, "a/b")));

			if (CanRunGitStatus)
			{
				Assert.AreEqual(0, System(trash, "git status"));
			}
		}
	}
}