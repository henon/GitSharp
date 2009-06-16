/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

namespace Gitty.Core
{
    public class RefUpdate
    {
        public enum RefUpdateResult
        {
            /// <summary>
            /// The ref update/delete has not been attempted by the caller.
            /// </summary>
            NotAttempted,
            /// <summary>
            /// The ref could not be locked for update/delete.
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
            /// necessary for an update. For delete the branch is removed.
            /// </summary>
            NoChange,
            /// <summary>
            /// The ref was created locally for an update, but ignored for delete.
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
            /// update/delete to take place, so ref still contains the old value. No
            /// previous history was lost.
            /// </summary>
            Rejected,
            /// <summary>
            /// Rejected because trying to delete the current branch.
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
        private String refLogMessage;

        /** Should the Result value be appended to {@link #refLogMessage}. */
        private bool refLogIncludeResult;

        /** If non-null, the value {@link #oldValue} must have to continue. */
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
         * @throws IOException
         *             an unexpected IO error occurred while writing changes.
         */
        public RefUpdateResult Update()
        {
            throw new NotImplementedException();
            //return Update(new RevWalk(db.Repository));
        }


        public RefUpdateResult Delete()
        {
            throw new NotImplementedException();
        }

#if false
        /**
         * Gracefully update the ref to the new value.
         * <p>
         * Merge test will be performed according to {@link #isForceUpdate()}.
         * 
         * @param walk
         *            a RevWalk instance this update command can borrow to perform
         *            the merge test. The walk will be reset to perform the test.
         * @return the result status of the update.
         * @throws IOException
         *             an unexpected IO error occurred while writing changes.
         */
        public RefUpdateResult update(RevWalk walk)
        {
            RequireCanDoUpdate();
            try
            {
                return result = updateImpl(walk, new UpdateStore());
            }
            catch (IOException x)
            {
                result = RefUpdateResult.IOFailure;
                throw;
            }
        }

        private RefUpdateResult updateImpl(RevWalk walk, Store store)
        {
            LockFile @lock;
            RevObject newObj;
            RevObject oldObj;

            @lock = new LockFile(looseFile);
            if (!@lock.Lock())
                return RefUpdateResult.LockFailure;
            try
            {
                oldValue = db.IdOf(Name);
                if (expValue != null)
                {
                    ObjectId o;
                    o = oldValue != null ? oldValue : ObjectId.ZeroId;
                    if (!expValue.equals(o))
                        return RefUpdateResult.LockFailure;
                }
                if (oldValue == null)
                    return store.store(@lock, RefUpdateResult.New);

                newObj = safeParse(walk, newValue);
                oldObj = safeParse(walk, oldValue);
                if (newObj == oldObj)
                    return store.store(@lock, RefUpdateResult.NoChange);

                if (newObj is RevCommit && oldObj is RevCommit)
                {
                    if (walk.isMergedInto((RevCommit)oldObj, (RevCommit)newObj))
                        return store.store(@lock, RefUpdateResult.FastForward);
                }

                if (isForceUpdate())
                    return store.store(@lock, RefUpdateResult.Forced);
                return RefUpdateResult.Rejected;
            }
            finally
            {
                @lock.Unlock();
            }
        }
#endif

        private abstract class StoreBase
        {
            public abstract RefUpdateResult Store(LockFile lockFile, RefUpdateResult status);
        }

        private class UpdateStore : StoreBase
        {
            public override RefUpdateResult Store(LockFile lockFile, RefUpdateResult status)
            {
                throw new NotImplementedException();
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