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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitSharp.Exceptions;
using GitSharp.RevWalk;
using GitSharp.RevWalk.Filter;

namespace GitSharp.Transport
{
    public class BasePackFetchConnection : BasePackConnection, IFetchConnection
    {
        private const int MAX_HAVES = 256;
        protected const int MIN_CLIENT_BUFFER = 2*32*46 + 8;
        public const string OPTION_INCLUDE_TAG = "include-tag";
        public const string OPTION_MULTI_ACK = "multi_ack";
        public const string OPTION_THIN_PACK = "thin-pack";
        public const string OPTION_SIDE_BAND = "side-band";
        public const string OPTION_SIDE_BAND_64K = "side-band-64k";
        public const string OPTION_OFS_DELTA = "ofs-delta";
        public const string OPTION_SHALLOW = "shallow";
        public const string OPTION_NO_PROGRESS = "no-progress";

        private RevWalk.RevWalk walk;
        private RevCommitList<RevCommit> reachableCommits;
        public readonly RevFlag REACHABLE;
        public readonly RevFlag COMMON;
        public readonly RevFlag ADVERTISED;
        private bool multiAck;
        private bool thinPack;
        private bool sideband;
        private bool includeTags;
        private bool allowOfsDelta;
        private string lockMessage;
        private PackLock packLock;

        public BasePackFetchConnection(IPackTransport packTransport) : base(packTransport)
        {
            RepositoryConfig cfg = local.Config;
            includeTags = transport.TagOpt != TagOpt.NO_TAGS;
            thinPack = transport.FetchThin;
            allowOfsDelta = cfg.GetBoolean("repack", "usedeltabaseoffset", true);

            walk = new RevWalk.RevWalk(local);
            reachableCommits = new RevCommitList<RevCommit>();
            REACHABLE = walk.newFlag("REACHABLE");
            COMMON = walk.newFlag("COMMON");
            ADVERTISED = walk.newFlag("ADVERTISED");

            walk.carry(COMMON);
            walk.carry(REACHABLE);
            walk.carry(ADVERTISED);
        }

        public void Fetch(ProgressMonitor monitor, List<Ref> want, List<ObjectId> have)
        {
            markStartedOperation();
            doFetch(monitor, want, have);
        }

        public bool DidFetchIncludeTags
        {
            get
            {
                return false;
            }
        }

        public bool DidFetchTestConnectivity
        {
            get
            {
                return false;
            }
        }

        public void SetPackLockMessage(string message)
        {
            lockMessage = message;
        }

        public List<PackLock> PackLocks
        {
            get
            {
                if (packLock != null)
                {
                    return new List<PackLock> {packLock};
                }
                return new List<PackLock>();
            }
        }

        protected void doFetch(ProgressMonitor monitor, List<Ref> want, List<ObjectId> have)
        {
            try
            {
                markRefsAdvertised();
                markReachable(have, maxTimeWanted(want));

                if (sendWants(want))
                {
                    negotiate(monitor);

                    walk.dispose();
                    reachableCommits = null;

                    receivePack(monitor);
                }
            }
            catch (CancelledException ce)
            {
                Close();
                return;
            }
            catch (IOException err)
            {
                Close();
                throw new TransportException(err.Message, err);
            }
        }

        private int maxTimeWanted(List<Ref> wants)
        {
            int maxTime = 0;
            foreach (Ref r in wants)
            {
                try
                {
                    RevObject obj = walk.parseAny(r.ObjectId);
                    if (obj is RevCommit)
                    {
                        int cTime = ((RevCommit) obj).commitTime;
                        if (maxTime < cTime)
                            maxTime = cTime;
                    }
                }
                catch (IOException error)
                {
                    
                }
            }

            return maxTime;
        }

        private void markReachable(List<ObjectId> have, int maxTime)
        {
            foreach (Ref r in local.Refs.Values)
            {
                try
                {
                    RevCommit o = walk.parseCommit(r.ObjectId);
                    o.add(REACHABLE);
                    reachableCommits.add(o);
                }
                catch (IOException readError)
                {
                    
                }
            }

            foreach (ObjectId id in have)
            {
                try
                {
                    RevCommit o = walk.parseCommit(id);
                    o.add(REACHABLE);
                    reachableCommits.add(o);
                }
                catch (IOException readError)
                {
                    
                }
            }

            if (maxTime > 0)
            {
                DateTime maxWhen = new DateTime(1970, 1, 1) + TimeSpan.FromSeconds(maxTime);
                walk.sort(RevSort.COMMIT_TIME_DESC);
                walk.markStart(reachableCommits);
                walk.setRevFilter(CommitTimeRevFilter.after(maxWhen));
                for (;;)
                {
                    RevCommit c = walk.next();
                    if (c == null)
                        break;
                    if (c.has(ADVERTISED) && !c.has(COMMON))
                    {
                        c.add(COMMON);
                        c.carry(COMMON);
                        reachableCommits.add(c);
                    }
                }
            }
        }

        private bool sendWants(List<Ref> want)
        {
            bool first = true;
            foreach (Ref r in want)
            {
                try
                {
                    if (walk.parseAny(r.ObjectId).has(REACHABLE))
                    {
                        continue;
                    }
                }
                catch (IOException err)
                {
                    
                }

                StringBuilder line = new StringBuilder(46);
                line.Append("want ");
                line.Append(r.ObjectId.Name);
                if (first)
                {
                    line.Append(enableCapabilities());
                    first = false;
                }
                line.Append('\n');
                pckOut.WriteString(line.ToString());
            }
            pckOut.End();
            outNeedsEnd = false;
            return !first;
        }

        private string enableCapabilities()
        {
            StringBuilder line = new StringBuilder();
            if (includeTags)
                includeTags = wantCapability(line, OPTION_INCLUDE_TAG);
            if (allowOfsDelta)
                wantCapability(line, OPTION_OFS_DELTA);
            multiAck = wantCapability(line, OPTION_MULTI_ACK);
            if (thinPack)
                thinPack = wantCapability(line, OPTION_THIN_PACK);
            if (wantCapability(line, OPTION_SIDE_BAND_64K))
                sideband = true;
            else if (wantCapability(line, OPTION_SIDE_BAND))
                sideband = true;
            return line.ToString();
        }

        private void negotiate(ProgressMonitor monitor)
        {
            MutableObjectId ackId = new MutableObjectId();
            int resultsPending = 0;
            int havesSent = 0;
            int havesSinceLastContinue = 0;
            bool receivedContinue = false;
            bool receivedAck = false;
            bool sendHaves = true;

            negotiateBegin();
            while (sendHaves)
            {
                RevCommit c = walk.next();
                if (c == null) break;

                pckOut.WriteString("have " + c.getId().Name + "\n");
                havesSent++;
                havesSinceLastContinue++;

                if ((31 & havesSent) != 0)
                {
                    continue;
                }

                if (monitor.IsCancelled)
                    throw new CancelledException();

                pckOut.End();
                resultsPending++;

                if (havesSent == 32)
                {
                    continue;
                }

                for (;;)
                {
                    PacketLineIn.AckNackResult anr;

                    anr = pckIn.readACK(ackId);
                    if (anr == PacketLineIn.AckNackResult.NAK)
                    {
                        resultsPending--;
                        break;
                    }

                    if (anr == PacketLineIn.AckNackResult.ACK)
                    {
                        multiAck = false;
                        resultsPending = 0;
                        receivedAck = true;
                        sendHaves = false;
                        break;
                    }

                    if (anr == PacketLineIn.AckNackResult.ACK_CONTINUE)
                    {
                        markCommon(walk.parseAny(ackId));
                        receivedAck = true;
                        receivedContinue = true;
                        havesSinceLastContinue = 0;
                    }

                    if (monitor.IsCancelled)
                        throw new CancelledException();
                }

                if (receivedContinue && havesSinceLastContinue > MAX_HAVES)
                {
                    break;
                }
            }

            if (monitor.IsCancelled)
                throw new CancelledException();
            pckOut.WriteString("done\n");
            pckOut.Flush();

            if (!receivedAck)
            {
                multiAck = false;
                resultsPending++;
            }

            while (resultsPending > 0 || multiAck)
            {
                PacketLineIn.AckNackResult anr;

                anr = pckIn.readACK(ackId);
                resultsPending--;

                if (anr == PacketLineIn.AckNackResult.ACK)
                    break;

                if (anr == PacketLineIn.AckNackResult.ACK_CONTINUE)
                    multiAck = true;

                if (monitor.IsCancelled)
                    throw new CancelledException();
            }
        }

        private class NegotiateBeginRevFilter : RevFilter
        {
            private readonly RevFlag COMMON;
            private readonly RevFlag ADVERTISED;

            public NegotiateBeginRevFilter(RevFlag c, RevFlag a)
            {
                COMMON = c;
                ADVERTISED = a;
            }

            public override RevFilter Clone()
            {
                return this;
            }

            public override bool include(GitSharp.RevWalk.RevWalk walker, RevCommit cmit)
            {
                bool remoteKnowsIsCommon = cmit.has(COMMON);
                if (cmit.has(ADVERTISED))
                {
                    cmit.add(COMMON);
                }
                return !remoteKnowsIsCommon;
            }
        }

        private void negotiateBegin()
        {
            walk.resetRetain(REACHABLE, ADVERTISED);
            walk.markStart(reachableCommits);
            walk.sort(RevSort.COMMIT_TIME_DESC);
            walk.setRevFilter(new NegotiateBeginRevFilter(COMMON, ADVERTISED));
        }

        private void markRefsAdvertised()
        {
            foreach (Ref r in Refs)
            {
                markAdvertised(r.ObjectId);
                if (r.PeeledObjectId != null)
                    markAdvertised(r.PeeledObjectId);
            }
        }

        private void markAdvertised(AnyObjectId id)
        {
            try
            {
                walk.parseAny(id).add(ADVERTISED);
            }
            catch (IOException)
            {
                
            }
        }

        private void markCommon(RevObject obj)
        {
            obj.add(COMMON);
            if (obj is RevCommit)
            {
                ((RevCommit) obj).carry(COMMON);
            }
        }

        private void receivePack(ProgressMonitor monitor)
        {
            IndexPack ip;
            ip = IndexPack.create(local, sideband ? pckIn.sideband(monitor) : stream);
            ip.setFixThin(thinPack);
            ip.setObjectChecking(transport.CheckFetchedObjects);
            ip.index(monitor);
            packLock = ip.renameAndOpenPack(lockMessage);
        }

        private class CancelledException : Exception
        {
            
        }
    }

}