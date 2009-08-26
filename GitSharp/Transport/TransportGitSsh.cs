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
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using GitSharp.Exceptions;
using GitSharp.Util;
using Tamir.SharpSsh.jsch;

namespace GitSharp.Transport
{
    public class TransportGitSsh : SshTransport, IPackTransport
    {
        public static bool canHandle(URIish uri)
        {
            if (!uri.IsRemote)
            {
                return false;
            }

            string scheme = uri.Scheme;
            
            if ("ssh".Equals(scheme))
            {
                return true;
            }
            
            if ("ssh+git".Equals(scheme))
            {
                return true;
            }
            
            if ("git+ssh".Equals(scheme))
            {
                return true;
            }
            
            if (scheme == null && uri.Host != null && uri.Path != null)
            {
                return true;
            }
            
            return false;
        }

        private Stream errStream;

        public TransportGitSsh(Repository local, URIish uri)
            : base(local, uri)
        {
        }

        public override IFetchConnection openFetch()
        {
            return new SshFetchConnection(this);
        }

        public override IPushConnection openPush()
        {
            return new SshPushConnection(this);
        }

        private static void sqMinimal(StringBuilder cmd, string val)
        {
            if (Regex.Matches(val, "^[a-zA-Z0-9._/-]*$").Count > 0)
            {
                cmd.Append(val);
            }
            else
            {
                sq(cmd, val);
            }
        }

        private static void sqAlways(StringBuilder cmd, string val)
        {
            sq(cmd, val);
        }

        private static void sq(StringBuilder cmd, string val)
        {
            if (val.Length > 0)
                cmd.Append(QuotedString.BOURNE.quote(val));
        }

        public ChannelExec exec(string exe)
        {
            initSession();

            try
            {
                ChannelExec channel = (ChannelExec) sock.openChannel("exec");
                string path = uri.Path;
                if (uri.Scheme != null && uri.Path.StartsWith("/~"))
                    path = (uri.Path.Substring(1));

                StringBuilder cmd = new StringBuilder();
                int gitspace = exe.IndexOf("git ");
                if (gitspace >= 0)
                {
                    sqMinimal(cmd, exe.Slice(0, gitspace + 3));
                    cmd.Append(' ');
                    sqMinimal(cmd, exe.Substring(gitspace + 4));
                }
                else
                    sqMinimal(cmd, exe);
                cmd.Append(' ');
                sqAlways(cmd, path);
                channel.setCommand(cmd.ToString());
                errStream = createErrorStream();
                channel.setErrStream(errStream);
                channel.connect();
                return channel;
            }
            catch (JSchException e)
            {
                throw new TransportException(uri, e.Message, e);
            }
        }

        private class GitSshErrorStream : MemoryStream
        {
            private readonly StringBuilder all = new StringBuilder();

            public override void Write(byte[] buffer, int offset, int count)
            {
                for (int i = offset; i < count + offset; i++)
                {
                    if (buffer[i] == '\n')
                    {
                        string line = Constants.CHARSET.GetString(ToArray());
                        Console.WriteLine(line);
                        all.AppendLine(line);
                        SetLength(0);
                        Write(buffer, offset + (i - offset), count - (i - offset));
                        return;
                    }
                    WriteByte(buffer[i]);
                }
                base.Write(buffer, offset, count);
            }

            public override string ToString()
            {
                return all + "\n" + Constants.CHARSET.GetString(ToArray());
            }
        }

        private static Stream createErrorStream()
        {
            return new GitSshErrorStream();
        }

        public NoRemoteRepositoryException cleanNotFound(NoRemoteRepositoryException nf)
        {
            string why = errStream.ToString();
            if (string.IsNullOrEmpty(why))
                return nf;

            string path = uri.Path;
            if (uri.Scheme != null && uri.Path.StartsWith("/~"))
                path = uri.Path.Substring(1);

            StringBuilder pfx = new StringBuilder();
            pfx.Append("fatal: ");
            sqAlways(pfx, path);
            pfx.Append(":");
            if (why.StartsWith(pfx.ToString()))
                why = why.Substring(pfx.Length);

            return new NoRemoteRepositoryException(uri, why);
        }

        private class SshFetchConnection :  BasePackFetchConnection
        {
            private ChannelExec channel;

            public SshFetchConnection(TransportGitSsh instance)
                : base(instance)
            {
                try
                {
                    channel = instance.exec(instance.OptionUploadPack);

                    if (channel.isConnected())
                        init(channel.getOutputStream());
                    else
                        throw new TransportException(uri, instance.errStream.ToString());
                }
                catch (TransportException err)
                {
                    Close();
                    throw err;
                }
                catch (SocketException err)
                {
                    Close();
                    throw new TransportException(uri, "remote hung up unexpectedly", err);
                }

                try
                {
                    readAdvertisedRefs();
                }
                catch (NoRemoteRepositoryException notFound)
                {
                    throw instance.cleanNotFound(notFound);
                }
            }

            public override void Close()
            {
                base.Close();

                if (channel != null)
                {
                    try
                    {
                        if (channel.isConnected())
                            channel.disconnect();
                    }
                    finally
                    {
                        channel = null;
                    }
                }
            }
        }

        private class SshPushConnection : BasePackPushConnection
        {
            private ChannelExec channel;

            public SshPushConnection(TransportGitSsh instance)
                : base(instance)
            {
                try
                {
                    channel = instance.exec(instance.OptionReceivePack);

                    if (channel.isConnected())
                        init(channel.getOutputStream());
                    else
                        throw new TransportException(uri, instance.errStream.ToString());
                }
                catch (TransportException err)
                {
                    Close();
                    throw err;
                }
                catch (SocketException err)
                {
                    Close();
                    throw new TransportException(uri, "remote hung up unexpectedly", err);
                }

                try
                {
                    readAdvertisedRefs();
                }
                catch (NoRemoteRepositoryException notFound)
                {
                    throw instance.cleanNotFound(notFound);
                }
            }

            public override void Close()
            {
                base.Close();

                if (channel != null)
                {
                    try
                    {
                        if (channel.isConnected())
                            channel.disconnect();
                    }
                    finally
                    {
                        channel = null;
                    }
                }
            }
        }
    }
}
