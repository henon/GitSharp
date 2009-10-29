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
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using GitSharp.Core.Exceptions;
using GitSharp.Core.FnMatch;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{

    public class OpenSshConfig
    {
        public const int SSH_PORT = 22;
		private Object locker = new Object();

        public static OpenSshConfig get()
        {
            DirectoryInfo home = FS.userHome() ?? new DirectoryInfo(Path.GetFullPath("."));

            FileInfo config = new FileInfo(Path.Combine(home.FullName, ".ssh" + Path.DirectorySeparatorChar + "config"));
            OpenSshConfig osc = new OpenSshConfig(home, config);
            osc.refresh();
            return osc;
        }

        private readonly DirectoryInfo home;
        private readonly FileInfo configFile;
        private long lastModified;
        private Dictionary<string, Host> hosts;

        public OpenSshConfig(DirectoryInfo home, FileInfo cfg)
        {
            this.home = home;
            configFile = cfg;
            hosts = new Dictionary<string, Host>();
        }

        public Host lookup(string hostName)
        {
            Dictionary<string, Host> cache = refresh();
            Host h = null;
            if (cache.ContainsKey(hostName))
            {
                h = cache[hostName];
            }
            if (h == null)
                h = new Host();
            if (h.patternsApplied)
                return h;

            foreach (string k in cache.Keys)
            {
                if (!isHostPattern(k))
                    continue;
                if (!isHostMatch(k, hostName))
                    continue;
                h.copyFrom(cache[k]);
            }

            if (h.getHostName() == null)
                h.hostName = hostName;
            if (h.user == null)
                h.user = userName();
            if (h.port == 0)
                h.port = SSH_PORT;
            h.patternsApplied = true;
            return h;
        }

        private Dictionary<string, Host> refresh()
        {
			lock(locker)
			{
	            long mtime = configFile.LastWriteTime.ToBinary();
	            if (mtime != lastModified)
	            {
	                try
	                {
	                    FileStream s = new FileStream(configFile.FullName, System.IO.FileMode.Open, FileAccess.Read);
	                    try
	                    {
	                        hosts = parse(s);
	                    }
	                    finally
	                    {
	                        s.Close();
	                    }
	                }
	                catch (FileNotFoundException)
	                {
	                    hosts = new Dictionary<string, Host>();
	                }
	                catch (IOException)
	                {
	                    hosts = new Dictionary<string, Host>();
	                }
	                lastModified = mtime;
	            }
	            return hosts;
			}
        }

        private Dictionary<string, Host> parse(Stream stream)
        {
            Dictionary<string, Host> m = new Dictionary<string, Host>();
            StreamReader sr = new StreamReader(stream);
            List<Host> current = new List<Host>(4);
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                string keyword = string.Empty, argValue = string.Empty;
                bool haveKeyword = false;
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (!haveKeyword && (c == ' ' || c == '\t' || c == '='))
                    {
                        keyword = line.Slice(0, i);
                        haveKeyword = true;
                        continue;
                    }

                    if (haveKeyword && c != '=' && c != ' ' && c != '\t')
                    {
                        argValue = line.Substring(i);
                        break;
                    }
                }

                if ("Host".Equals(keyword, StringComparison.CurrentCultureIgnoreCase))
                    {
                        current.Clear();
                        foreach (string pattern in Regex.Split(argValue, "[ \t]"))
                        {
                            string name = dequote(pattern);
                            Host c = null;
                            if (m.ContainsKey(name))
                            {
                                c = m[name];
                            }
                            if (c == null)
                            {
                                c = new Host();
                                m.Add(name, c);
                            }
                            current.Add(c);
                        }
                    }

                if (current.Count == 0)
                {
                    continue;
                }

                if ("HostName".Equals(keyword, StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (Host c in current)
                        if (c.hostName == null)
                            c.hostName = dequote(argValue);
                }
                else if ("User".Equals(keyword, StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (Host c in current)
                        if (c.user == null)
                            c.user = dequote(argValue);
                }
                else if ("Port".Equals(keyword, StringComparison.CurrentCultureIgnoreCase))
                {
                    try
                    {
                        int port = int.Parse(dequote(argValue));
                        foreach (Host c in current)
                            if (c.port == 0)
                                c.port = port;
                    }
                    catch (FormatException)
                    {
                        
                    }
                }
                else if ("IdentityFile".Equals(keyword, StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (Host c in current)
                        if (c.identityFile == null)
                            c.identityFile = toFile(dequote(argValue));
                }
                else if ("PreferredAuthentications".Equals(keyword, StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (Host c in current)
                        if (c.preferredAuthentications == null)
                            c.preferredAuthentications = nows(dequote(argValue));
                }
                else if ("BatchMode".Equals(keyword, StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (Host c in current)
                        if (c.batchMode == null)
                            c.batchMode = yesno(dequote(argValue));
                }
                else if ("StrictHostKeyChecking".Equals(keyword, StringComparison.CurrentCultureIgnoreCase))
                {
                    string value = dequote(argValue);
                    foreach (Host c in current)
                        if (c.strictHostKeyChecking == null)
                            c.strictHostKeyChecking = value;
                }
            }

            return m;
        }

        private static bool isHostPattern(string s)
        {
            return s.IndexOf('*') >= 0 || s.IndexOf('?') >= 0;
        }

        private static bool isHostMatch(string pattern, string name)
        {
            FileNameMatcher fn;
            try
            {
                fn = new FileNameMatcher(pattern, null);
            }
            catch (InvalidPatternException)
            {
                return false;
            }
            fn.Append(name);
            return fn.IsMatch();
        }

        private static string dequote(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return value.Slice(1, value.Length - 1);
            return value;
        }

        private static string nows(string value)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
                if (value[i] != ' ')
                    b.Append(value[i]);
            return b.ToString();
        }

        private static bool yesno(string value)
        {
            if ("yes".Equals(value, StringComparison.CurrentCultureIgnoreCase))
                return true;
            return false;
        }

        public static string userName()
        {
            return Environment.UserName;
        }

        private FileInfo toFile(string path)
        {
            if (path.StartsWith("~/"))
            {
                return new FileInfo(Path.Combine(home.FullName, path.Substring(2)));
            }

            if (Path.IsPathRooted(path))
            {
                return new FileInfo(path);
            }

            return new FileInfo(Path.Combine(home.FullName, path));
        }

        public class Host
        {
            public bool patternsApplied;
            public string hostName;
            public string user;
            public string preferredAuthentications;
            public int port;
            public FileInfo identityFile;
            public bool? batchMode;
            public string strictHostKeyChecking;

            public void copyFrom(Host src)
            {
                if (hostName == null)
                    hostName = src.hostName;
                if (port == 0)
                    port = src.port;
                if (identityFile == null)
                    identityFile = src.identityFile;
                if (user == null)
                    user = src.user;
                if (preferredAuthentications == null)
                    preferredAuthentications = src.preferredAuthentications;
                if (batchMode == null)
                    batchMode = src.batchMode;
                if (strictHostKeyChecking == null)
                    strictHostKeyChecking = src.strictHostKeyChecking;
            }

            public string getStrictHostKeyChecking()
            {
                return strictHostKeyChecking;
            }

            public string getHostName()
            {
                return hostName;
            }

            public int getPort()
            {
                return port;
            }

            public FileInfo getIdentityFile()
            {
                return identityFile;
            }

            public string getUser()
            {
                return user;
            }

            public string getPreferredAuthentications()
            {
                return preferredAuthentications;
            }

            public bool isBatchMode()
            {
                return batchMode != null && batchMode.Value;
            }
        }
    }

}