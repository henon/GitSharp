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

using System.Collections.Generic;
using System.IO;
using System.Text;
using GitSharp.Exceptions;
using GitSharp.RevWalk;
using GitSharp.Util;

namespace GitSharp.Transport
{

    public class UploadPack
    {
        private static readonly string OPTION_INCLUDE_TAG = BasePackFetchConnection.OPTION_INCLUDE_TAG;
        private static readonly string OPTION_MULTI_ACK = BasePackFetchConnection.OPTION_MULTI_ACK;
        private static readonly string OPTION_THIN_PACK = BasePackFetchConnection.OPTION_THIN_PACK;
        private static readonly string OPTION_SIDE_BAND = BasePackFetchConnection.OPTION_SIDE_BAND;
        private static readonly string OPTION_SIDE_BAND_64K = BasePackFetchConnection.OPTION_SIDE_BAND_64K;
        private static readonly string OPTION_OFS_DELTA = BasePackFetchConnection.OPTION_OFS_DELTA;
        private static readonly string OPTION_NO_PROGRESS = BasePackFetchConnection.OPTION_NO_PROGRESS;

        private readonly Repository db;
        private readonly RevWalk.RevWalk walk;
        private Stream stream;
        private PacketLineIn pckIn;
        private PacketLineOut pckOut;

        private Dictionary<string, Ref> refs;
        private readonly List<string> options = new List<string>();
        private readonly List<RevObject> wantAll = new List<RevObject>();
        private readonly List<RevCommit> wantCommits = new List<RevCommit>();
        private readonly List<RevObject> commonBase = new List<RevObject>();

        private readonly RevFlag ADVERTISED;
        private readonly RevFlag WANT;
        private readonly RevFlag PEER_HAS;
        private readonly RevFlag COMMON;
        private readonly RevFlagSet SAVE;

        private bool multiAck;

        public UploadPack(Repository copyFrom)
        {
            db = copyFrom;
            walk = new RevWalk.RevWalk(db);

            ADVERTISED = walk.newFlag("ADVERTISED");
            WANT = walk.newFlag("WANT");
            PEER_HAS = walk.newFlag("PEER_HAS");
            COMMON = walk.newFlag("COMMON");
            walk.carry(PEER_HAS);

            SAVE = new RevFlagSet();
            SAVE.Add(ADVERTISED);
            SAVE.Add(WANT);
            SAVE.Add(PEER_HAS);
        }

        public Repository Repository
        {
            get
            {
                return db;
            }
        }

        public RevWalk.RevWalk RevWalk
        {
            get
            {
                return walk;
            }
        }

        public void Upload(Stream stream, Stream messages)
        {
            this.stream = stream;
            pckIn = new PacketLineIn(stream);
            pckOut = new PacketLineOut(stream);
            service();
        }

        private void service()
        {
            sendAdvertisedRefs();
            recvWants();
            if (wantAll.Count == 0)
                return;
            multiAck = options.Contains(OPTION_MULTI_ACK);
            negotiate();
            sendPack();
        }

        private void sendAdvertisedRefs()
        {
            refs = db.Refs;

            StringBuilder m = new StringBuilder(100);
            char[] idtmp = new char[2 * Constants.OBJECT_ID_LENGTH];
            IEnumerator<Ref> i = RefComparator.Sort(refs.Values).GetEnumerator();
            if (i.MoveNext())
            {
                Ref r = i.Current;
                RevObject o = safeParseAny(r.ObjectId);
                if (o != null)
                {
                    advertise(m, idtmp, o, r.OriginalName);
                    m.Append('\0');
                    m.Append(' ');
                    m.Append(OPTION_INCLUDE_TAG);
                    m.Append(' ');
                    m.Append(OPTION_MULTI_ACK);
                    m.Append(' ');
                    m.Append(OPTION_OFS_DELTA);
                    m.Append(' ');
                    m.Append(OPTION_SIDE_BAND);
                    m.Append(' ');
                    m.Append(OPTION_SIDE_BAND_64K);
                    m.Append(' ');
                    m.Append(OPTION_THIN_PACK);
                    m.Append(' ');
                    m.Append(OPTION_NO_PROGRESS);
                    m.Append(' ');
                    writeAdvertisedRef(m);
                    if (o is RevTag)
                        writeAdvertisedTag(m, idtmp, o, r.Name);
                }
            }
            while (i.MoveNext())
            {
                Ref r = i.Current;
                RevObject o = safeParseAny(r.ObjectId);
                if (o != null)
                {
                    advertise(m, idtmp, o, r.OriginalName);
                    writeAdvertisedRef(m);
                    if (o is RevTag)
                        writeAdvertisedTag(m, idtmp, o, r.Name);
                }
            }
            pckOut.End();
        }

        private RevObject safeParseAny(ObjectId id)
        {
            try
            {
                return walk.parseAny(id);
            }
            catch (IOException)
            {
                return null;
            }
        }

        private void advertise(StringBuilder m, char[] idtmp, RevObject o, string name)
        {
            o.add(ADVERTISED);
            m.Length = 0;
            o.getId().CopyTo(idtmp, m);
            m.Append(' ');
            m.Append(name);
        }

        private void writeAdvertisedRef(StringBuilder m)
        {
            m.Append('\n');
            pckOut.WriteString(m.ToString());
        }

        private void writeAdvertisedTag(StringBuilder m, char[] idtmp, RevObject tag, string name)
        {
            RevObject o = tag;
            while (o is RevTag)
            {
                try
                {
                    walk.parse(((RevTag) o).getObject());
                }
                catch (IOException)
                {
                    return;
                }
                o = ((RevTag) o).getObject();
                o.add(ADVERTISED);
            }
            advertise(m, idtmp, ((RevTag) tag).getObject(), name + "^{}");
            writeAdvertisedRef(m);
        }

        private void recvWants()
        {
            bool isFirst = true;
            for (;; isFirst = false)
            {
                string line;
                try
                {
                    line = pckIn.ReadString();
                }
                catch (EndOfStreamException eof)
                {
                    if (isFirst) break;
                    throw eof;
                }

                if (line.Length == 0) break;
                if (!line.StartsWith("want ") || line.Length < 45)
                    throw new PackProtocolException("expected want; got " + line);

                if (isFirst)
                {
                    int sp = line.IndexOf(' ', 45);
                    if (sp >= 0)
                    {
                        foreach (string c in line.Substring(sp + 1).Split(' '))
                            options.Add(c);
                        line = line.Slice(0, sp);
                    }
                }

                string name = line.Substring(5);
                ObjectId id = ObjectId.FromString(name);
                RevObject o;
                try
                {
                    o = walk.parseAny(id);
                }
                catch (IOException e)
                {
                    throw new PackProtocolException(name + " not valid", e);
                }
                if (!o.has(ADVERTISED))
                    throw new PackProtocolException(name + " not valid");
                want(o);
            }
        }

        private void want(RevObject o)
        {
            if (!o.has(WANT))
            {
                o.add(WANT);
                wantAll.Add(o);

                if (o is RevCommit)
                    wantCommits.Add((RevCommit) o);
                else if (o is RevTag)
                {
                    do
                    {
                        o = ((RevTag) o).getObject();
                    } while (o is RevTag);
                    if (o is RevCommit)
                        want(o);
                }
            }
        }

        private void negotiate()
        {
            ObjectId last = ObjectId.ZeroId;
            string lastName = "";
            for (;;)
            {
                string line;
                try
                {
                    line = pckIn.ReadString();
                }
                catch (EndOfStreamException eof)
                {
                    throw eof;
                }

                if (line.Length == 0)
                {
                    if (commonBase.Count == 0 || multiAck)
                        pckOut.WriteString("NAK\n");
                    pckOut.Flush();
                }
                else if (line.StartsWith("have ") && line.Length == 45)
                {
                    string name = line.Substring(5);
                    ObjectId id = ObjectId.FromString(name);
                    if (matchHave(id))
                    {
                        if (multiAck)
                        {
                            last = id;
                            lastName = name;
                            pckOut.WriteString("ACK " + name + " continue\n");
                        }
                        else if (commonBase.Count == 1)
                            pckOut.WriteString("ACK " + name + "\n");
                    }
                    else
                    {
                        if (multiAck && okToGiveUp())
                            pckOut.WriteString("ACK " + name + " continue\n");
                    }
                }
                else if (line.Equals("done"))
                {
                    if (commonBase.Count == 0)
                        pckOut.WriteString("NAK\n");
                    else if (multiAck)
                        pckOut.WriteString("ACK " + lastName + "\n");
                    break;
                }
                else
                {
                    throw new PackProtocolException("expected have; got " + line);
                }
            }
        }

        private bool matchHave(ObjectId id)
        {
            RevObject o;
            try
            {
                o = walk.parseAny(id);
            }
            catch (IOException)
            {
                return false;
            }

            if (!o.has(PEER_HAS))
            {
                o.add(PEER_HAS);
                if (o is RevCommit)
                    ((RevCommit) o).carry(PEER_HAS);
                if (!o.has(COMMON))
                {
                    o.add(COMMON);
                    commonBase.Add(o);
                }
            }
            return true;
        }

        private bool okToGiveUp()
        {
            if (commonBase.Count == 0)
                return false;

            try
            {
                for (var i = wantCommits.GetEnumerator(); i.MoveNext();)
                {
                    RevCommit want = i.Current;
                    if (wantSatisfied(want))
                        wantCommits.Remove(want);
                }
            }
            catch (IOException e)
            {
                throw new PackProtocolException("internal revision error", e);
            }
            return wantCommits.Count == 0;
        }

        private bool wantSatisfied(RevCommit want)
        {
            walk.resetRetain(SAVE);
            walk.markStart(want);
            for (;;)
            {
                RevCommit c = walk.next();
                if (c == null) break;
                if (c.has(PEER_HAS))
                {
                    if (!c.has(COMMON))
                    {
                        c.add(COMMON);
                        commonBase.Add(c);
                    }
                    return true;
                }
                c.dispose();
            }
            return false;            
        }

        private void sendPack()
        {
            bool thin = options.Contains(OPTION_THIN_PACK);
            bool progress = !options.Contains(OPTION_NO_PROGRESS);
            bool sideband = options.Contains(OPTION_SIDE_BAND) || options.Contains(OPTION_SIDE_BAND_64K);

            ProgressMonitor pm = new NullProgressMonitor();
            Stream packOut = stream;

            if (sideband)
            {
                int bufsz = SideBandOutputStream.SMALL_BUF;
                if (options.Contains(OPTION_SIDE_BAND_64K))
                    bufsz = SideBandOutputStream.MAX_BUF;
                bufsz -= SideBandOutputStream.HDR_SIZE;

                packOut = new BufferedStream(new SideBandOutputStream(SideBandOutputStream.CH_DATA, pckOut), bufsz);

                // [caytchen] TODO: SideBandProgressMonitor
                /*if (progress)
                    pm = new SideBandProgressMonitor(pckOut);*/
            }

            PackWriter pw;
            pw = new PackWriter(db, pm, new NullProgressMonitor());
            pw.DeltaBaseAsOffset = options.Contains(OPTION_OFS_DELTA);
            pw.Thin = thin;
            pw.preparePack(wantAll, commonBase);
            if (options.Contains(OPTION_INCLUDE_TAG))
            {
                foreach (Ref r in refs.Values)
                {
                    RevObject o;
                    try
                    {
                        o = walk.parseAny(r.ObjectId);
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    if (o.has(WANT) || !(o is RevTag))
                        continue;
                    RevTag t = (RevTag) o;
                    if (!pw.willInclude(t) && pw.willInclude(t.getObject()))
                        pw.addObject(t);
                }
            }
            pw.writePack(packOut);

            if (sideband)
            {
                packOut.Flush();
                pckOut.End();
            }
            else
            {
                stream.Flush();
            }
        }
    }

}