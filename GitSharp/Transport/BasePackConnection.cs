/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp.Transport
{

    public abstract class BasePackConnection : BaseConnection
    {
        protected readonly Repository local;
        protected readonly URIish uri;
        protected readonly Transport transport;
        protected Stream stream;
        protected PacketLineIn pckIn;
        protected PacketLineOut pckOut;
        protected bool outNeedsEnd;
        private readonly List<string> remoteCapabilies = new List<string>();
        protected readonly List<ObjectId> additionalHaves = new List<ObjectId>();

        protected BasePackConnection(IPackTransport packTransport)
        {
            transport = (Transport) packTransport;
            local = transport.Local;
            uri = transport.Uri;
        }

        protected void init(Stream myStream)
        {
            stream = myStream is BufferedStream ? myStream : new BufferedStream(myStream, IndexPack.BUFFER_SIZE);

            pckIn = new PacketLineIn(stream);
            pckOut = new PacketLineOut(stream);
            outNeedsEnd = true;
        }

        protected void readAdvertisedRefs()
        {
            try
            {
                readAdvertisedRefsImpl();
            }
            catch (TransportException err)
            {
                Close();
                throw err;
            }
            catch (IOException err)
            {
                Close();
                throw new TransportException(err.Message, err);
            }
        }

        private void readAdvertisedRefsImpl()
        {
            Dictionary<string, Ref> avail = new Dictionary<string, Ref>();
            for (;;)
            {
                string line;

                try
                {
                    line = pckIn.ReadString();
                }
                catch (EndOfStreamException eof)
                {
                    if (avail.Count == 0)
                        throw noRepository();
                    throw eof;
                }

                if (avail.Count == 0)
                {
                    int nul = line.IndexOf('\0');
                    if (nul >= 0)
                    {
                        foreach (string c in line.Substring(nul + 1).Split(' '))
                            remoteCapabilies.Add(c);
                        line = line.Slice(0, nul);
                    }
                }

                if (line.Length == 0)
                    break;

                string name = line.Slice(41, line.Length);
                if (avail.Count == 0 && name.Equals("capabilities^{}"))
                {
                    continue;
                }

                string idname = line.Slice(0, 40);
                ObjectId id = ObjectId.FromString(idname);
                if (name.Equals(".have"))
                    additionalHaves.Add(id);
                else if (name.EndsWith("^{}"))
                {
                    name = name.Slice(0, name.Length - 3);
                    Ref prior = avail.ContainsKey(name) ? avail[name] : null;
                    if (prior == null)
                        throw new PackProtocolException(uri, "advertisement of " + name + "^{} came before " + name);

                    if (prior.PeeledObjectId != null)
                        throw duplicateAdvertisement(name + "^{}");

                    avail.Add(name, new Ref(Ref.Storage.Network, name, prior.ObjectId, id, true));
                }
                else
                {
                    Ref prior = avail.ContainsKey(name) ? avail[name] : null;
                    if (prior != null)
                        throw duplicateAdvertisement(name);
                    avail.Add(name, new Ref(Ref.Storage.Network, name, id));
                }
            }
            available(avail);
        }

        protected TransportException noRepository()
        {
            return new NoRemoteRepositoryException(uri, "not found.");
        }

        protected bool isCapableOf(string option)
        {
            return remoteCapabilies.Contains(option);
        }

        protected bool wantCapability(StringBuilder b, string option)
        {
            if (!isCapableOf(option))
                return false;
            b.Append(' ');
            b.Append(option);
            return true;
        }

        private PackProtocolException duplicateAdvertisement(string name)
        {
            return new PackProtocolException(uri, "duplicate advertisements of " + name);
        }

        public override void Close()
        {
            if (stream != null)
            {
                try
                {
                    if (outNeedsEnd)
                        pckOut.End();
                    stream.Close();
                }
                catch (IOException)
                {
                    
                }
                finally
                {
                    stream = null;
                    pckOut = null;
                }
            }
        }
    }

}