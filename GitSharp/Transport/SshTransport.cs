/*
 * Copyright (C) 2009, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, JetBrains s.r.o.
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
using GitSharp.Exceptions;
using Tamir.SharpSsh.jsch;

namespace GitSharp.Transport
{

    public abstract class SshTransport : TcpTransport
    {
        private SshSessionFactory sch;
        protected Session sock;

        protected SshTransport(Repository local, URIish uri)
            : base(local, uri)
        {
            sch = SshSessionFactory.Instance;
        }

        public void setSshSessionFactory(SshSessionFactory factory)
        {
            if (factory == null)
                throw new ArgumentException("The factory must not be null");
            if (sock != null)
                throw new ApplicationException("An SSH session has already been created");
            sch = factory;
        }

        public SshSessionFactory getSshSessionFactory()
        {
            return sch;
        }

        protected void initSesssion()
        {
            if (sock != null)
                return;

            string user = uri.User;
            string pass = uri.Pass;
            string host = uri.Host;
            int port = uri.Port;
            try
            {
                sock = sch.getSession(user, pass, host, port);
                if (!sock.isConnected())
                    sock.connect();
            }
            catch (JSchException je)
            {
                throw new TransportException(uri, je.Message, je.InnerException);
            }
            catch (SocketException e)
            {
                throw new TransportException(e.Message, e.InnerException ?? e);
            }
        }

        public override void close()
        {
            if (sock != null)
            {
                try
                {
                    sch.releaseSession(sock);
                }
                finally
                {
                    sock = null;
                }
            }
        }
    }
}