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

using System;
using System.Net.Sockets;
using System.Text;
using GitSharp.Exceptions;

namespace GitSharp.Transport
{
    public class TransportGitAnon : TcpTransport, IPackTransport
    {
        public const int GIT_PORT = Daemon.DEFAULT_PORT;

        public static bool canHandle(URIish uri)
        {
            return "git".Equals(uri.Scheme);
        }

        public TransportGitAnon(Repository local, URIish uri)
            : base(local, uri)
        {
        }

        public override IFetchConnection openFetch()
        {
            return new TcpFetchConnection(this);
        }

        public override IPushConnection openPush()
        {
            return new TcpPushConnection(this);
        }

        public override void close()
        {
        }

        public Socket openConnection()
        {
            int port = uri.Port > 0 ? uri.Port : GIT_PORT;
            try
            {
                Socket ret = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ret.Connect(uri.Host, port);
                return ret;
            }
            catch (SocketException e)
            {
                throw new TransportException(uri, "Connection failed", e);
            }
        }

        public void service(string name, PacketLineOut pckOut)
        {
            StringBuilder cmd = new StringBuilder();
            cmd.Append(name);
            cmd.Append(' ');
            cmd.Append(uri.Path);
            cmd.Append('\0');
            cmd.Append("host=");
            cmd.Append(uri.Host);
            if (uri.Port > 0 && uri.Port != GIT_PORT)
            {
                cmd.Append(":");
                cmd.Append(uri.Port);
            }
            cmd.Append('\0');
            pckOut.WriteString(cmd.ToString());
            pckOut.Flush();
        }

        private class TcpFetchConnection : BasePackFetchConnection
        {
            private Socket sock;

            public TcpFetchConnection(TransportGitAnon instance)
                : base(instance)
            {
                sock = instance.openConnection();
                try
                {
                    init(new NetworkStream(sock));
                    instance.service("git-upload-pack", pckOut);
                }
                catch (SocketException err)
                {
                    Close();
                    throw new TransportException(uri, "remote hung up unexpectedly", err);
                }
                readAdvertisedRefs();
            }

            public override void Close()
            {
                base.Close();

                if (sock != null)
                {
                    try
                    {
                        sock.Close();
                    }
                    catch (SocketException)
                    {
                        
                    }
                    finally
                    {
                        sock = null;
                    }
                }
            }
        }

        private class TcpPushConnection : BasePackPushConnection
        {
            private Socket sock;

            public TcpPushConnection(TransportGitAnon instance)
                : base(instance)
            {
                sock = instance.openConnection();
                try
                {
                    init(new NetworkStream(sock));
                    instance.service("git-receive-pack", pckOut);
                }
                catch (SocketException err)
                {
                    Close();
                    throw new TransportException(uri, "remote hung up unexpectedly", err);
                }
                readAdvertisedRefs();
            }

            public override void Close()
            {
                base.Close();

                if (sock != null)
                {
                    try
                    {
                        sock.Close();
                    }
                    catch (SocketException)
                    {
                        
                    }
                    finally
                    {
                        sock = null;
                    }
                }
            }
        }
    }
}
