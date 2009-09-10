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

using System.IO;
using GitSharp.RevWalk;
using NUnit.Framework;

namespace GitSharp.Tests
{
	[TestFixture]
	public class RefUpdateTest : RepositoryTestCase
	{
		private RefUpdate updateRef(string name)
		{
			RefUpdate @ref = db.UpdateRef(name);
			@ref.NewObjectId = db.Resolve(Constants.HEAD);
			return @ref;
		}

		private void delete(RefUpdate @ref, RefUpdate.RefUpdateResult expected)
		{
			delete(@ref, expected, true, true);
		}

		private void delete(RefUpdate @ref, RefUpdate.RefUpdateResult expected, bool exists, bool removed)
		{
			Assert.AreEqual(exists, db.getRef(@ref.Name) != null);
			Assert.AreEqual(expected, @ref.Delete());
			Assert.AreEqual(!removed, db.getRef(@ref.Name) != null);
		}

		[Test]
		public virtual void testDeleteFastForward()
		{
			RefUpdate @ref = updateRef("refs/heads/a");
			delete(@ref, RefUpdate.RefUpdateResult.FastForward);
		}

		[Test]
		public void testDeleteForce()
		{
			RefUpdate @ref = db.UpdateRef("refs/heads/b");
			@ref.NewObjectId = db.Resolve("refs/heads/a");
			delete(@ref, RefUpdate.RefUpdateResult.Rejected, true, false);
			@ref.IsForceUpdate = true;
			delete(@ref, RefUpdate.RefUpdateResult.Forced);
		}

		[Test]
		public virtual void testDeleteHead()
		{
			RefUpdate @ref = updateRef(Constants.HEAD);
			delete(@ref, RefUpdate.RefUpdateResult.RejectedCurrentBranch, true, false);
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
			Assert.IsNull(db.Resolve("refs/heads/z/c"));
			Assert.IsFalse(new DirectoryInfo(Path.Combine(db.Directory.FullName, Constants.R_HEADS + "z")).Exists);
			Assert.IsFalse(new DirectoryInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/z")).Exists);
		}

		///	<summary>
		/// Delete a ref that exists both as packed and loose. Make sure the ref
		///	cannot be resolved After delete.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testDeleteLoosePacked()
		{
			ObjectId pid = db.Resolve("refs/heads/c^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/c");
			updateRef.NewObjectId = pid;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, update); // internal

			// The real test here
			RefUpdate updateRef2 = db.UpdateRef("refs/heads/c");
			updateRef2.IsForceUpdate = true;
			RefUpdate.RefUpdateResult delete = updateRef2.Delete();
			Assert.AreEqual(RefUpdate.RefUpdateResult.Forced, delete);
			Assert.IsNull(db.Resolve("refs/heads/c"));
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
		public void testDeleteNotFound()
		{
			RefUpdate @ref = updateRef("refs/heads/xyz");
			delete(@ref, RefUpdate.RefUpdateResult.New, false, true);
		}

		[Test]
		public void testLooseDelete()
		{
			const string newRef = "refs/heads/abc";
			RefUpdate @ref = updateRef(newRef);
			@ref.Update(); // Create loose ref
			@ref = updateRef(newRef); // refresh
			delete(@ref, RefUpdate.RefUpdateResult.NoChange);
		}

		[Test]
		public void testNoCacheObjectIdSubclass()
		{
			const string newRef = "refs/heads/abc";
			RefUpdate ru = updateRef(newRef);
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
		}

		[Test]
		public virtual void testRefKeySameAsOrigName()
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
			Assert.IsTrue(lockFile1.Lock()); // precondition to test
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.AreEqual(RefUpdate.RefUpdateResult.LockFailure, update);
			Assert.AreEqual(opid, db.Resolve("refs/heads/master"));
			var lockFile2 = new LockFile(new FileInfo(Path.Combine(db.Directory.FullName, "refs/heads/master")));
			Assert.IsFalse(lockFile2.Lock()); // was locked, still is
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
	}
}