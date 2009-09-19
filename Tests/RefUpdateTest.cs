/*
 * Copyright (C) 2008, Charles O'Farrell <charleso@charleso.org>
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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using GitSharp.RevWalk;
using NUnit.Framework;

namespace GitSharp.Tests
{
	[TestFixture]
	public class RefUpdateTest : RepositoryTestCase
	{
		private RefUpdate UpdateRef(string name)
		{
			RefUpdate refUpdate = db.UpdateRef(name);
			refUpdate.NewObjectId = db.Resolve(Constants.HEAD);
			return refUpdate;
		}

		private void Delete(RefUpdate refUpdate, RefUpdate.RefUpdateResult expected)
		{
			Delete(refUpdate, expected, true, true);
		}

		private void Delete(RefUpdate refUpdate, RefUpdate.RefUpdateResult expected, bool exists, bool removed)
		{
			Assert.AreEqual(exists, db.getRef(refUpdate.Name) != null);
			Assert.AreEqual(expected, refUpdate.Delete());
			Assert.AreEqual(!removed, db.getRef(refUpdate.Name) != null);
		}

		[Test]
		public void testNoCacheObjectIdSubclass()
		{
			const string newRef = "refs/heads/abc";
			RefUpdate ru = UpdateRef(newRef);
			var newid = new RevCommit(ru.NewObjectId);

			ru.NewObjectId = newid;
			RefUpdate.RefUpdateResult update = ru.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.New, update);
			Ref r = db.getRef(newRef);
			Assert.IsNotNull(r);
			Assert.AreEqual(newRef, r.Name);
			Assert.IsNotNull(r.ObjectId);
			Assert.AreNotSame(newid, r.ObjectId);
			Assert.AreSame(typeof(ObjectId), r.ObjectId.GetType());
			Assert.AreEqual(newid.Copy(), r.ObjectId);

			var reverseEntries1 = db.ReflogReader("refs/heads/abc").getReverseEntries();
			Assert.AreEqual(1, reverseEntries1.Count);
			var entry1 = reverseEntries1[0];
			Assert.AreEqual(ObjectId.ZeroId, entry1.getOldId());
			Assert.AreEqual(r.ObjectId, entry1.getNewId());
			Assert.AreEqual(new PersonIdent(db).ToString(), entry1.getWho().ToString());
			Assert.AreEqual(string.Empty, entry1.getComment());
			var reverseEntries2 = db.ReflogReader("HEAD").getReverseEntries();
			Assert.AreEqual(0, reverseEntries2.Count);
		}

		[Test]
		public void testNewNamespaceConflictWithLoosePrefixNameExists()
		{
			const string newRef = "refs/heads/z";
			RefUpdate ru = UpdateRef(newRef);
			var newid = new RevCommit(ru.NewObjectId);
			ru.NewObjectId = newid;
			var update = ru.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.New, update);

			// end setup
			const string newRef2 = "refs/heads/z/a";
			RefUpdate ru2 = UpdateRef(newRef2);
			var newid2 = new RevCommit(ru2.NewObjectId);

			ru.NewObjectId = newid2;
			var update2 = ru2.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.LockFailure, update2);
			Assert.AreEqual(1, db.ReflogReader("refs/heads/z").getReverseEntries().Count);
			Assert.AreEqual(0, db.ReflogReader("HEAD").getReverseEntries().Count);
		}

		[Test]
		public void testNewNamespaceConflictWithPackedPrefixNameExists()
		{
			const string newRef = "refs/heads/master/x";
			RefUpdate ru = UpdateRef(newRef);
			var newid = new RevCommit(ru.NewObjectId);

			ru.NewObjectId = newid;
			RefUpdate.RefUpdateResult update = ru.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.LockFailure, update);
			Assert.IsNotNull(db.ReflogReader("refs/heads/master/x"));
			Assert.AreEqual(0, db.ReflogReader("HEAD").getReverseEntries().Count);
		}

		[Test]
		public void testNewNamespaceConflictWithLoosePrefixOfExisting()
		{
			const string newRef = "refs/heads/z/a";
			RefUpdate ru = UpdateRef(newRef);
			var newid = new RevCommit(ru.NewObjectId);

			ru.NewObjectId = newid;
			RefUpdate.RefUpdateResult update = ru.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.New, update);

			// end setup
			const string newRef2 = "refs/heads/z";
			RefUpdate ru2 = UpdateRef(newRef2);
			var newid2 = new RevCommit(ru2.NewObjectId);

			ru.NewObjectId = newid2;
			RefUpdate.RefUpdateResult update2 = ru2.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.LockFailure, update2);
			Assert.AreEqual(1, db.ReflogReader("refs/heads/z/a").getReverseEntries().Count);
			Assert.IsNotNull(db.ReflogReader("refs/heads/z"));
			Assert.AreEqual(0, db.ReflogReader("HEAD").getReverseEntries().Count);
		}

		[Test]
		public void testNewNamespaceConflictWithPackedPrefixOfExisting()
		{
			const string newRef = "refs/heads/prefix";
			RefUpdate ru = UpdateRef(newRef);
			var newid = new RevCommit(ru.NewObjectId);

			ru.NewObjectId = newid;
			RefUpdate.RefUpdateResult update = ru.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.LockFailure, update);
			Assert.IsNotNull(db.ReflogReader("refs/heads/prefix"));
			Assert.AreEqual(0, db.ReflogReader("HEAD").getReverseEntries().Count);
		}

		///	<summary>
		/// Delete a ref that is pointed to by HEAD
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testDeleteHEADreferencedRef()
		{
			ObjectId pid = db.Resolve("refs/heads/master^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = pid;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, update); // internal

			RefUpdate updateRef2 = db.UpdateRef("refs/heads/master");
			RefUpdate.RefUpdateResult delete = updateRef2.Delete();
			Assert.AreEqual(RefUpdate.RefUpdateResult.RejectedCurrentBranch, delete);
			Assert.AreEqual(pid, db.Resolve("refs/heads/master"));
			Assert.AreEqual(RefUpdate.RefUpdateResult.RejectedCurrentBranch, delete);
			Assert.AreEqual(pid, db.Resolve("refs/heads/master"));
		}

		[Test]
		public void testLooseDelete()
		{
			const string newRef = "refs/heads/abc";
			RefUpdate @ref = UpdateRef(newRef);
			@ref.Update(); // Create loose ref
			@ref = UpdateRef(newRef); // refresh
			Delete(@ref, RefUpdate.RefUpdateResult.NoChange);
			Assert.IsNotNull(db.ReflogReader("refs/heads/abc"));
		}

		[Test]
		public void testDeleteHead()
		{
			RefUpdate @ref = UpdateRef(Constants.HEAD);
			Delete(@ref, RefUpdate.RefUpdateResult.RejectedCurrentBranch, true, false);
			Assert.AreEqual(0, db.ReflogReader("refs/heads/master").getReverseEntries().Count);
			Assert.AreEqual(0, db.ReflogReader("HEAD").getReverseEntries().Count);
		}

		///	<summary>
		/// Delete a loose ref and make sure the directory in refs is deleted too,
		///	and the reflog dir too
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testDeleteLooseAndItsDirectory()
		{
			ObjectId pid = db.Resolve("refs/heads/c^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/z/c");
			updateRef.NewObjectId = pid;
			updateRef.IsForceUpdate = true;
			updateRef.SetRefLogMessage("new test ref", false);
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.New, update); // internal
			Assert.IsTrue(new DirectoryInfo(Path.Combine(db.Directory.FullName, Constants.R_HEADS + "z")).Exists);
			Assert.IsTrue(new DirectoryInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/z")).Exists);

			// The real test here
			RefUpdate updateRef2 = db.UpdateRef("refs/heads/z/c");
			updateRef2.IsForceUpdate = true;
			RefUpdate.RefUpdateResult delete = updateRef2.Delete();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, delete);
			Assert.IsNotNull(db.Resolve("refs/heads/z/c"));
			Assert.IsFalse(new DirectoryInfo(Path.Combine(db.Directory.FullName, Constants.R_HEADS + "z")).Exists);
			Assert.IsFalse(new DirectoryInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/z")).Exists);
		}

		[Test]
		public void testDeleteNotFound()
		{
			RefUpdate @ref = UpdateRef("refs/heads/xyz");
			Delete(@ref, RefUpdate.RefUpdateResult.New, false, true);
		}

		[Test]
		public void testDeleteFastForward()
		{
			RefUpdate @ref = UpdateRef("refs/heads/a");
			Delete(@ref, RefUpdate.RefUpdateResult.FastForward);
		}

		[Test]
		public void testDeleteForce()
		{
			RefUpdate @ref = db.UpdateRef("refs/heads/b");
			@ref.NewObjectId = db.Resolve("refs/heads/a");
			Delete(@ref, RefUpdate.RefUpdateResult.Rejected, true, false);
			@ref.IsForceUpdate = true;
			Delete(@ref, RefUpdate.RefUpdateResult.Forced);
		}

		[Test]
		public void testRefKeySameAsOrigName()
		{
			foreach (var e in db.getAllRefs())
			{
				Assert.AreEqual(e.Key, e.Value.OriginalName);
			}
		}

		///	<summary>
		/// Try modify a ref forward, fast forward
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testUpdateRefForward()
		{
			ObjectId ppid = db.Resolve("refs/heads/master^");
			ObjectId pid = db.Resolve("refs/heads/master");

			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = ppid;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, update);
			Assert.AreEqual(ppid, db.Resolve("refs/heads/master"));

			// real test
			RefUpdate updateRef2 = db.UpdateRef("refs/heads/master");
			updateRef2.NewObjectId = pid;
			RefUpdate.RefUpdateResult update2 = updateRef2.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.FastForward, update2);
			Assert.AreEqual(pid, db.Resolve("refs/heads/master"));
		}

		///	<summary>
		/// Delete a ref that exists both as packed and loose. Make sure the ref
		///	cannot be resolved after delete.
		///	</summary>
		///	<exception cref="IOException"></exception>
		[Test]
		public void testDeleteLoosePacked()
		{
			ObjectId pid = db.Resolve("refs/heads/c^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/c");
			updateRef.NewObjectId = pid;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, update); // internal

			// The real test is here
			RefUpdate updateRef2 = db.UpdateRef("refs/heads/c");
			updateRef2.IsForceUpdate = true;
			RefUpdate.RefUpdateResult delete = updateRef2.Delete();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, delete);
			Assert.IsNotNull(db.Resolve("refs/heads/c"));
		}

		///	<summary>
		/// Try modify a ref to same
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testUpdateRefNoChange()
		{
			ObjectId pid = db.Resolve("refs/heads/master");
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = pid;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.NoChange, update);
			Assert.AreEqual(pid, db.Resolve("refs/heads/master"));
		}

		///	<summary>
		/// Test case originating from
		///	<a href="http://bugs.eclipse.org/285991">bug 285991</a>
		///	<para />
		///	Make sure the in memory cache is updated properly after
		///	update of symref. This one did not fail because the
		///	ref was packed due to implementation issues.
		///	</summary>
		[Test]
		public void testRefsCacheAfterUpdate()
		{
			// Do not use the defalt repo for this case.
			var allRefs = db.getAllRefs();
			ObjectId oldValue = db.Resolve("HEAD");
			ObjectId newValue = db.Resolve("HEAD^");

			// first make HEAD refer to loose ref
			RefUpdate updateRef = db.UpdateRef(Constants.HEAD);
			updateRef.IsForceUpdate = true;
			updateRef.NewObjectId = newValue;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, update);

			// now update that ref
			updateRef = db.UpdateRef(Constants.HEAD);
			updateRef.IsForceUpdate = true;
			updateRef.NewObjectId = oldValue;
			update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.FastForward, update);
			allRefs = db.getAllRefs();
			Assert.AreEqual("refs/heads/master", allRefs["refs/heads/master"].Name);
			Assert.AreEqual("refs/heads/master", allRefs["refs/heads/master"].OriginalName);
			Assert.AreEqual("refs/heads/master", allRefs["HEAD"].Name);
			Assert.AreEqual("HEAD", allRefs["HEAD"].OriginalName);
		}

		/// <summary>
		/// Test case originating from
		/// <a href="http://bugs.eclipse.org/285991">bug 285991</a>
		/// <para />
		///	Make sure the in memory cache is updated properly after
		///	update of symref.
		///	</summary>
		[Test]
		public void testRefsCacheAfterUpdateLoosOnly()
		{
			// Do not use the defalt repo for this case.
			var allRefs = db.getAllRefs();
			ObjectId oldValue = db.Resolve("HEAD");
			db.WriteSymref(Constants.HEAD, "refs/heads/newref");
			RefUpdate updateRef = db.UpdateRef(Constants.HEAD);
			updateRef.IsForceUpdate = true;
			updateRef.NewObjectId = oldValue;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.New, update);
			allRefs = db.getAllRefs();
			Assert.AreEqual("refs/heads/newref", allRefs["HEAD"].Name);
			Assert.AreEqual("HEAD", allRefs["HEAD"].OriginalName);
			Assert.AreEqual("refs/heads/newref", allRefs["refs/heads/newref"].Name);
			Assert.AreEqual("refs/heads/newref", allRefs["refs/heads/newref"].OriginalName);
		}

		/// <summary>
		/// Try modify a ref, but get wrong expected old value
		/// </summary>
		/// <exception cref="IOException"> </exception>
		[Test]
		public void testUpdateRefLockFailureWrongOldValue()
		{
			ObjectId pid = db.Resolve("refs/heads/master");
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = pid;
			updateRef.ExpectedOldObjectId = db.Resolve("refs/heads/master^");
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.LockFailure, update);
			Assert.AreEqual(pid, db.Resolve("refs/heads/master"));
		}

		/// <summary>
		/// Try modify a ref that is locked
		/// </summary>
		/// <exception cref="IOException"> </exception>
		[Test]
		public void testUpdateRefLockFailureLocked()
		{
			ObjectId opid = db.Resolve("refs/heads/master");
			ObjectId pid = db.Resolve("refs/heads/master^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = pid;
			var lockFile1 = new LockFile(new FileInfo(Path.Combine(db.Directory.FullName, "refs/heads/master")));
			try
			{
				Assert.IsTrue(lockFile1.Lock()); // precondition to test
				RefUpdate.RefUpdateResult update = updateRef.Update();
				Assert.AreEqual(RefUpdate.RefUpdateResult.LockFailure, update);
				Assert.AreEqual(opid, db.Resolve("refs/heads/master"));
				var lockFile2 = new LockFile(new FileInfo(Path.Combine(db.Directory.FullName, "refs/heads/master")));
				Assert.IsFalse(lockFile2.Lock()); // was locked, still is
			}
			finally
			{
				lockFile1.Unlock();
			}
		}

		///	<summary>
		/// Try to delete a ref. Delete requires force.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testDeleteLoosePackedRejected()
		{
			ObjectId pid = db.Resolve("refs/heads/c^");
			ObjectId oldpid = db.Resolve("refs/heads/c");
			RefUpdate updateRef = db.UpdateRef("refs/heads/c");
			updateRef.NewObjectId = pid;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Rejected, update);
			Assert.AreEqual(oldpid, db.Resolve("refs/heads/c"));
		}

		[Test]
		public void testRenameBranchNoPreviousLog()
		{
			Assert.IsFalse(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/b")).Exists, "precondition, no log on old branchg");
			ObjectId rb = db.Resolve("refs/heads/b");
			ObjectId oldHead = db.Resolve(Constants.HEAD);
			Assert.IsFalse(rb.Equals(oldHead)); // assumption for this test
			RefRename renameRef = db.RenameRef("refs/heads/b", "refs/heads/new/name");
			RefUpdate.RefUpdateResult result = renameRef.Rename();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Renamed, result);
			Assert.AreEqual(rb, db.Resolve("refs/heads/new/name"));
			Assert.IsNotNull(db.Resolve("refs/heads/b"));
			Assert.AreEqual(1, db.ReflogReader("new/name").getReverseEntries().Count);
			Assert.AreEqual("Branch: renamed b to new/name", db.ReflogReader("new/name").getLastEntry().getComment());
			Assert.IsFalse(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/b")).Exists);
			Assert.AreEqual(oldHead, db.Resolve(Constants.HEAD)); // unchanged
		}

		[Test]
		public void testRenameBranchHasPreviousLog()
		{
			ObjectId rb = db.Resolve("refs/heads/b");
			ObjectId oldHead = db.Resolve(Constants.HEAD);
			Assert.IsFalse(rb.Equals(oldHead), "precondition for this test, branch b != HEAD");
			RefLogWriter.WriteReflog(db, rb, rb, "Just a message", "refs/heads/b");
			Assert.IsTrue(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/b")).Exists, "no log on old branch");
			RefRename renameRef = db.RenameRef("refs/heads/b", "refs/heads/new/name");
			RefUpdate.RefUpdateResult result = renameRef.Rename();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Renamed, result);
			Assert.AreEqual(rb, db.Resolve("refs/heads/new/name"));
			Assert.IsNotNull(db.Resolve("refs/heads/b"));
			Assert.AreEqual(2, db.ReflogReader("new/name").getReverseEntries().Count);
			Assert.AreEqual("Branch: renamed b to new/name", db.ReflogReader("new/name").getLastEntry().getComment());
			Assert.AreEqual("Just a message", db.ReflogReader("new/name").getReverseEntries()[1].getComment());
			Assert.IsFalse(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/b")).Exists);
			Assert.AreEqual(oldHead, db.Resolve(Constants.HEAD)); // unchanged
		}

		[Test]
		public void testRenameCurrentBranch()
		{
			ObjectId rb = db.Resolve("refs/heads/b");
			db.WriteSymref(Constants.HEAD, "refs/heads/b");
			ObjectId oldHead = db.Resolve(Constants.HEAD);
			Assert.IsTrue(rb.Equals(oldHead), "internal test condition, b == HEAD");
			RefLogWriter.WriteReflog(db, rb, rb, "Just a message", "refs/heads/b");
			Assert.IsTrue(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/b")).Exists, "no log on old branch");
			RefRename renameRef = db.RenameRef("refs/heads/b", "refs/heads/new/name");
			RefUpdate.RefUpdateResult result = renameRef.Rename();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Renamed, result);
			Assert.AreEqual(rb, db.Resolve("refs/heads/new/name"));
			Assert.IsNotNull(db.Resolve("refs/heads/b"));
			Assert.AreEqual("Branch: renamed b to new/name", db.ReflogReader("new/name").getLastEntry().getComment());
			Assert.IsFalse(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/b")).Exists);
			Assert.AreEqual(rb, db.Resolve(Constants.HEAD));
			Assert.AreEqual(2, db.ReflogReader("new/name").getReverseEntries().Count);
			Assert.AreEqual("Branch: renamed b to new/name", db.ReflogReader("new/name").getReverseEntries()[0].getComment());
			Assert.AreEqual("Just a message", db.ReflogReader("new/name").getReverseEntries()[1].getComment());
		}

		[Test]
		public void testRenameBranchAlsoInPack()
		{
			ObjectId rb = db.Resolve("refs/heads/b");
			ObjectId rb2 = db.Resolve("refs/heads/b~1");
			Assert.AreEqual(Ref.Storage.Packed, db.getRef("refs/heads/b").StorageFormat);
			RefUpdate updateRef = db.UpdateRef("refs/heads/b");
			updateRef.NewObjectId = rb2;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.IsTrue(RefUpdate.RefUpdateResult.Forced == update, "internal check new ref is loose");
			Assert.AreEqual(Ref.Storage.LoosePacked, db.getRef("refs/heads/b").StorageFormat);
			RefLogWriter.WriteReflog(db, rb, rb, "Just a message", "refs/heads/b");
			Assert.IsTrue(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/b")).Exists, "no log on old branch");
			RefRename renameRef = db.RenameRef("refs/heads/b", "refs/heads/new/name");
			RefUpdate.RefUpdateResult result = renameRef.Rename();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Renamed, result);
			Assert.AreEqual(rb2, db.Resolve("refs/heads/new/name"));
			Assert.IsNotNull(db.Resolve("refs/heads/b"));
			Assert.AreEqual("Branch: renamed b to new/name", db.ReflogReader("new/name").getLastEntry().getComment());
			Assert.AreEqual(3, db.ReflogReader("refs/heads/new/name").getReverseEntries().Count);
			Assert.AreEqual("Branch: renamed b to new/name", db.ReflogReader("refs/heads/new/name").getReverseEntries()[0].getComment());
			Assert.AreEqual(0, db.ReflogReader("HEAD").getReverseEntries().Count);

			// make sure b's log file is gone too.
			Assert.IsFalse(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/b")).Exists);

			// Create new Repository instance, to reread caches and make sure our
			// assumptions are persistent.
			var ndb = new Repository(db.Directory);
			Assert.AreEqual(rb2, ndb.Resolve("refs/heads/new/name"));
			Assert.IsNotNull(ndb.Resolve("refs/heads/b"));
		}

		public void tryRenameWhenLocked(string toLock, string fromName, string toName, string headPointsTo)
		{
			// Setup
			db.WriteSymref(Constants.HEAD, headPointsTo);
			ObjectId oldfromId = db.Resolve(fromName);
			ObjectId oldHeadId = db.Resolve(Constants.HEAD);
			RefLogWriter.WriteReflog(db, oldfromId, oldfromId, "Just a message", fromName);
			IList<ReflogReader.Entry> oldFromLog = db.ReflogReader(fromName).getReverseEntries();
			IList<ReflogReader.Entry> oldHeadLog = oldHeadId != null ? db.ReflogReader(Constants.HEAD).getReverseEntries() : null;

			Assert.IsTrue(new FileInfo(Path.Combine(db.Directory.FullName, "logs/" + fromName)).Exists, "internal check, we have a log");

			// "someone" has branch X locked
			var lockFile = new LockFile(new FileInfo(Path.Combine(db.Directory.FullName, toLock)));
			try
			{
				Assert.IsTrue(lockFile.Lock());

				// Now this is our test
				RefRename renameRef = db.RenameRef(fromName, toName);
				RefUpdate.RefUpdateResult result = renameRef.Rename();
				Assert.AreEqual(RefUpdate.RefUpdateResult.LockFailure, result);

				// Check that the involved refs are the same despite the failure
				AssertExists(false, toName);
				if (!toLock.Equals(toName))
				{
					AssertExists(false, toName + ".lock");
				}

				AssertExists(true, toLock + ".lock");
				if (!toLock.Equals(fromName))
				{
					AssertExists(false, "logs/" + fromName + ".lock");
				}

				AssertExists(false, "logs/" + toName + ".lock");
				Assert.AreEqual(oldHeadId, db.Resolve(Constants.HEAD));
				Assert.AreEqual(oldfromId, db.Resolve(fromName));
				Assert.IsNull(db.Resolve(toName));
				Assert.AreEqual(oldFromLog.ToString(), db.ReflogReader(fromName).getReverseEntries().ToString());
				if (oldHeadId != null)
				{
					Assert.AreEqual(oldHeadLog, db.ReflogReader(Constants.HEAD).getReverseEntries());
				}
			}
			finally
			{
				lockFile.Unlock();
			}
		}

		private void AssertExists(bool positive, string toName)
		{
			Assert.IsTrue(positive == new FileInfo(Path.Combine(db.Directory.FullName, toName)).Exists, toName + (positive ? " " : " does not ") + "exist");
		}

		[Test]
		public void testRenameBranchCannotLockAFileHEADisFromLockHEAD()
		{
			tryRenameWhenLocked("HEAD", "refs/heads/b", "refs/heads/new/name", "refs/heads/b");
		}

		[Test]
		public void testRenameBranchCannotLockAFileHEADisFromLockFrom()
		{
			tryRenameWhenLocked("refs/heads/b", "refs/heads/b", "refs/heads/new/name", "refs/heads/b");
		}

		[Test]
		public void testRenameBranchCannotLockAFileHEADisFromLockTo()
		{
			tryRenameWhenLocked("refs/heads/new/name", "refs/heads/b", "refs/heads/new/name", "refs/heads/b");
		}

		[Test]
		public void testRenameBranchCannotLockAFileHEADisToLockFrom()
		{
			tryRenameWhenLocked("refs/heads/b", "refs/heads/b", "refs/heads/new/name", "refs/heads/new/name");
		}

		[Test]
		public void testRenameBranchCannotLockAFileHEADisToLockTo()
		{
			tryRenameWhenLocked("refs/heads/new/name", "refs/heads/b", "refs/heads/new/name", "refs/heads/new/name");
		}

		[Test]
		public void testRenameBranchCannotLockAFileHEADisToLockTmp()
		{
			tryRenameWhenLocked("RENAMED-REF.." + Thread.CurrentThread.ManagedThreadId, "refs/heads/b", "refs/heads/new/name", "refs/heads/new/name");
		}

		[Test]
		public void testRenameBranchCannotLockAFileHEADisOtherLockFrom()
		{
			tryRenameWhenLocked("refs/heads/b", "refs/heads/b", "refs/heads/new/name", "refs/heads/a");
		}

		[Test]
		public void testRenameBranchCannotLockAFileHEADisOtherLockTo()
		{
			tryRenameWhenLocked("refs/heads/new/name", "refs/heads/b", "refs/heads/new/name", "refs/heads/a");
		}

		[Test]
		public void testRenameBranchCannotLockAFileHEADisOtherLockTmp()
		{
			tryRenameWhenLocked("RENAMED-REF.." + Thread.CurrentThread.ManagedThreadId, "refs/heads/b", "refs/heads/new/name", "refs/heads/a");
		}

		[Test]
		public void testRenameRefNameColission1avoided()
		{
			// Setup
			ObjectId rb = db.Resolve("refs/heads/b");
			db.WriteSymref(Constants.HEAD, "refs/heads/a");
			RefUpdate updateRef = db.UpdateRef("refs/heads/a");
			updateRef.NewObjectId = rb;
			updateRef.SetRefLogMessage("Setup", false);
			Assert.AreEqual(RefUpdate.RefUpdateResult.FastForward, updateRef.Update());
			ObjectId oldHead = db.Resolve(Constants.HEAD);
			Assert.IsTrue(rb.Equals(oldHead)); // assumption for this test
			RefLogWriter.WriteReflog(db, rb, rb, "Just a message", "refs/heads/a");
			Assert.IsTrue(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/a")).Exists, "internal check, we have a log");

			// Now this is our test
			RefRename renameRef = db.RenameRef("refs/heads/a", "refs/heads/a/b");
			RefUpdate.RefUpdateResult result = renameRef.Rename();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Renamed, result);
			Assert.IsNotNull(db.Resolve("refs/heads/a"));
			Assert.AreEqual(rb, db.Resolve("refs/heads/a/b"));
			Assert.AreEqual(3, db.ReflogReader("a/b").getReverseEntries().Count);
			Assert.AreEqual("Branch: renamed a to a/b", db.ReflogReader("a/b").getReverseEntries()[0].getComment());
			Assert.AreEqual("Just a message", db.ReflogReader("a/b").getReverseEntries()[1].getComment());
			Assert.AreEqual("Setup", db.ReflogReader("a/b").getReverseEntries()[2].getComment());

			// same thing was logged to HEAD
			Assert.AreEqual("Branch: renamed a to a/b", db.ReflogReader("HEAD").getReverseEntries()[0].getComment());
		}

		[Test]
		public void testRenameRefNameColission2avoided()
		{
			// setup
			ObjectId rb = db.Resolve("refs/heads/b");
			db.WriteSymref(Constants.HEAD, "refs/heads/prefix/a");
			RefUpdate updateRef = db.UpdateRef("refs/heads/prefix/a");
			updateRef.NewObjectId = rb;
			updateRef.SetRefLogMessage("Setup", false);
			updateRef.IsForceUpdate = true;
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, updateRef.Update());
			ObjectId oldHead = db.Resolve(Constants.HEAD);
			Assert.IsTrue(rb.Equals(oldHead)); // assumption for this test
			RefLogWriter.WriteReflog(db, rb, rb, "Just a message", "refs/heads/prefix/a");
			Assert.IsTrue(new FileInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/prefix/a")).Exists, "internal check, we have a log");

			// Now this is our test
			RefRename renameRef = db.RenameRef("refs/heads/prefix/a", "refs/heads/prefix");
			RefUpdate.RefUpdateResult result = renameRef.Rename();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Renamed, result);

			Assert.IsNotNull(db.Resolve("refs/heads/prefix/a"));
			Assert.AreEqual(rb, db.Resolve("refs/heads/prefix"));
			Assert.AreEqual(3, db.ReflogReader("prefix").getReverseEntries().Count);
			Assert.AreEqual("Branch: renamed prefix/a to prefix", db.ReflogReader("prefix").getReverseEntries()[0].getComment());
			Assert.AreEqual("Just a message", db.ReflogReader("prefix").getReverseEntries()[1].getComment());
			Assert.AreEqual("Setup", db.ReflogReader("prefix").getReverseEntries()[2].getComment());
			Assert.AreEqual("Branch: renamed prefix/a to prefix", db.ReflogReader("HEAD").getReverseEntries()[0].getComment());
		}

		/// <summary>
		/// Try modify a ref forward, fast forward, checking old value first.
		/// </summary>
		[Test]
		public void testUpdateRefForwardWithCheck1()
		{
			ObjectId ppid = db.Resolve("refs/heads/master^");
			ObjectId pid = db.Resolve("refs/heads/master");

			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = ppid;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, update);
			Assert.AreEqual(ppid, db.Resolve("refs/heads/master"));

			// real test
			RefUpdate updateRef2 = db.UpdateRef("refs/heads/master");
			updateRef2.ExpectedOldObjectId = ppid;
			updateRef2.NewObjectId = pid;
			RefUpdate.RefUpdateResult update2 = updateRef2.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.FastForward, update2);
			Assert.AreEqual(pid, db.Resolve("refs/heads/master"));
		}

		/// <summary>
		/// Try modify a ref forward, fast forward, checking old commit first
		/// </summary>
		[Test]
		public void testUpdateRefForwardWithCheck2()
		{
			ObjectId ppid = db.Resolve("refs/heads/master^");
			ObjectId pid = db.Resolve("refs/heads/master");

			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = ppid;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, update);
			Assert.AreEqual(ppid, db.Resolve("refs/heads/master"));

			// real test
			RevCommit old = new GitSharp.RevWalk.RevWalk(db).parseCommit(ppid);
			RefUpdate updateRef2 = db.UpdateRef("refs/heads/master");
			updateRef2.ExpectedOldObjectId = old;
			updateRef2.NewObjectId = pid;
			RefUpdate.RefUpdateResult update2 = updateRef2.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.FastForward, update2);
			Assert.AreEqual(pid, db.Resolve("refs/heads/master"));
		}
	}
}