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
using GitSharp.Core.Exceptions;
using Tamir.SharpSsh.jsch;

namespace GitSharp.Core.Transport
{
    public abstract class SshTransport : TcpTransport
    {
        private SshSessionFactory _sch;
        private Session _sock;

    	protected SshTransport(Repository local, URIish uri)
            : base(local, uri)
        {
            _sch = SshSessionFactory.Instance;
        }

		public Session Sock
		{
			get { return _sock; }
		}

    	public SshSessionFactory SshSessionFactory
    	{
			get { return _sch; }
			set
			{
				if (value == null)
				{
					throw new ArgumentException("The factory must not be null");
				}

				if (_sock != null)
				{
					throw new ApplicationException("An SSH session has already been created");
				}

				_sch = value;
			}
    	}

        protected void InitSession()
        {
            if (_sock != null) return;

			string user = Uri.User;
			string pass = Uri.Pass;
			string host = Uri.Host;
			int port = Uri.Port;
            try
            {
                _sock = _sch.getSession(user, pass, host, port);
                if (!_sock.isConnected())
                {
                	_sock.connect();
                }
            }
            catch (JSchException je)
            {
				throw new TransportException(Uri, je.Message, je.InnerException);
            }
            catch (SocketException e)
            {
                throw new TransportException(e.Message, e.InnerException ?? e);
            }
        }

        public override void close()
        {
            if (_sock == null) return;

			try
			{
				_sch.releaseSession(_sock);
			}
			finally
			{
				_sock = null;
			}

#if DEBUG
            GC.SuppressFinalize(this); // Disarm lock-release checker
#endif
        }

#if DEBUG
        // A debug mode warning if the type has not been disposed properly
        ~SshTransport()
        {
            Console.Error.WriteLine(GetType().Name + " has not been properly disposed: {" + Local.Directory + "}/{" + Uri + "}");
        }
#endif
    }
}