/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, JetBrains s.r.o.
 * Copyright (C) 2009, Google, Inc.
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
using GitSharp.Util;
using Tamir.SharpSsh.jsch;

namespace GitSharp.Transport
{

    public abstract class SshConfigSessionFactory : SshSessionFactory
    {
        private OpenSshConfig config;
        private readonly Dictionary<string, JSch> byIdentityFile = new Dictionary<string, JSch>();
        private JSch defaultJSch;

        public override Session getSession(string user, string pass, string host, int port)
        {
            OpenSshConfig.Host hc = getConfig().lookup(host);
            host = hc.getHostName();
            if (port <= 0)
                port = hc.getPort();
            if (user == null)
                user = hc.getUser();

            Session session = createSession(hc, user, host, port);
            if (pass != null)
                session.setPassword(pass);
            string strictHostKeyCheckingPolicy = hc.getStrictHostKeyChecking();
            //if (strictHostKeyCheckingPolicy != null)
            //    session.setConfig();
            configure(hc, session);
            return session;
        }

        protected Session createSession(OpenSshConfig.Host hc, string user, string host, int port)
        {
            return getJSch(hc).getSession(user, host, port);
        }

        protected abstract void configure(OpenSshConfig.Host hc, Session session);

        protected JSch getJSch(OpenSshConfig.Host hc)
        {
            JSch def = getDefaultJSch();
            FileInfo identityFile = hc.getIdentityFile();
            if (identityFile == null)
                return def;

            string identityKey = Path.GetFullPath(identityFile.ToString());
            JSch jsch = byIdentityFile[identityKey];
            if (jsch == null)
            {
                jsch = new JSch();
                jsch.setHostKeyRepository(def.getHostKeyRepository());
                jsch.addIdentity(identityKey);
                byIdentityFile.Add(identityKey, jsch);
            }
            return jsch;
        }

        private JSch getDefaultJSch()
        {
            if (defaultJSch == null)
            {
                defaultJSch = createDefaultJSch();
                // no identity file support
            }
            return defaultJSch;
        }

        protected static JSch createDefaultJSch()
        {
            JSch jsch = new JSch();
            knownHosts(jsch);
            identities(jsch);
            return jsch;
        }

        private OpenSshConfig getConfig()
        {
            if (config == null)
                config = OpenSshConfig.get();
            return config;
        }
           

        private static void knownHosts(JSch sch)
        {
            DirectoryInfo home = FS.userHome();
            if (home == null)
                return;
            FileInfo known_hosts = new FileInfo(Path.Combine(home.ToString(), ".ssh/known_hosts"));
            try
            {
                FileStream s = new FileStream(known_hosts.ToString(), System.IO.FileMode.Open, FileAccess.Read);
                try
                {
                    sch.setKnownHosts(new StreamReader(s));
                }
                finally
                {
                    s.Close();
                }
            }
            catch (FileNotFoundException)
            {
                
            }
            catch (IOException)
            {
                
            }
        }

        private static void identities(JSch sch)
        {
            DirectoryInfo home = FS.userHome();
            if (home == null)
                return;
            DirectoryInfo sshdir = new DirectoryInfo(Path.Combine(home.ToString(), ".ssh"));
            if (sshdir.Exists)
            {
                loadIdentity(sch, new FileInfo(Path.Combine(sshdir.ToString(), "identity")));
                loadIdentity(sch, new FileInfo(Path.Combine(sshdir.ToString(), "id_rsa")));
                loadIdentity(sch, new FileInfo(Path.Combine(sshdir.ToString(), "id_dsa")));
            }
        }

        private static void loadIdentity(JSch sch, FileInfo priv)
        {
            if (!File.Exists(priv.ToString())) return;
            try
            {
                sch.addIdentity(Path.GetFullPath(priv.ToString()));
            }
            catch (JSchException)
            {
                
            }
        }
    }

}