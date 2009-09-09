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
using Xunit;

namespace GitSharp.Tests
{
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
			Assert.Equal(exists, db.getRef(@ref.Name) != null);
			Assert.Equal(expected, @ref.Delete());
			Assert.Equal(!removed, db.getRef(@ref.Name) != null);
		}

		[Fact]
		public virtual void testDeleteFastForward()
		{
			RefUpdate @ref = updateRef("refs/heads/a");
			delete(@ref, RefUpdate.RefUpdateResult.FastForward);
		}

		[Fact]
		public void testDeleteForce()
		{
			RefUpdate @ref = db.UpdateRef("refs/heads/b");
			@ref.NewObjectId = (db.Resolve("refs/heads/a"));
			delete(@ref, RefUpdate.RefUpdateResult.Rejected, true, false);
			@ref.IsForceUpdate = (true);
			delete(@ref, RefUpdate.RefUpdateResult.Forced);
		}

		[Fact]
		public virtual void testDeleteHead()
		{
			RefUpdate @ref = updateRef(Constants.HEAD);
			delete(@ref, RefUpdate.RefUpdateResult.RejectedCurrentBranch, true, false);
		}

		///	<summary>
		/// Delete a ref that is pointed to by HEAD
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Fact]
		public void testDeleteHEADreferencedRef()
		{
			ObjectId pid = db.Resolve("refs/heads/master^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = pid;
			updateRef.IsForceUpdate = true;
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.Forced, update); // internal

			RefUpdate updateRef2 = db.UpdateRef("refs/heads/master");
			RefUpdate.RefUpdateResult delete = updateRef2.Delete();
			Assert.Equal(RefUpdate.RefUpdateResult.RejectedCurrentBranch, delete);
			Assert.Equal(pid, db.Resolve("refs/heads/master"));
		}

		///	<summary>
		/// Delete a loose ref and make sure the directory in refs is deleted too,
		///	and the reflog dir too
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Fact]
		public void testDeleteLooseAndItsDirectory()
		{
			ObjectId pid = db.Resolve("refs/heads/c^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/z/c");
			updateRef.NewObjectId = (pid);
			updateRef.IsForceUpdate = (true);
      updateRef.SetRefLogMessage("new test ref", false);
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.New, update); // internal
			Assert.True(new DirectoryInfo(Path.Combine(db.Directory.FullName, Constants.R_HEADS + "z")).Exists);
			Assert.True(new DirectoryInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/z")).Exists);

			// The real test here
			RefUpdate updateRef2 = db.UpdateRef("refs/heads/z/c");
			updateRef2.IsForceUpdate = (true);
			RefUpdate.RefUpdateResult delete = updateRef2.Delete();
			Assert.Equal(RefUpdate.RefUpdateResult.Forced, delete);
			Assert.Null(db.Resolve("refs/heads/z/c"));
      Assert.False(new DirectoryInfo(Path.Combine(db.Directory.FullName, Constants.R_HEADS + "z")).Exists);
      Assert.False(new DirectoryInfo(Path.Combine(db.Directory.FullName, "logs/refs/heads/z")).Exists);
		}

		///	<summary>
		/// Delete a ref that exists both as packed and loose. Make sure the ref
		///	cannot be resolved After delete.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Fact]
		public void testDeleteLoosePacked()
		{
			ObjectId pid = db.Resolve("refs/heads/c^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/c");
			updateRef.NewObjectId = (pid);
			updateRef.IsForceUpdate = (true);
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.Forced, update); // internal

			// The real test here
			RefUpdate updateRef2 = db.UpdateRef("refs/heads/c");
			updateRef2.IsForceUpdate = true;
			RefUpdate.RefUpdateResult delete = updateRef2.Delete();
			Assert.Equal(RefUpdate.RefUpdateResult.Forced, delete);
			Assert.Null(db.Resolve("refs/heads/c"));
		}

		///	<summary>
		/// Try to delete a ref. Delete requires force.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Fact]
		public void testDeleteLoosePackedRejected()
		{
			ObjectId pid = db.Resolve("refs/heads/c^");
			ObjectId oldpid = db.Resolve("refs/heads/c");
			RefUpdate updateRef = db.UpdateRef("refs/heads/c");
			updateRef.NewObjectId = (pid);
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.Rejected, update);
			Assert.Equal(oldpid, db.Resolve("refs/heads/c"));
		}

		[Fact]
		public void testDeleteNotFound()
		{
			RefUpdate @ref = updateRef("refs/heads/xyz");
			delete(@ref, RefUpdate.RefUpdateResult.New, false, true);
		}

		[Fact]
		public void testLooseDelete()
		{
			const string newRef = "refs/heads/abc";
			RefUpdate @ref = updateRef(newRef);
			@ref.Update(); // Create loose ref
			@ref = updateRef(newRef); // refresh
			delete(@ref, RefUpdate.RefUpdateResult.NoChange);
		}

		[Fact]
		public void testNoCacheObjectIdSubclass()
		{
			const string newRef = "refs/heads/abc";
			RefUpdate ru = updateRef(newRef);
			var newid = new RevCommit(ru.NewObjectId);
			ru.NewObjectId = newid;
			RefUpdate.RefUpdateResult update = ru.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.New, update);
		    Ref r = db.getRef(newRef);
			Assert.NotNull(r);
			Assert.Equal(newRef, r.Name);
			Assert.NotNull(r.ObjectId);
			Assert.NotSame(newid, r.ObjectId);
			Assert.Same(typeof (ObjectId), r.ObjectId.GetType());
			Assert.Equal(newid.Copy(), r.ObjectId);
		}

		[Fact]
		public virtual void testRefKeySameAsOrigName()
		{
			foreach (var e in db.getAllRefs())
			{
				Assert.Equal(e.Key, e.Value.OriginalName);
			}
		}

		///	<summary>
		/// Try modify a ref forward, fast forward
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Fact]
		public void testUpdateRefForward()
		{
			ObjectId ppid = db.Resolve("refs/heads/master^");
			ObjectId pid = db.Resolve("refs/heads/master");

			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = (ppid);
			updateRef.IsForceUpdate = (true);
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.Forced, update);
			Assert.Equal(ppid, db.Resolve("refs/heads/master"));

			// real test
			RefUpdate updateRef2 = db.UpdateRef("refs/heads/master");
			updateRef2.NewObjectId = (pid);
			RefUpdate.RefUpdateResult update2 = updateRef2.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.FastForward, update2);
			Assert.Equal(pid, db.Resolve("refs/heads/master"));
		}

		/// <summary>
		/// Try modify a ref that is locked
		/// </summary>
		/// <exception cref="IOException"> </exception>
		[Fact]
		public void testUpdateRefLockFailureLocked()
		{
			ObjectId opid = db.Resolve("refs/heads/master");
			ObjectId pid = db.Resolve("refs/heads/master^");
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = (pid);
			var lockFile1 = new LockFile(new FileInfo(Path.Combine(db.Directory.FullName, "refs/heads/master")));
			Assert.True(lockFile1.Lock()); // precondition to test
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.LockFailure, update);
			Assert.Equal(opid, db.Resolve("refs/heads/master"));
			var lockFile2 = new LockFile(new FileInfo(Path.Combine(db.Directory.FullName, "refs/heads/master")));
			Assert.False(lockFile2.Lock()); // was locked, still is
		}

		/// <summary>
		/// Try modify a ref, but get wrong expected old value
		/// </summary>
		/// <exception cref="IOException"> </exception>
		[Fact]
		public void testUpdateRefLockFailureWrongOldValue()
		{
			ObjectId pid = db.Resolve("refs/heads/master");
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = (pid);
			updateRef.ExpectedOldObjectId = db.Resolve("refs/heads/master^");
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.LockFailure, update);
			Assert.Equal(pid, db.Resolve("refs/heads/master"));
		}

		///	<summary>
		/// Try modify a ref to same
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Fact]
		public void testUpdateRefNoChange()
		{
			ObjectId pid = db.Resolve("refs/heads/master");
			RefUpdate updateRef = db.UpdateRef("refs/heads/master");
			updateRef.NewObjectId = (pid);
			RefUpdate.RefUpdateResult update = updateRef.Update();
			Assert.Equal(RefUpdate.RefUpdateResult.NoChange, update);
			Assert.Equal(pid, db.Resolve("refs/heads/master"));
		}
	}
}