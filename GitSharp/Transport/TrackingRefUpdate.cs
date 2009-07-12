/*
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

namespace GitSharp.Transport
{

    public class TrackingRefUpdate
    {
        public string RemoteName { get; private set; }
        private readonly RefUpdate update;

        public TrackingRefUpdate(Repository db, RefSpec spec, AnyObjectId nv, string msg)
            : this(db, spec.Destination, spec.Source, spec.Force, nv, msg)
        {
        }

        public TrackingRefUpdate(Repository db, string localName, string remoteName, bool forceUpdate, AnyObjectId nv, string msg)
        {
            RemoteName = remoteName;
            update = db.UpdateRef(localName);
            update.IsForceUpdate = forceUpdate;
            update.NewObjectId = nv.Copy();
            update.SetRefLogMessage(msg, true);
        }

        public string LocalName
        {
            get
            {
                return update.Name;
            }
        }

        public ObjectId NewObjectId
        {
            get
            {
                return update.NewObjectId;
            }
        }

        public ObjectId OldObjectId
        {
            get
            {
                return update.OldObjectId;
            }
        }

        public RefUpdate.RefUpdateResult Result
        {
            get
            {
                return update.Result;
            }
        }

        public void Update(RevWalk.RevWalk walk)
        {
            update.Update(walk);
        }

        public void Delete(RevWalk.RevWalk walk)
        {
            update.Delete(walk);
        }
    }

}