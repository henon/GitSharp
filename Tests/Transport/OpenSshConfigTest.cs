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

using System.IO;
using GitSharp.Transport;
using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests.Transport
{
    [TestFixture]
    public class OpenSshConfigTest : RepositoryTestCase
    {
        private DirectoryInfo home;
        private FileInfo configFile;
        private OpenSshConfig osc;

        public override void setUp()
        {
            base.setUp();

            home = new DirectoryInfo(Path.Combine(trash.ToString(), "home"));
            configFile = new FileInfo(Path.Combine(home.ToString(), ".ssh"));
            Directory.CreateDirectory(configFile.ToString());

            configFile = new FileInfo(Path.Combine(configFile.ToString(), "config"));

            // can't do
            //Environment.UserName = "jex_junit";

            osc = new OpenSshConfig(home, configFile);
        }

        private void config(string data)
        {
            StreamWriter fw =
                new StreamWriter(
                    new FileStream(configFile.ToString(), System.IO.FileMode.Create, FileAccess.ReadWrite),
                    Encoding.UTF8);
            fw.Write(data);
            fw.Close();
        }

        [Test]
        public void testNoConfig()
        {
            OpenSshConfig.Host h = osc.lookup("repo.or.cz");
            Assert.IsNotNull(h);
            Assert.AreEqual("repo.or.cz", h.getHostName());
            Assert.AreEqual(Environment.UserName, h.getUser());
            Assert.AreEqual(22, h.getPort());
            Assert.IsNull(h.getIdentityFile());
        }

        [Test]
        public void testSeperatorParsing()
        {
            config("Host\tfirst\n" +
                   "\tHostName\tfirst.tld\n" +
                   "\n" +
                   "Host second\n" +
                   " HostName\tsecond.tld\n" +
                   "Host=third\n" +
                   "HostName=third.tld\n\n\n" +
                   "\t Host = fourth\n\n\n" +
                   " \t HostName\t=fourth.tld\n" +
                   "Host\t =     last\n" +
                   "HostName  \t    last.tld");

            Assert.IsNotNull(osc.lookup("first"));
            Assert.AreEqual("first.tld", osc.lookup("first").getHostName());
            Assert.IsNotNull(osc.lookup("second"));
            Assert.AreEqual("second.tld", osc.lookup("second").getHostName());
            Assert.IsNotNull(osc.lookup("third"));
            Assert.AreEqual("third.tld", osc.lookup("third").getHostName());
            Assert.IsNotNull(osc.lookup("fourth"));
            Assert.AreEqual("fourth.tld", osc.lookup("fourth").getHostName());
            Assert.IsNotNull(osc.lookup("last"));
            Assert.AreEqual("last.tld", osc.lookup("last").getHostName());
        }

        [Test]
        public void testQuoteParsing()
        {
            config("Host \"good\"\n" +
                   " HostName=\"good.tld\"\n" +
                   " Port=\"6007\"\n" +
                   " User=\"gooduser\"\n" +
                   "Host multiple unquoted and \"quoted\" \"hosts\"\n" +
                   " Port=\"2222\"\n" +
                   "Host \"spaced\"\n" +
                   "# Bad host name, but testing preservation of spaces\n" +
                   " HostName=\" spaced\ttld \"\n" +
                   "# Misbalanced quotes\n" +
                   "Host \"bad\"\n" +
                   "# OpenSSH doesn't allow this but ...\n" +
                   " HostName=bad.tld\"\n");

            Assert.AreEqual("good.tld", osc.lookup("good").getHostName());
            Assert.AreEqual("gooduser", osc.lookup("good").getUser());
            Assert.AreEqual(6007, osc.lookup("good").getPort());
            Assert.AreEqual(2222, osc.lookup("multiple").getPort());
            Assert.AreEqual(2222, osc.lookup("quoted").getPort());
            Assert.AreEqual(2222, osc.lookup("and").getPort());
            Assert.AreEqual(2222, osc.lookup("unquoted").getPort());
            Assert.AreEqual(2222, osc.lookup("hosts").getPort());
            Assert.AreEqual(" spaced\ttld ", osc.lookup("spaced").getHostName());
            Assert.AreEqual("bad.tld\"", osc.lookup("bad").getHostName());
        }
    }
}
