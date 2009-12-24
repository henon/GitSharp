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
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{

    public class ReceivePack : IDisposable
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

        public ReceivePack(Repository into)
        {
            db = into;
            walk = new RevWalk.RevWalk(db);

            RepositoryConfig cfg = db.Config;
            checkReceivedObjects = cfg.getBoolean("receive", "fsckobjects", false);
            allowCreates = true;
            allowDeletes = !cfg.getBoolean("receive", "denydeletes", false);
            allowNonFastForwards = !cfg.getBoolean("receive", "denynonfastforwards", false);
            allowOfsDelta = cfg.getBoolean("repack", "usedeltabaseoffset", true);
            preReceive = PreReceiveHook.NULL;
            postReceive = PostReceiveHook.NULL;
        }

        public void receive(Stream stream, Stream messages)
        {
            try
            {
                raw = stream;

                pckIn = new PacketLineIn(raw);
                pckOut = new PacketLineOut(raw);

                if (messages != null)
                {
                    msgs = new StreamWriter(messages);
                }

                enabledCapabilities = new List<string>();
                commands = new List<ReceiveCommand>();

                Service();
            }
            finally
            {
                try
                {
                    if (msgs != null)
                        msgs.Flush();
                }
                finally
                {
                    UnlockPack();
                    raw = null;
                    pckIn = null;
                    pckOut = null;
                    msgs = null;
                    refs = null;
                    enabledCapabilities = null;
                    commands = null;
                }
            }
        }

        private void Service()
        {
            SendAdvertisedRefs();
            RecvCommands();
        	if (commands.isEmpty()) return;
        	EnableCapabilities();

        	if (NeedPack())
        	{
        		try
        		{
        			receivePack();
        			if (isCheckReceivedObjects())
        			{
        				CheckConnectivity();
        			}
        			unpackError = null;
        		}
        		catch (IOException err)
        		{
        			unpackError = err;
        		}
        		catch (Exception err)
        		{
        			unpackError = err;
        		}
        	}

        	if (unpackError == null)
        	{
        		ValidateCommands();
        		ExecuteCommands();
        	}
        	UnlockPack();

        	if (reportStatus)
        	{
        		SendStatusReport(true, new ServiceReporter(pckOut));
        		pckOut.End();
        	}
        	else if (msgs != null)
        	{
        		SendStatusReport(false, new MessagesReporter(msgs.BaseStream));
        		msgs.Flush();
        	}

        	postReceive.OnPostReceive(this, FilterCommands(ReceiveCommand.Result.OK));
        }

        private void RecvCommands()
        {
            while (true)
            {
                string line;
                try
                {
                    line = pckIn.ReadStringRaw();
                }
                catch (EndOfStreamException)
                {
                    if (commands.isEmpty())
                    {
                        return;
                    }
                    throw;
                }

                if (commands.isEmpty())
                {
                    int nul = line.IndexOf('\0');
                    if (nul >= 0)
                    {
                        foreach (string c in line.Substring(nul + 1).Split(' '))
                        {
                            enabledCapabilities.Add(c);
                        }
                        line = line.Slice(0, nul);
                    }
                }

                if (line.Length == 0) break;
                if (line.Length < 83)
                {
                    string m = "error: invalid protocol: wanted 'old new ref'";
                    sendError(m);
                    throw new PackProtocolException(m);
                }

                ObjectId oldId = ObjectId.FromString(line.Slice(0, 40));
                ObjectId newId = ObjectId.FromString(line.Slice(41, 81));
                string name = line.Substring(82);
                var cmd = new ReceiveCommand(oldId, newId, name);
                cmd.setRef(refs[cmd.getRefName()]);
                commands.Add(cmd);
            }
        }

        private void receivePack()
        {
            IndexPack ip = IndexPack.Create(db, raw);
            ip.setFixThin(true);
            ip.setObjectChecking(isCheckReceivedObjects());
            ip.index(new NullProgressMonitor());

            // [caytchen] TODO: reflect gitsharp
            string lockMsg = "jgit receive-pack";
            if (getRefLogIdent() != null)
                lockMsg += " from " + getRefLogIdent().ToExternalString();
            packLock = ip.renameAndOpenPack(lockMsg);
        }

        private void SendStatusReport(bool forClient, Reporter rout)
        {
            if (unpackError != null)
            {
                rout.SendString("unpack error " + unpackError.Message);
                if (forClient)
                {
                    foreach (ReceiveCommand cmd in commands)
                    {
                        rout.SendString("ng " + cmd.getRefName() + " n/a (unpacker error)");
                    }
                }

                return;
            }

            if (forClient)
            {
            	rout.SendString("unpack ok");
            }
            foreach (ReceiveCommand cmd in commands)
            {
                if (cmd.getResult() == ReceiveCommand.Result.OK)
                {
                    if (forClient)
                        rout.SendString("ok " + cmd.getRefName());
                    continue;
                }

                var r = new StringBuilder();
                r.Append("ng ");
                r.Append(cmd.getRefName());
                r.Append(" ");

                switch (cmd.getResult())
                {
                    case ReceiveCommand.Result.NOT_ATTEMPTED:
                        r.Append("server bug; ref not processed");
                        break;

                    case ReceiveCommand.Result.REJECTED_NOCREATE:
                        r.Append("creation prohibited");
                        break;

                    case ReceiveCommand.Result.REJECTED_NODELETE:
                        r.Append("deletion prohibited");
                        break;

                    case ReceiveCommand.Result.REJECTED_NONFASTFORWARD:
                        r.Append("non-fast forward");
                        break;

                    case ReceiveCommand.Result.REJECTED_CURRENT_BRANCH:
                        r.Append("branch is currently checked out");
                        break;

                    case ReceiveCommand.Result.REJECTED_MISSING_OBJECT:
                        if (cmd.getMessage() == null)
                            r.Append("missing object(s)");
                        else if (cmd.getMessage().Length == 2 * Constants.OBJECT_ID_LENGTH)
                            r.Append("object " + cmd.getMessage() + " missing");
                        else
                            r.Append(cmd.getMessage());
                        break;

                    case ReceiveCommand.Result.REJECTED_OTHER_REASON:
                        if (cmd.getMessage() == null)
                            r.Append("unspecified reason");
                        else
                            r.Append(cmd.getMessage());
                        break;

                    case ReceiveCommand.Result.LOCK_FAILURE:
                        r.Append("failed to lock");
                        break;

                    case ReceiveCommand.Result.OK:
                        // We shouldn't have reached this case (see 'ok' case above).
                        continue;
                }

                rout.SendString(r.ToString());
            }
        }

        private void SendAdvertisedRefs()
        {
            refs = db.getAllRefs();

            var m = new StringBuilder(100);
            char[] idtmp = new char[2 * Constants.OBJECT_ID_LENGTH];
            IEnumerator<Ref> i = RefComparator.Sort(refs.Values).GetEnumerator();
            {
                if (i.MoveNext())
                {
                    Ref r = i.Current;
                    Format(m, idtmp, r.ObjectId, r.OriginalName);
                }
                else
                {
                    Format(m, idtmp, ObjectId.ZeroId, "capabilities^^{}");
                }
                m.Append('\0');
                m.Append(' ');
                m.Append(CAPABILITY_DELETE_REFS);
                m.Append(' ');
                m.Append(CAPABILITY_REPORT_STATUS);
                if (allowOfsDelta)
                {
                    m.Append(' ');
                    m.Append(CAPABILITY_OFS_DELTA);
                }
                m.Append(' ');
                WriteAdvertisedRef(m);
            }

            while (i.MoveNext())
            {
                Ref r = i.Current;
                Format(m, idtmp, r.ObjectId, r.Name);
                WriteAdvertisedRef(m);
            }
            pckOut.End();
        }

        private void ExecuteCommands()
        {
            preReceive.onPreReceive(this, FilterCommands(ReceiveCommand.Result.NOT_ATTEMPTED));
            foreach (ReceiveCommand cmd in FilterCommands(ReceiveCommand.Result.NOT_ATTEMPTED))
            {
                Execute(cmd);
            }
        }

        public void sendError(string what)
        {
            SendMessage("error", what);
        }

        public void sendMessage(string what)
        {
            SendMessage("remote", what);
        }

        private void SendMessage(string type, string what)
        {
            if (msgs != null)
            {
                msgs.WriteLine(type + ": " + what);
            }
        }

        private void UnlockPack()
        {
            if (packLock != null)
            {
                packLock.Unlock();
                packLock = null;
            }
        }

        private static void Format(StringBuilder m, char[] idtmp, AnyObjectId id, string name)
        {
            m.Length = 0;
            id.CopyTo(idtmp, m);
            m.Append(' ');
            m.Append(name);
        }

        private void WriteAdvertisedRef(StringBuilder m)
        {
            m.Append('\n');
            pckOut.WriteString(m.ToString());
        }

        private void EnableCapabilities()
        {
            reportStatus = enabledCapabilities.Contains(CAPABILITY_REPORT_STATUS);
        }

        private bool NeedPack()
        {
            foreach (ReceiveCommand cmd in commands)
            {
                if (cmd.getType() != ReceiveCommand.Type.DELETE) return true;
            }
            return false;
        }

        private void CheckConnectivity()
        {
            using(var ow = new ObjectWalk(db))
			{
	            foreach (ReceiveCommand cmd in commands)
	            {
	                if (cmd.getResult() != ReceiveCommand.Result.NOT_ATTEMPTED) continue;
	                if (cmd.getType() == ReceiveCommand.Type.DELETE) continue;
	                ow.markStart(ow.parseAny(cmd.getNewId()));
	            }
	            foreach (Ref @ref in refs.Values)
	            {
	            	ow.markUninteresting(ow.parseAny(@ref.ObjectId));
	            }
	            ow.checkConnectivity();
			}
        }

        private void ValidateCommands()
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

					RevCommit oldComm = (oldObj as RevCommit);
					RevCommit newComm = (newObj as RevCommit);
                    if (oldComm != null && newComm != null)
                    {
                        try
                        {
                            if (!walk.isMergedInto(oldComm, newComm))
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

        private void Execute(ReceiveCommand cmd)
        {
            try
            {
                RefUpdate ru = db.UpdateRef(cmd.getRefName());
                ru.RefLogIdent = getRefLogIdent();
                switch (cmd.getType())
                {
                    case ReceiveCommand.Type.DELETE:
                        if (!ObjectId.ZeroId.Equals(cmd.getOldId()))
                        {
                            ru.ExpectedOldObjectId = cmd.getOldId();
                        }
                        ru.IsForceUpdate = true;
                        Status(cmd, ru.Delete(walk));
                        break;

                    case ReceiveCommand.Type.CREATE:
                    case ReceiveCommand.Type.UPDATE:
                    case ReceiveCommand.Type.UPDATE_NONFASTFORWARD:
                        ru.IsForceUpdate = isAllowNonFastForwards();
                        ru.ExpectedOldObjectId = cmd.getOldId();
                        ru.NewObjectId = cmd.getNewId();
                        ru.SetRefLogMessage("push", true);
                        Status(cmd, ru.Update(walk));
                        break;
                }
            }
            catch (IOException err)
            {
                cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, "lock error: " + err.Message);
            }
        }

        private List<ReceiveCommand> FilterCommands(ReceiveCommand.Result want)
        {
            var r = new List<ReceiveCommand>(commands.Count);
            foreach (ReceiveCommand cmd in commands)
            {
                if (cmd.getResult() == want)
                    r.Add(cmd);
            }
            return r;
        }

        private static void Status(ReceiveCommand cmd, RefUpdate.RefUpdateResult result)
        {
            switch (result)
            {
                case RefUpdate.RefUpdateResult.NotAttempted:
                    cmd.setResult(ReceiveCommand.Result.NOT_ATTEMPTED);
                    break;

                case RefUpdate.RefUpdateResult.LockFailure:
                case RefUpdate.RefUpdateResult.IOFailure:
                    cmd.setResult(ReceiveCommand.Result.LOCK_FAILURE);
                    break;

                case RefUpdate.RefUpdateResult.NoChange:
                case RefUpdate.RefUpdateResult.New:
                case RefUpdate.RefUpdateResult.Forced:
                case RefUpdate.RefUpdateResult.FastForward:
                    cmd.setResult(ReceiveCommand.Result.OK);
                    break;

                case RefUpdate.RefUpdateResult.Rejected:
                    cmd.setResult(ReceiveCommand.Result.REJECTED_NONFASTFORWARD);
                    break;

                case RefUpdate.RefUpdateResult.RejectedCurrentBranch:
                    cmd.setResult(ReceiveCommand.Result.REJECTED_CURRENT_BRANCH);
                    break;

                default:
                    cmd.setResult(ReceiveCommand.Result.REJECTED_OTHER_REASON, result.ToString());
                    break;
            }
		}
		
		public void Dispose ()
		{
			walk.Dispose();
			raw.Dispose();
			msgs.Dispose();
		}
		

		#region Nested Types

		private abstract class Reporter
        {
            public abstract void SendString(string s);
        }

        private class ServiceReporter : Reporter
        {
            private readonly PacketLineOut _pckOut;

            public ServiceReporter(PacketLineOut pck)
            {
                _pckOut = pck;
            }

            public override void SendString(string s)
            {
                _pckOut.WriteString(s + "\n");
            }
        }

        private class MessagesReporter : Reporter
        {
            private readonly Stream _stream;

            public MessagesReporter(Stream ms)
            {
                _stream = ms;
            }

            public override void SendString(string s)
            {
            	byte[] data = Constants.encode(s);
                _stream.Write(data, 0, data.Length);
            }
		}

		#endregion
	}
}