/*
 * Copyright (C) 2008, Google Inc.
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

namespace GitSharp.Transport
{

    public class ReceivePack
    {
        private const string CAPABILITY_REPORT_STATUS = BasePackPushConnection.CAPABILITY_REPORT_STATUS;
        private const string CAPABILITY_DELETE_REFS = BasePackPushConnection.CAPABILITY_DELETE_REFS;
        private const string CAPABILITY_OFS_DELTA = BasePackPushConnection.CAPABILITY_OFS_DELTA;

        private readonly Repository db;
        private readonly RevWalk.RevWalk walk;
        private bool checkReceivedObjects;
        private bool allowCreates;
        private bool allowDeletes;
        private bool allowNonFastForwards;
        private bool allowOfsDelta;
        private PersonIdent refLogIdent;
        private IPreReceiveHook preReceive;
        private IPostReceiveHook postReceive;
        private Stream raw;
        private PacketLineIn pckIn;
        private PacketLineOut pckOut;
        private StreamWriter msgs;
        private Dictionary<string, Ref> refs;
        private List<string> enabledCapabilities;
        private List<ReceiveCommand> commands;
        private Exception unpackError;
        private bool reportStatus;
        private PackLock packLock;

        public Repository getRepository()
        {
            return db;
        }

        public RevWalk.RevWalk getRevWalk()
        {
            return walk;
        }

        public Dictionary<string, Ref> getAdvertisedRefs()
        {
            return refs;
        }

        public bool isCheckReceivedObjects()
        {
            return checkReceivedObjects;
        }

        public void setCheckReceivedObjects(bool check)
        {
            checkReceivedObjects = check;
        }

        public bool isAllowCreates()
        {
            return allowCreates;
        }

        public bool isAllowDeletes()
        {
            return allowDeletes;
        }

        public void setAllowDeletes(bool canDelete)
        {
            allowDeletes = canDelete;
        }

        public bool isAllowNonFastForwards()
        {
            return allowNonFastForwards;
        }

        public void setAllowNonFastForwards(bool canRewind)
        {
            allowNonFastForwards = canRewind;
        }

        public PersonIdent getRefLogIdent()
        {
            return refLogIdent;
        }

        public void setRefLogIdent(PersonIdent pi)
        {
            refLogIdent = pi;
        }

        public IPreReceiveHook getPreReceiveHook()
        {
            return preReceive;
        }

        public void setPreReceiveHook(IPreReceiveHook h)
        {
            preReceive = h ?? PreReceiveHook.NULL;
        }

        public IPostReceiveHook getPostReceiveHook()
        {
            return postReceive;
        }

        public void setPostReceiveHook(IPostReceiveHook h)
        {
            postReceive = h ?? PostReceiveHook.NULL;
        }

        public List<ReceiveCommand> getAllCommands()
        {
            return commands;
        }

        public void sendError(string what)
        {
            sendMessage("error", what);
        }

        public void sendMessage(string what)
        {
            sendMessage("remote", what);
        }

        private void sendMessage(string type, string what)
        {
            if (msgs != null)
            {
                msgs.WriteLine(type + ": " + what);
            }
        }

        private void unlockPack()
        {
            if (packLock != null)
            {
                packLock.Unlock();
                packLock = null;
            }
        }

        private void format(StringBuilder m, char[] idtmp, ObjectId id, string name)
        {
            m.Length = 0;
            id.CopyTo(idtmp, m);
            m.Append(' ');
            m.Append(name);
        }

        private void writeAdvertisedRef(StringBuilder m)
        {
            m.Append('\n');
            pckOut.WriteString(m.ToString());
        }

        private void enableCapabilities()
        {
            reportStatus = enabledCapabilities.Contains(CAPABILITY_REPORT_STATUS);
        }

        private bool needPack()
        {
            foreach (ReceiveCommand cmd in commands)
            {
                if (cmd.getType() != ReceiveCommand.Type.DELETE)
                    return true;
            }
            return false;
        }

        private void checkConnectivity()
        {
            ObjectWalk ow = new ObjectWalk(db);
            foreach (ReceiveCommand cmd in commands)
            {
                if (cmd.getResult() != ReceiveCommand.Result.NOT_ATTEMPTED)
                    continue;
                if (cmd.getType() == ReceiveCommand.Type.DELETE)
                    continue;
                ow.markStart(ow.parseAny(cmd.getNewId()));
            }
            foreach (Ref @ref in refs.Values)
                ow.markUninteresting(ow.parseAny(@ref.ObjectId));
            ow.checkConnectivity();
        }

        private void validateCommands()
        {
            foreach (ReceiveCommand cmd in commands)
            {
                Ref @ref = cmd.getRef();
                if (cmd.getResult() != ReceiveCommand.Result.NOT_ATTEMPTED)
                    continue;

                if (cmd.getType() == ReceiveCommand.Type.DELETE && !isAllowDeletes())
                {
                    cmd.setResult(ReceiveCommand.Result.REJECTED_NODELETE);
                    continue;
                }

                if (cmd.getType() == ReceiveCommand.Type.CREATE)
                {
                    if (!isAllowCreates())
                    {
                        cmd.setResult(ReceiveCommand.Result.REJECTED_NOCREATE);
                        continue;
                    }

                    if (@ref != null && !isAllowNonFastForwards())
                    {
                        cmd.setResult(ReceiveCommand.Result.REJECTED_NONFASTFORWARD);
                        continue;
                    }

                    if (@ref != null)
                    {
                        cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "ref exists");
                        continue;
                    }
                }

                if (cmd.getType() == ReceiveCommand.Type.DELETE && @ref != null && !ObjectId.ZeroId.Equals(cmd.getOldId()) && !@ref.ObjectId.Equals(cmd.getOldId()))
                {
                    cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "invalid old id sent");
                    continue;
                }

                if (cmd.getType() == ReceiveCommand.Type.UPDATE)
                {
                    if (@ref == null)
                    {
                        cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "no such ref");
                        continue;
                    }

                    if (!@ref.ObjectId.Equals(cmd.getOldId()))
                    {
                        cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "invalid old id sent");
                        continue;
                    }

                    RevObject oldObj, newObj;
                    try
                    {
                        oldObj = walk.parseAny(cmd.getOldId());
                    }
                    catch (IOException)
                    {
                        cmd.setResult(ReceiveCommand.Result.REJECTED_MISSING_OBJECT, cmd.getOldId().ToString());
                        continue;
                    }

                    try
                    {
                        newObj = walk.parseAny(cmd.getNewId());
                    }
                    catch (IOException)
                    {
                        cmd.setResult(ReceiveCommand.Result.REJECTED_MISSING_OBJECT, cmd.getNewId().ToString());
                        continue;
                    }

                    if (oldObj is RevCommit && newObj is RevCommit)
                    {
                        try
                        {
                            if (!walk.isMergedInto((RevCommit)oldObj, (RevCommit)newObj))
                            {
                                cmd.setType(ReceiveCommand.Type.UPDATE_NONFASTFORWARD);   
                            }
                        }
                        catch (MissingObjectException e)
                        {
                            cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, e.Message);
                        }
                        catch (IOException)
                        {
                            cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON);
                        }
                    }
                    else
                    {
                        cmd.setType(ReceiveCommand.Type.UPDATE_NONFASTFORWARD);
                    }
                }

                if (!cmd.getRefName().StartsWith(Constants.R_REFS) || !Repository.IsValidRefName(cmd.getRefName()))
                {
                    cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "funny refname");
                }
            }
        }

        private List<ReceiveCommand> filterCommands(ReceiveCommand.Result want)
        {
            List<ReceiveCommand> r = new List<ReceiveCommand>(commands.Count);
            foreach (ReceiveCommand cmd in commands)
            {
                if (cmd.getResult() == want)
                    r.Add(cmd);
            }
            return r;
        }

        private abstract class Reporter
        {
            public abstract void sendString(string s);
        }
    }

}