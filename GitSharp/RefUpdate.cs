/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System;
using System.IO;
using GitSharp.RevWalk;
using GitSharp.Exceptions;

namespace GitSharp
{
	public class RefUpdate
	{
		public enum RefUpdateResult
		{
			/// <summary>
			/// The ref update/Delete has not been attempted by the caller.
			/// </summary>
			NotAttempted,

			/// <summary>
			/// The ref could not be locked for update/Delete.
			/// This is generally a transient failure and is usually caused by
			/// another process trying to access the ref at the same time as this
			/// process was trying to update it. It is possible a future operation
			/// will be successful.
			/// </summary>
			/// 

			LockFailure,
			/// <summary>
			/// Same value already stored.
			/// 
			/// Both the old value and the new value are identical. No change was
			/// necessary for an update. For Delete the branch is removed.
			/// </summary>
			NoChange,

			/// <summary>
			/// The ref was created locally for an update, but ignored for Delete.
			/// <para>
			/// The ref did not exist when the update started, but it was created
			/// successfully with the new value.
			/// </para>
			/// </summary>
			New,

			/// <summary>
			/// The ref had to be forcefully updated/deleted.
			/// <para>
			/// The ref already existed but its old value was not fully merged into
			/// the new value. The configuration permitted a forced update to take
			/// place, so ref now contains the new value. History associated with the
			/// objects not merged may no longer be reachable.
			/// </para>
			/// </summary>
			Forced,

			/// <summary>
			/// The ref was updated/deleted in a fast-forward way.
			/// <para>
			/// The tracking ref already existed and its old value was fully merged
			/// into the new value. No history was made unreachable.
			/// </para>
			/// </summary>
			FastForward,

			/// <summary>
			/// Not a fast-forward and not stored.
			/// <para>
			/// The tracking ref already existed but its old value was not fully
			/// merged into the new value. The configuration did not allow a forced
			/// update/Delete to take place, so ref still contains the old value. No
			/// previous history was lost.
			/// </para>
			/// </summary>
			Rejected,

			/// <summary>
			/// Rejected because trying to Delete the current branch.
			/// <para>
			/// Has no meaning for update.
			/// </para>
			/// </summary>
			RejectedCurrentBranch,

			/// <summary>
			/// The ref was probably not updated/deleted because of I/O error.
			/// <para>
			/// Unexpected I/O error occurred when writing new ref. Such error may
			/// result in uncertain state, but most probably ref was not updated.
			/// </para><para>
			/// This kind of error doesn't include <see cref="LockFailure"/>, 
			/// which is a different case.
			/// </para>
			/// </summary>
			IOFailure,

			/// <summary>
			/// The ref was renamed from another name
			/// </summary>
			Renamed
		}

		// Repository the ref is stored in.
		private readonly RefDatabase _db;

		// Location of the loose file holding the value of this ref.
		private readonly FileInfo _looseFile;

		private readonly Ref _ref;

		// New value the caller wants this ref to have.
		private ObjectId _newValue;

		// Message the caller wants included in the reflog.
		private string _refLogMessage;

		// Should the Result value be appended to the log message.
		private bool _refLogIncludeResult;

		// If non-null, the value that the old object id must have to continue.
		private ObjectId _expValue;


		public RefUpdate(RefDatabase refDb, Ref r, FileInfo f)
		{
			_db = refDb;
			_ref = r;
			OldObjectId = r.ObjectId;
			_looseFile = f;
			Result = RefUpdateResult.NotAttempted;
		}

		/// <summary>
		/// Gets the repository the updated ref resides in
		/// </summary>
		public Repository Repository
		{
			get { return _db.Repository; }
		}

		/// <summary>
		/// Gets the name of the ref this update will operate on.
		/// </summary>
		public string Name
		{
			get { return _ref.Name; }
		}

		/// <summary>
		/// Gets the new value the ref will be (or was) updated to.
		/// </summary>
		public ObjectId NewObjectId
		{
			get { return _newValue; }
			set { _newValue = value.Copy(); }
		}

		/// <summary>
		/// Gets the expected value of the ref after the lock is taken, but before
		/// update occurs. Null to avoid the compare and swap test. Use
		/// <see cref="ObjectId.ZeroId"/> to indicate expectation of a
		/// non-existant ref.
		/// </summary>
		public ObjectId ExpectedOldObjectId
		{
			get { return _expValue; }
			set { _expValue = value != null ? value.ToObjectId() : null; }
		}

		/// <summary>
		/// If this update wants to forcefully change the ref.
		/// </summary>
		public bool IsForceUpdate { get; set; }

		/// <summary>
		/// Gets the identity of the user making the change in the reflog.
		/// </summary>
		public PersonIdent RefLogIdent { get; set; }

		/// <summary>
		/// The old value of the ref, prior to the update being attempted.
		/// <para>
		/// This value may differ before and after the update method. Initially it is
		/// populated with the value of the ref before the lock is taken, but the old
		/// value may change if someone else modified the ref between the time we
		/// last read it and when the ref was locked for update.
		/// </para>
		/// </summary>
		public ObjectId OldObjectId { get; private set; }

		/// <summary>
		/// Gets the status of this update.
		/// </summary>
		public RefUpdateResult Result { get; private set; }

		public string GetRefLogMessage()
		{
			return _refLogMessage;
		}

		public void SetRefLogMessage(string msg, bool appendStatus)
		{
			if (string.IsNullOrEmpty(msg))
			{
				if (appendStatus)
				{
					_refLogMessage = string.Empty;
					_refLogIncludeResult = true;
				}
				else
				{
					DisableRefLog();
				}

			}
			else
			{
				_refLogMessage = msg;
				_refLogIncludeResult = appendStatus;
			}
		}

		private void RequireCanDoUpdate()
		{
			if (_newValue == null)
			{
				throw new InvalidOperationException("A NewObjectId is required.");
			}
		}

		public void DisableRefLog()
		{
			_refLogMessage = null;
			_refLogIncludeResult = false;
		}

		public RefUpdateResult ForceUpdate()
		{
			IsForceUpdate = true;
			return Update();
		}

		/// <summary>
		/// Gracefully update the ref to the new value.
		/// <para>
		/// Merge test will be performed according to {@link #isForceUpdate()}.
		/// </para><para>
		/// This is the same as:
		/// <example>
		/// return Update(new RevWalk(repository));
		/// </example>
		/// </summary>
		/// <returns>the result status of the update.</returns>
		public RefUpdateResult Update()
		{
			return Update(new RevWalk.RevWalk(_db.Repository));
		}

		/// <summary>
		/// Gracefully update the ref to the new value.
		/// </summary>
		/// <param name="walk">
		/// A <see cref="RevWalk"/> instance this update command can borrow to 
		/// perform the merge test. The walk will be reset to perform the test.
		/// </param>
		/// <returns>The result status of the update.</returns>
		public RefUpdateResult Update(RevWalk.RevWalk walk)
		{
			RequireCanDoUpdate();
			try
			{
				return Result = UpdateImpl(walk, new UpdateStore(this));
			}
			catch (IOException)
			{
				Result = RefUpdateResult.IOFailure;
				throw;
			}
		}

		private RefUpdateResult UpdateImpl(RevWalk.RevWalk walk, StoreBase store)
		{
			int lastSlash = Name.LastIndexOf('/');
			if (lastSlash > 0)
			{
				if (Repository.getAllRefs().ContainsKey(Name.Substring(0, lastSlash)))
				{
					return RefUpdateResult.LockFailure;
				}
			}

			string rName = Name + "/";
			foreach (Ref r in Repository.getAllRefs().Values)
			{
				if (r.Name.StartsWith(rName))
				{
					return RefUpdateResult.LockFailure;
				}
			}

			var @lock = new LockFile(_looseFile);
			if (!@lock.Lock())
			{
				return RefUpdateResult.LockFailure;
			}

			try
			{
				OldObjectId = _db.IdOf(Name);
				if (_expValue != null)
				{
					ObjectId o = OldObjectId ?? ObjectId.ZeroId;
					if (!_expValue.Equals(o))
					{
						return RefUpdateResult.LockFailure;
					}
				}

				if (OldObjectId == null)
				{
					return store.Store(@lock, RefUpdateResult.New);
				}

				RevObject newObj = SafeParse(walk, _newValue);
				RevObject oldObj = SafeParse(walk, OldObjectId);
				if (newObj == oldObj)
				{
					return store.Store(@lock, RefUpdateResult.NoChange);
				}

				if (newObj is RevCommit && oldObj is RevCommit)
				{
					if (walk.isMergedInto((RevCommit)oldObj, (RevCommit)newObj))
					{
						return store.Store(@lock, RefUpdateResult.FastForward);
					}
				}

				if (IsForceUpdate)
				{
					return store.Store(@lock, RefUpdateResult.Forced);
				}

				return RefUpdateResult.Rejected;
			}
			finally
			{
				@lock.Unlock();
			}
		}

		/// <summary>
		/// Delete the ref.
		/// <para>
		/// This is the same as:
		/// <example>
		/// return Delete(new RevWalk(repository));
		/// </example>
		/// </summary>
		/// <returns>The result status of the Delete.</returns>
		public RefUpdateResult Delete()
		{
			return Delete(new RevWalk.RevWalk(_db.Repository));
		}

		/// <summary>
		/// Delete the ref.
		/// </summary>
		/// <param name="walk">
		/// A <see cref="RevWalk"/> instance this Delete command can borrow to 
		/// perform the merge test. The walk will be reset to perform the test.
		/// </param>
		/// <returns>The result status of the Delete.</returns>
		public RefUpdateResult Delete(RevWalk.RevWalk walk)
		{
			if (Name.StartsWith(Constants.R_HEADS))
			{
				Ref head = _db.ReadRef(Constants.HEAD);
				if (head != null && Name.Equals(head.Name))
				{
					return Result = RefUpdateResult.RejectedCurrentBranch;
				}
			}

			try
			{
				return Result = UpdateImpl(walk, new DeleteStore(this));
			}
			catch (IOException)
			{
				Result = RefUpdateResult.IOFailure;
				throw;
			}
		}

		private static RevObject SafeParse(RevWalk.RevWalk rw, AnyObjectId id)
		{
			try
			{
				return id != null ? rw.parseAny(id) : null;
			}
			catch (MissingObjectException)
			{
				// We can expect some objects to be missing, like if we are
				// trying to force a deletion of a branch and the object it
				// points to has been pruned from the database due to freak
				// corruption accidents (it happens with 'git new-work-dir').
				//
				return null;
			}
		}

		private RefUpdateResult UpdateRepositoryStore(LockFile @lock, RefUpdateResult status)
		{
			if (status == RefUpdateResult.NoChange) return status;

			@lock.NeedStatInformation = true;
			@lock.Write(_newValue);

			string msg = GetRefLogMessage();

			if (!string.IsNullOrEmpty(msg))
			{
				if (_refLogIncludeResult)
				{
					String strResult = ToResultString(status);
					if (strResult != null)
					{
						msg = !string.IsNullOrEmpty(msg) ? msg + ": " + strResult : strResult;
					}
				}
				RefLogWriter.append(this, msg);
			}

			if (!@lock.Commit())
			{
				return RefUpdateResult.LockFailure;
			}

			_db.Stored(_ref.OriginalName, _ref.Name, _newValue, @lock.CommitLastModified);
			return status;
		}

		private static string ToResultString(RefUpdateResult status)
		{
			switch (status)
			{
				case RefUpdateResult.Forced:
					return "forced-update";

				case RefUpdateResult.FastForward:
					return "fast forward";

				case RefUpdateResult.New:
					return "created";

				default:
					return null;
			}
		}

		internal static int Count(string s, char c)
		{
			int count = 0;
			for (int p = s.IndexOf(c); p >= 0; p = s.IndexOf(c, p + 1))
			{
				count++;
			}
			return count;
		}

		#region Nested Types

		private abstract class StoreBase
		{
			private readonly RefUpdate _refUpdate;

			protected RefUpdate RefUpdate
			{
				get { return _refUpdate; }
			}

			protected StoreBase(RefUpdate refUpdate)
			{
				_refUpdate = refUpdate;
			}

			public abstract RefUpdateResult Store(LockFile lockFile, RefUpdateResult status);
		}

		private class UpdateStore : StoreBase
		{
			public UpdateStore(RefUpdate refUpdate) : base(refUpdate) { }

			public override RefUpdateResult Store(LockFile lockFile, RefUpdateResult status)
			{
				return RefUpdate.UpdateRepositoryStore(lockFile, status);
			}
		}

		private class DeleteStore : StoreBase
		{
			public DeleteStore(RefUpdate refUpdate) : base(refUpdate) { }

			public override RefUpdateResult Store(LockFile @lock, RefUpdateResult status)
			{
				var storage = RefUpdate._ref.StorageFormat;
				if (storage == Ref.Storage.New)
				{
					return status;
				}

				if (storage.IsPacked)
				{
					RefUpdate._db.RemovePackedRef(RefUpdate._ref.Name);
				}

				int levels = Count(RefUpdate._ref.Name, '/') - 2;

				// Delete logs _before_ unlocking
				DirectoryInfo gitDir = RefUpdate._db.Repository.Directory;
				var logDir = new DirectoryInfo(gitDir + "/" + Constants.LOGS);
				DeleteFileAndEmptyDir(new FileInfo(logDir + "/" + RefUpdate._ref.Name), levels);

				// We have to unlock before (maybe) deleting the parent directories
				@lock.Unlock();
				if (storage.IsLoose)
				{
					DeleteFileAndEmptyDir(RefUpdate._looseFile, levels);
				}
				return status;
			}

			private static void DeleteFileAndEmptyDir(FileInfo file, int depth)
			{
				if (!file.Exists) return;

				file.Delete();
				file.Refresh();
				if (file.Exists)
				{
					throw new IOException("File cannot be deleted: " + file);
				}
				DeleteEmptyDir(file.Directory, depth);
			}
		}

		#endregion

		internal static void DeleteEmptyDir(DirectoryInfo dir, int depth)
		{
			for (; depth > 0 && dir != null; depth--)
			{
				dir.Delete();
				if (dir.Exists) break;
				dir = dir.Parent;
			}
		}
	}
}