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
using GitSharp.Core;
using GitSharp.Core.Transport;
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
		private DirectoryInfo _home;
		private FileInfo _configFile;
		private OpenSshConfig _osc;

		public override void setUp()
		{
			base.setUp();

			_home = new DirectoryInfo(Path.Combine(trash.FullName, "home"));
			_configFile = new FileInfo(Path.Combine(_home.FullName, ".ssh"));
			Directory.CreateDirectory(_configFile.FullName);

			_configFile = new FileInfo(Path.Combine(_configFile.FullName, "config"));

			// can't do
			//Environment.UserName = "jex_junit";

			_osc = new OpenSshConfig(_home, _configFile);
		}

		private void Config(string data)
		{
			using (var fw = new StreamWriter(new FileStream(_configFile.FullName, System.IO.FileMode.Create, FileAccess.ReadWrite), Constants.CHARSET))
			{
			    fw.Write(data);
			}
		}

		[Test]
		public void testNoConfig()
		{
			OpenSshConfig.Host h = _osc.lookup("repo.or.cz");
			Assert.IsNotNull(h);
			Assert.AreEqual("repo.or.cz", h.getHostName());
			Assert.AreEqual(Environment.UserName, h.getUser());
			Assert.AreEqual(22, h.getPort());
			Assert.IsNull(h.getIdentityFile());
		}

		[Test]
		public void testSeperatorParsing()
		{
			Config("Host\tfirst\n" +
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

			Assert.IsNotNull(_osc.lookup("first"));
			Assert.AreEqual("first.tld", _osc.lookup("first").getHostName());
			Assert.IsNotNull(_osc.lookup("second"));
			Assert.AreEqual("second.tld", _osc.lookup("second").getHostName());
			Assert.IsNotNull(_osc.lookup("third"));
			Assert.AreEqual("third.tld", _osc.lookup("third").getHostName());
			Assert.IsNotNull(_osc.lookup("fourth"));
			Assert.AreEqual("fourth.tld", _osc.lookup("fourth").getHostName());
			Assert.IsNotNull(_osc.lookup("last"));
			Assert.AreEqual("last.tld", _osc.lookup("last").getHostName());
		}

		[Test]
		public void testQuoteParsing()
		{
			Config("Host \"good\"\n" +
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

			Assert.AreEqual("good.tld", _osc.lookup("good").getHostName());
			Assert.AreEqual("gooduser", _osc.lookup("good").getUser());
			Assert.AreEqual(6007, _osc.lookup("good").getPort());
			Assert.AreEqual(2222, _osc.lookup("multiple").getPort());
			Assert.AreEqual(2222, _osc.lookup("quoted").getPort());
			Assert.AreEqual(2222, _osc.lookup("and").getPort());
			Assert.AreEqual(2222, _osc.lookup("unquoted").getPort());
			Assert.AreEqual(2222, _osc.lookup("hosts").getPort());
			Assert.AreEqual(" spaced\ttld ", _osc.lookup("spaced").getHostName());
			Assert.AreEqual("bad.tld\"", _osc.lookup("bad").getHostName());
		}

		[Test]
		public void testAlias_DoesNotMatch()
		{
			Config("Host orcz\n" + "\tHostName repo.or.cz\n");
			OpenSshConfig.Host h = _osc.lookup("repo.or.cz");
			Assert.IsNotNull(h);
			Assert.AreEqual("repo.or.cz", h.getHostName());
			Assert.AreEqual(Environment.UserName, h.getUser());
			Assert.AreEqual(22, h.port);
			Assert.IsNull(h.getIdentityFile());
		}

		[Test]
		public void testAlias_OptionsSet()
		{
			Config("Host orcz\n" + "\tHostName repo.or.cz\n" + "\tPort 2222\n"
				   + "\tUser jex\n" + "\tIdentityFile .ssh/id_jex\n"
				   + "\tForwardX11 no\n");

			OpenSshConfig.Host h = _osc.lookup("orcz");
			Assert.IsNotNull(h);
			Assert.AreEqual("repo.or.cz", h.getHostName());
			Assert.AreEqual("jex", h.getUser());
			Assert.AreEqual(2222, h.getPort());
			Assert.AreEqual(new FileInfo(Path.Combine(_home.FullName, ".ssh/id_jex")).FullName, h.getIdentityFile().FullName);
		}

		[Test]
		public void testAlias_OptionsKeywordCaseInsensitive()
		{
			Config("hOsT orcz\n" + "\thOsTnAmE repo.or.cz\n" + "\tPORT 2222\n"
				   + "\tuser jex\n" + "\tidentityfile .ssh/id_jex\n"
				   + "\tForwardX11 no\n");

			OpenSshConfig.Host h = _osc.lookup("orcz");
			Assert.IsNotNull(h);
			Assert.AreEqual("repo.or.cz", h.getHostName());
			Assert.AreEqual("jex", h.getUser());
			Assert.AreEqual(2222, h.getPort());
			Assert.AreEqual(new FileInfo(Path.Combine(_home.FullName, ".ssh/id_jex")).FullName, h.getIdentityFile().FullName);
		}

		[Test]
		public void testAlias_OptionsInherit()
		{
			Config("Host orcz\n" + "\tHostName repo.or.cz\n" + "\n" + "Host *\n"
				   + "\tHostName not.a.host.example.com\n" + "\tPort 2222\n"
				   + "\tUser jex\n" + "\tIdentityFile .ssh/id_jex\n"
				   + "\tForwardX11 no\n");

			OpenSshConfig.Host h = _osc.lookup("orcz");
			Assert.IsNotNull(h);
			Assert.AreEqual("repo.or.cz", h.getHostName());
			Assert.AreEqual("jex", h.getUser());
			Assert.AreEqual(2222, h.getPort());
			Assert.AreEqual(new FileInfo(Path.Combine(_home.FullName, ".ssh/id_jex")).FullName, h.getIdentityFile().FullName);
		}

		[Test]
		public void testAlias_PreferredAuthenticationsDefault()
		{
			OpenSshConfig.Host h = _osc.lookup("orcz");
			Assert.IsNotNull(h);
			Assert.IsNull(h.getPreferredAuthentications());
		}

		[Test]
		public void testAlias_PreferredAuthentications()
		{
			Config("Host orcz\n" + "\tPreferredAuthentications publickey\n");
			OpenSshConfig.Host h = _osc.lookup("orcz");
			Assert.IsNotNull(h);
			Assert.AreEqual("publickey", h.getPreferredAuthentications());
		}

		[Test]
		public void testAlias_InheritPreferredAuthentications()
		{
			Config("Host orcz\n" + "\tHostName repo.or.cz\n" + "\n" + "Host *\n"
				   + "\tPreferredAuthentications publickey, hostbased\n");
			OpenSshConfig.Host h = _osc.lookup("orcz");
			Assert.IsNotNull(h);
			Assert.AreEqual("publickey,hostbased", h.getPreferredAuthentications());
		}

		[Test]
		public void testAlias_BatchModeDefault()
		{
			OpenSshConfig.Host h = _osc.lookup("orcz");
			Assert.IsNotNull(h);
			Assert.AreEqual(false, h.isBatchMode());
		}

		[Test]
		public void testAlias_BatchModeYes()
		{
			Config("Host orcz\n" + "\tBatchMode yes\n");
			OpenSshConfig.Host h = _osc.lookup("orcz");
			Assert.IsNotNull(h);
			Assert.AreEqual(true, h.isBatchMode());
		}

		[Test]
		public void testAlias_InheritBatchMode()
		{
			Config("Host orcz\n" + "\tHostName repo.or.cz\n" + "\n" + "Host *\n"
				   + "\tBatchMode yes\n");
			OpenSshConfig.Host h = _osc.lookup("orcz");
			Assert.IsNotNull(h);
			Assert.AreEqual(true, h.isBatchMode());
		}
	}
}
