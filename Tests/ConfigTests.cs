/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using GitSharp.Tests.Util;
using Xunit;

namespace GitSharp.Tests
{
	public class ConfigTests : RepositoryTestCase
	{
		const string ConfigStr = "  [core];comment\n\tfilemode = yes\n"
						 + "[user]\n"
						 + "  email = A U Thor <thor@example.com> # Just an example...\n"
						 + " name = \"A  Thor \\\\ \\\"\\t \"\n"
						 + "    defaultCheckInComment = a many line\\n\\\ncomment\\n\\\n"
						 + " to test\n";

		[Fact]
		public void test004_CheckNewConfig()
		{
			RepositoryConfig c = db.Config;
			Assert.NotNull(c);
			Assert.Equal(Constants.RepositoryFormatVersion, c.getString("core", null, "repositoryformatversion"));
			Assert.Equal(Constants.RepositoryFormatVersion, c.getString("CoRe", null, "REPOSITORYFoRmAtVeRsIoN"));
			Assert.Equal("true", c.getString("core", null, "filemode"));
			Assert.Equal("true", c.getString("cOrE", null, "fIlEModE"));
			Assert.Null(c.getString("notavalue", null, "reallyNotAValue"));
			c.load();
		}

		[Fact]
		public void test005_ReadSimpleConfig()
		{
			RepositoryConfig c = db.Config;
			Assert.NotNull(c);
			c.load();
			Assert.Equal(Constants.RepositoryFormatVersion, c.getString("core", null, "repositoryformatversion"));
			Assert.Equal(Constants.RepositoryFormatVersion, c.getString("CoRe", null, "REPOSITORYFoRmAtVeRsIoN"));
			Assert.Equal("true", c.getString("core", null, "filemode"));
			Assert.Equal("true", c.getString("cOrE", null, "fIlEModE"));
			Assert.Null(c.getString("notavalue", null, "reallyNotAValue"));
		}

		[Fact]
		public void test006_ReadUglyConfig()
		{
			RepositoryConfig c = db.Config;
			string cfg = c.getFile().FullName; // db.Directory.FullName + "config";
			//FileWriter pw = new FileWriter(cfg);

			File.WriteAllText(cfg, ConfigStr);
			c.load();
			Assert.Equal("yes", c.getString("core", null, "filemode"));
			Assert.Equal("A U Thor <thor@example.com>", c
					.getString("user", null, "email"));
			Assert.Equal("A  Thor \\ \"\t ", c.getString("user", null, "name"));
			Assert.Equal("a many line\ncomment\n to test", c.getString("user", null, "defaultCheckInComment"));
			c.save();
			var configStr1 = File.ReadAllText(cfg);
			Assert.Equal(ConfigStr, configStr1);
		}

		[Fact]
		public void test007_Open()
		{
			var db2 = new Repository(db.Directory);
			Assert.Equal(db.Directory, db2.Directory);
			Assert.Equal(db.ObjectsDirectory.FullName, db2.ObjectsDirectory.FullName);
			Assert.NotSame(db.Config, db2.Config);
		}

		[Fact]
		public void test008_FailOnWrongVersion()
		{
			string cfg = db.Directory.FullName + "/config";

			const string badvers = "ihopethisisneveraversion";
			const string configStr = "[core]\n" + "\trepositoryFormatVersion="
			                         + badvers + "\n";
			File.WriteAllText(cfg, configStr);

			try
			{
				new Repository(db.Directory);
				Assert.False(true, "incorrectly opened a bad repository");
			}
			catch (IOException ioe)
			{
				Assert.True(ioe.Message.IndexOf("format") > 0);
				Assert.True(ioe.Message.IndexOf(badvers) > 0);
			}
		}
	}
}
