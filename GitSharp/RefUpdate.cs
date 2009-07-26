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
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            /// <p>
            /// The ref did not exist when the update started, but it was created
            /// successfully with the new value.
            /// </summary>
            New,
            /// <summary>
            /// The ref had to be forcefully updated/deleted.
            /// <p>
            /// The ref already existed but its old value was not fully merged into
            /// the new value. The configuration permitted a forced update to take
            /// place, so ref now contains the new value. History associated with the
            /// objects not merged may no longer be reachable.
            /// </summary>
            Forced,
            /// <summary>
            /// The ref was updated/deleted in a fast-forward way.
            /// <p>
            /// The tracking ref already existed and its old value was fully merged
            /// into the new value. No history was made unreachable.
            /// </summary>
            FastForward,
            /// <summary>
            /// Not a fast-forward and not stored.
            /// <p>
            /// The tracking ref already existed but its old value was not fully
            /// merged into the new value. The configuration did not allow a forced
            /// update/Delete to take place, so ref still contains the old value. No
            /// previous history was lost.
            /// </summary>
            Rejected,
            /// <summary>
            /// Rejected because trying to Delete the current branch.
            /// <p>
            /// Has no meaning for update.
            /// </summary>
            RejectedCurrentBranch,
            /// <summary>
            /// The ref was probably not updated/deleted because of I/O error.
            /// <p>
            /// Unexpected I/O error occurred when writing new ref. Such error may
            /// result in uncertain state, but most probably ref was not updated.
            /// <p>
            /// This kind of error doesn't include {@link #LOCK_FAILURE}, which is a
            /// different case.
            /// </summary>
            IOFailure,
        }

        /** Repository the ref is stored in. */
        private RefDatabase db;

        /** Location of the loose file holding the value of this ref. */
        private FileInfo looseFile;

        /** New value the caller wants this ref to have. */
        private ObjectId newValue;

        /** Message the caller wants included in the reflog. */
        private string refLogMessage;

        /** Should the Result value be appended to {@link #refLogMessage}. */
        private bool refLogIncludeResult;

        /** If non-null, the value {@link #OldObjectId} must have to continue. */
        private ObjectId expValue;

        private Ref _ref;

        public RefUpdate(RefDatabase refDb, Ref r, FileInfo f)
        {
            db = refDb;
            this._ref = r;
            this.OldObjectId = r.ObjectId;
            looseFile = f;
            this.Result = RefUpdateResult.NotAttempted;
        }

        public Repository Repository
        {
            get
            {
                return this.db.Repository;
            }
        }

        public string Name
        {
            get
            {
                return _ref.Name;
            }
        }

        public ObjectId NewObjectId
        {
            get
            {
                return newValue;
            }
            set
            {
                newValue = value.Copy();
            }
        }

        public ObjectId ExpectedOldObjectId
        {
            get
            {
                return expValue;
            }
            set
            {
                expValue = value != null ? value.ToObjectId() : null;
            }
        }

        public bool IsForceUpdate { get; set; }

        public PersonIdent RefLogIdent { get; set; }

        public ObjectId OldObjectId { get; private set; }

        public RefUpdateResult Result { get; private set; }


        public string GetRefLogMessage()
        {
            return refLogMessage;
        }

        public void SetRefLogMessage(string msg, bool appendStatus)
        {
            refLogMessage = msg;
            refLogIncludeResult = appendStatus;
        }

        private void RequireCanDoUpdate()
        {
            if (newValue == null)
                throw new InvalidOperationException("A NewObjectId is required.");
        }


        public RefUpdateResult ForceUpdate()
        {
            this.IsForceUpdate = true;
            return Update();
        }

        /**
         * Gracefully update the ref to the new value.
         * <p>
         * Merge test will be performed according to {@link #isForceUpdate()}.
         * <p>
         * This is the same as:
         * 
         * <pre>
         * return update(new RevWalk(repository));
         * </pre>
         * 
         * @return the result status of the update.
         * @
         *             an unexpected IO error occurred while writing changes.
         */
        public RefUpdateResult Update()
        {
            return update(new RevWalk.RevWalk(db.Repository));
        }

        public RefUpdateResult Update(RevWalk.RevWalk walk)
        {
            return update(walk);
        }

        /**
         * Gracefully update the ref to the new value.
         * <p>
         * Merge test will be performed according to {@link #isForceUpdate()}.
         * 
         * @param walk
         *            a RevWalk instance this update command can borrow to perform
         *            the merge test. The walk will be reset to perform the test.
         * @return the result status of the update.
         * @
         *             an unexpected IO error occurred while writing changes.
         */
        public RefUpdateResult update(RevWalk.RevWalk walk)
        {
            RequireCanDoUpdate();
            try
            {
                return Result = updateImpl(walk, new UpdateStore(this));
            }
            catch (IOException)
            {
                Result = RefUpdateResult.IOFailure;
                throw;
            }
        }

        private RefUpdateResult updateImpl(RevWalk.RevWalk walk, StoreBase store)
        {
            LockFile @lock;
            RevObject newObj;
            RevObject oldObj;

            @lock = new LockFile(looseFile);
            if (!@lock.Lock())
                return RefUpdateResult.LockFailure;
            try
            {
                OldObjectId = db.IdOf(Name);
                if (expValue != null)
                {
                    ObjectId o;
                    o = OldObjectId != null ? OldObjectId : ObjectId.ZeroId;
                    if (!expValue.Equals(o))
                        return RefUpdateResult.LockFailure;
                }
                if (OldObjectId == null)
                    return store.Store(@lock, RefUpdateResult.New);

                newObj = safeParse(walk, newValue);
                oldObj = safeParse(walk, OldObjectId);
                if (newObj == oldObj)
                    return store.Store(@lock, RefUpdateResult.NoChange);

                if (newObj is RevCommit && oldObj is RevCommit)
                {
                    if (walk.isMergedInto((RevCommit)oldObj, (RevCommit)newObj))
                        return store.Store(@lock, RefUpdateResult.FastForward);
                }

                if (IsForceUpdate)
                    return store.Store(@lock, RefUpdateResult.Forced);
                return RefUpdateResult.Rejected;
            }
            finally
            {
                @lock.Unlock();
            }
        }


        /**
         * Delete the ref.
         * <p>
         * This is the same as:
         * 
         * <pre>
         * return Delete(new RevWalk(repository));
         * </pre>
         * 
         * @return the result status of the Delete.
         * @
         */
        public RefUpdateResult Delete()
        {
            return Delete(new RevWalk.RevWalk(db.Repository));
        }

        /**
         * Delete the ref.
         * 
         * @param walk
         *            a RevWalk instance this Delete command can borrow to perform
         *            the merge test. The walk will be reset to perform the test.
         * @return the result status of the Delete.
         * @
         */
        public RefUpdateResult Delete(RevWalk.RevWalk walk)
        {
            if (Name.StartsWith(Constants.R_HEADS))
            {
                Ref head = db.ReadRef(Constants.HEAD);
                if (head != null && Name.Equals(head.Name))
                    return Result = RefUpdateResult.RejectedCurrentBranch;
            }

            try
            {
                return Result = updateImpl(walk, new DeleteStore(this));
            }
            catch (IOException)
            {
                Result = RefUpdateResult.IOFailure;
                throw;
            }
        }

        private static RevObject safeParse(RevWalk.RevWalk rw, AnyObjectId id)
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

        private RefUpdateResult updateStore(LockFile @lock, RefUpdateResult status)
        {
            if (status == RefUpdateResult.NoChange)
                return status;
            @lock.NeedStatInformation=(true);
            @lock.Write(newValue);
            string msg = GetRefLogMessage();
            if (msg != null && refLogIncludeResult)
            {
                if (status == RefUpdateResult.Forced)
                    msg += ": forced-update";
                else if (status == RefUpdateResult.FastForward)
                    msg += ": fast forward";
                else if (status == RefUpdateResult.New)
                    msg += ": created";
            }
            RefLogWriter.append(this, msg);
            if (!@lock.Commit())
                return RefUpdateResult.LockFailure;
            db.stored(this._ref.OriginalName, _ref.Name, newValue, @lock.CommitLastModified);
            return status;
        }

        private abstract class StoreBase
        {
            protected RefUpdate ref_update;

            public StoreBase(RefUpdate ref_update)
            {
                this.ref_update = ref_update;
            }

            public abstract RefUpdateResult Store(LockFile lockFile, RefUpdateResult status);
        }

        private class UpdateStore : StoreBase
        {
            public UpdateStore(RefUpdate ref_update) : base(ref_update) { }

            public override RefUpdateResult Store(LockFile lockFile, RefUpdateResult status)
            {
                return ref_update.updateStore(lockFile, status);
            }
        }

        private class DeleteStore : StoreBase
        {
            public DeleteStore(RefUpdate ref_update) : base(ref_update) { }

            public override RefUpdateResult Store(LockFile @lock, RefUpdateResult status)
            {
                GitSharp.Ref.Storage storage = ref_update._ref.StorageFormat;
                if (storage == GitSharp.Ref.Storage.New)
                    return status;
                if (storage.IsPacked)
                    ref_update.db.removePackedRef(ref_update._ref.Name);

                int levels = Count(ref_update._ref.Name, '/') - 2;

                // Delete logs _before_ unlocking
                DirectoryInfo gitDir = ref_update.db.Repository.Directory;
                DirectoryInfo logDir = new DirectoryInfo(gitDir+"/"+ Constants.LOGS);
                deleteFileAndEmptyDir(new FileInfo(logDir + "/" + ref_update._ref.Name), levels);

                // We have to unlock before (maybe) deleting the parent directories
                @lock.Unlock();
                if (storage.IsLoose)
                    deleteFileAndEmptyDir(ref_update.looseFile, levels);
                return status;
            }

            private void deleteFileAndEmptyDir(FileInfo file, int depth)
            {
                if (file.Exists)
                {
                    file.Delete();
                    if (file.Exists)
                        throw new IOException("File cannot be deleted: " + file);
                    deleteEmptyDir(file.Directory, depth);
                }
            }

            private void deleteEmptyDir(DirectoryInfo dir, int depth)
            {
                for (; depth > 0 && dir != null; depth--)
                {
                    dir.Delete();
                    if (dir.Exists)
                        break;
                    dir = dir.Parent;
                }
            }
        }

        private static int Count(string s, char c)
        {
            int count = 0;
            for (int p = s.IndexOf(c); p >= 0; p = s.IndexOf(c, p + 1))
            {
                count++;
            }
            return count;
        }
    }
}
