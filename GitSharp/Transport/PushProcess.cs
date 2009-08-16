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
using System.IO;
using GitSharp.Exceptions;
using GitSharp.RevWalk;

namespace GitSharp.Transport
{

    public class PushProcess
    {
        public const string PROGRESS_OPENING_CONNECTION = "Opening connection";

        private readonly Transport transport;
        private IPushConnection connection;
        private readonly Dictionary<string, RemoteRefUpdate> toPush;
        private readonly RevWalk.RevWalk walker;

        public PushProcess(Transport transport, List<RemoteRefUpdate> toPush)
        {
            walker = new RevWalk.RevWalk(transport.Local);
            this.transport = transport;
            foreach (RemoteRefUpdate rru in toPush)
            {
                if (this.toPush.ContainsKey(rru.RemoteName))
                {
                    throw new TransportException("Duplicate remote ref update is illegal. Affected remote name: " + rru.RemoteName);
                }
                else
                {
                    this.toPush.Add(rru.RemoteName, rru);
                }
            }
        }

        public PushResult execute(ProgressMonitor monitor)
        {
            monitor.BeginTask(PROGRESS_OPENING_CONNECTION, -1);
            connection = transport.openPush();
            try
            {
                monitor.EndTask();

                Dictionary<string, RemoteRefUpdate> preprocessed = prepareRemoteUpdates();
                if (transport.DryRun)
                    modifyUpdatesForDryRun();
                else if (preprocessed.Count != 0)
                    connection.Push(monitor, preprocessed);
            }
            finally
            {
                connection.Close();
            }
            if (!transport.DryRun)
                updateTrackingRefs();
            return prepareOperationResult();
        }

        private Dictionary<string, RemoteRefUpdate> prepareRemoteUpdates()
        {
            Dictionary<string, RemoteRefUpdate> result = new Dictionary<string, RemoteRefUpdate>();
            foreach (RemoteRefUpdate rru in toPush.Values)
            {
                Ref advertisedRef = connection.GetRef(rru.RemoteName);
                ObjectId advertisedOld = (advertisedRef == null ? ObjectId.ZeroId : advertisedRef.ObjectId);

                if (rru.NewObjectId.Equals(advertisedOld))
                {
                    if (rru.IsDelete)
                    {
                        rru.Status = RemoteRefUpdate.UpdateStatus.NON_EXISTING;
                    }
                    else
                    {
                        rru.Status = RemoteRefUpdate.UpdateStatus.UP_TO_DATE;
                    }

                    continue;
                }

                if (rru.IsExpectingOldObjectId && !rru.ExpectedOldObjectId.Equals(advertisedOld))
                {
                    rru.Status = RemoteRefUpdate.UpdateStatus.REJECTED_REMOTE_CHANGED;
                    continue;
                }

                if (advertisedOld.Equals(ObjectId.ZeroId) || rru.IsDelete)
                {
                    rru.FastForward = true;
                    result.Add(rru.RemoteName, rru);
                    continue;
                }

                bool fastForward = true;
                try
                {
                    RevObject oldRev = walker.parseAny(advertisedOld);
                    RevObject newRev = walker.parseAny(rru.NewObjectId);
                    if (!(oldRev is RevCommit) || !(newRev is RevCommit) ||
                        !walker.isMergedInto((RevCommit) oldRev, (RevCommit) newRev))
                        fastForward = false;
                }
                catch (MissingObjectException)
                {
                    fastForward = false;
                }
                catch (Exception e)
                {
                    throw new TransportException(transport.URI,
                                                 "reading objects from local repository failed: " + e.Message, e);
                }

                rru.FastForward = fastForward;
                if (!fastForward && !rru.ForceUpdate)
                    rru.Status = RemoteRefUpdate.UpdateStatus.REJECTED_NONFASTFORWARD;
                else
                {
                    result.Add(rru.RemoteName, rru);
                }
            }

            return result;
        }

        private void modifyUpdatesForDryRun()
        {
            foreach (RemoteRefUpdate rru in toPush.Values)
            {
                if (rru.Status == RemoteRefUpdate.UpdateStatus.NOT_ATTEMPTED)
                    rru.Status = RemoteRefUpdate.UpdateStatus.OK;
            }
        }

        private void updateTrackingRefs()
        {
            foreach (RemoteRefUpdate rru in toPush.Values)
            {
                RemoteRefUpdate.UpdateStatus status = rru.Status;
                if (rru.HasTrackingRefUpdate && (status == RemoteRefUpdate.UpdateStatus.UP_TO_DATE || status == RemoteRefUpdate.UpdateStatus.OK))
                {
                    try
                    {
                        rru.updateTrackingRef(walker);
                    }
                    catch (IOException)
                    {
                        
                    }
                }
            }
        }

        private PushResult prepareOperationResult()
        {
            PushResult result = new PushResult();
            result.SetAdvertisedRefs(transport.URI, connection.RefsMap);
            result.SetRemoteUpdates(toPush);

            foreach (RemoteRefUpdate rru in toPush.Values)
            {
                TrackingRefUpdate tru = rru.TrackingRefUpdate;
                if (tru != null)
                    result.Add(tru);
            }

            return result;
        }
    }

}