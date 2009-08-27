/*
 * Copyright (C) 2008, 2009, Google Inc.
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
using GitSharp.RevWalk;

namespace GitSharp.Transport
{
    
    public class RefAdvertiser
    {
        private readonly PacketLineOut pckOut;
        private readonly RevWalk.RevWalk walk;
        private readonly RevFlag ADVERTISED;
        private readonly StringBuilder tmpLine = new StringBuilder(100);
        private readonly char[] tmpId = new char[2 * Constants.OBJECT_ID_LENGTH];
        private readonly List<string> capabilities = new List<string>();
        private bool derefTags;
        private bool first = true;

        public RefAdvertiser(PacketLineOut o, RevWalk.RevWalk protoWalk, RevFlag advertisedFlag)
        {
            pckOut = o;
            walk = protoWalk;
            ADVERTISED = advertisedFlag;
        }

        public void setDerefTags(bool deref)
        {
            derefTags = deref;
        }

        public void advertiseCapability(string name)
        {
            capabilities.Add(name);
        }

        public void send(List<Ref> refs)
        {
            foreach (Ref r in RefComparator.Sort(refs))
            {
                RevObject obj = parseAnyOrNull(r.ObjectId);
                if (obj != null)
                {
                    advertiseAny(obj, r.OriginalName);
                    if (derefTags && obj is RevTag)
                        advertiseTag((RevTag) obj, r.OriginalName + "^{}");
                }
            }
        }

        public void advertiseHave(AnyObjectId id)
        {
            RevObject obj = parseAnyOrNull(id);
            if (obj != null)
                advertiseAnyOnce(obj, ".have");
            if (obj is RevTag)
                advertiseAnyOnce(((RevTag) obj).getObject(), ".have");
        }

        public void includeAdditionalHaves()
        {
            additionalHaves(walk.getRepository().getObjectDatabase());
        }

        private void additionalHaves(ObjectDatabase db)
        {
            if (db is AlternateRepositoryDatabase)
            {
                additionalHaves(((AlternateRepositoryDatabase) db).getRepository());
            }
            foreach (ObjectDatabase alt in db.getAlternates())
                additionalHaves(alt);
        }

        private void additionalHaves(Repository alt)
        {
            foreach (Ref r in alt.Refs.Values)
            {
                advertiseHave(r.ObjectId);
            }
        }

        public bool isEmpty()
        {
            return first;
        }

        private RevObject parseAnyOrNull(AnyObjectId id)
        {
            if (id == null)
                return null;
            try
            {
                return walk.parseAny(id);
            }
            catch (IOException)
            {
                return null;
            }
        }

        private void advertiseAnyOnce(RevObject obj, string refName)
        {
            if (!obj.has(ADVERTISED))
                advertiseAny(obj, refName);
        }

        private void advertiseAny(RevObject obj, string refName)
        {
            obj.add(ADVERTISED);
            advertiseId(obj, refName);
        }

        private void advertiseTag(RevTag tag, string refName)
        {
            RevObject o = tag;
            do
            {
                RevObject target = ((RevTag) o).getObject();
                try
                {
                    walk.parseHeaders(target);
                }
                catch (IOException)
                {
                    return;
                }
                target.add(ADVERTISED);
                o = target;
            } while (o is RevTag);
            advertiseAny(tag.getObject(), refName);
        }

        void advertiseId(AnyObjectId id, string refName)
        {
            tmpLine.Length = 0;
            id.CopyTo(tmpId, tmpLine);
            tmpLine.Append(' ');
            tmpLine.Append(refName);
            if (first)
            {
                first = false;
                if (capabilities.Count > 0)
                {
                    tmpLine.Append('\0');
                    foreach (string capName in capabilities)
                    {
                        tmpLine.Append(' ');
                        tmpLine.Append(capName);
                    }
                    tmpLine.Append(' ');
                }
            }
            tmpLine.Append('\n');
            pckOut.WriteString(tmpLine.ToString());
        }
    }

}