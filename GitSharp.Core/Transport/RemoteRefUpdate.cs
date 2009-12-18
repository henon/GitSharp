/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
 * - Neither the remoteName of the Git Development Community nor the
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

namespace GitSharp.Core.Transport
{

    public class RemoteRefUpdate
    {
		[Serializable]
        public enum UpdateStatus
        {
            NOT_ATTEMPTED,
            UP_TO_DATE,
            REJECTED_NONFASTFORWARD,
            REJECTED_NODELETE,
            REJECTED_REMOTE_CHANGED,
            REJECTED_OTHER_REASON,
            NON_EXISTING,
            AWAITING_REPORT,
            OK
        }

        public UpdateStatus Status { get; set; }
        public ObjectId ExpectedOldObjectId { get; private set; }
        public ObjectId NewObjectId { get; private set; }
        public string RemoteName { get; private set; }
        public TrackingRefUpdate TrackingRefUpdate { get; private set; }
        public string SourceRef { get; private set; }
        public bool ForceUpdate { get; private set; }
        public bool FastForward { get; set; }
        public string Message { get; set; }
        private Repository _localDb;

        public RemoteRefUpdate(Repository localDb, string srcRef, string remoteName, bool forceUpdate, string localName, ObjectId expectedOldObjectId)
        {
			if (localDb == null)
				throw new ArgumentNullException ("localDb");
            if (remoteName == null)
                throw new ArgumentException("Remote name can't be null.");

            SourceRef = srcRef;
            NewObjectId = (srcRef == null ? ObjectId.ZeroId : localDb.Resolve(srcRef));
            if (NewObjectId == null)
            {
                throw new IOException("Source ref " + srcRef + " doesn't resolve to any object.");
            }
            RemoteName = remoteName;
            ForceUpdate = forceUpdate;
            if (localName != null && localDb != null)
            {
                TrackingRefUpdate = new TrackingRefUpdate(localDb, localName, remoteName, true, NewObjectId, "push");
            }
            else
            {
                TrackingRefUpdate = null;
            }
            this._localDb = localDb;
            ExpectedOldObjectId = expectedOldObjectId;
            Status = UpdateStatus.NOT_ATTEMPTED;
        }

        public RemoteRefUpdate(RemoteRefUpdate baseUpdate, ObjectId newExpectedOldObjectId)
            : this(baseUpdate._localDb, baseUpdate.SourceRef, baseUpdate.RemoteName, baseUpdate.ForceUpdate, (baseUpdate.TrackingRefUpdate == null ? null : baseUpdate.TrackingRefUpdate.LocalName), newExpectedOldObjectId)
        {
			if (baseUpdate == null)
				throw new ArgumentNullException ("baseUpdate");
        }

        public bool IsExpectingOldObjectId
        {
            get
            {
                return ExpectedOldObjectId != null;
            }
        }

        public bool IsDelete
        {
            get
            {
                return ObjectId.ZeroId.Equals(NewObjectId);
            }
        }

        public bool HasTrackingRefUpdate
        {
            get
            {
                return TrackingRefUpdate != null;
            }
        }

        protected internal void updateTrackingRef(RevWalk.RevWalk walk)
        {
            if (IsDelete)
                TrackingRefUpdate.Delete(walk);
            else
                TrackingRefUpdate.Update(walk);
        }

        public override string ToString()
        {
            return "RemoteRefUpdate[remoteName=" + RemoteName + ", " + Status
                   + ", " + (ExpectedOldObjectId != null ? ExpectedOldObjectId.Abbreviate(_localDb).name() : "(null)")
                   + "..." + (NewObjectId != null ? NewObjectId.Abbreviate(_localDb).name() : "(null)")
                   + (FastForward ? ", fastForward" : string.Empty)
                   + ", srcRef=" + SourceRef + (ForceUpdate ? ", forceUpdate" : string.Empty) + ", message=" +
                   (Message != null
                        ? "\""
                          + Message + "\""
                        : "null") + ", " + _localDb.Directory + "]";
        }
    }
}