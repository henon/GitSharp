/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
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
using System.Collections.Generic;
using System.Linq;
using GitSharp.Core;
using GitSharp.Tests.GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core
{
    [TestFixture]
    public class RepositoryConfigTest
    {
        [Test]
        public void test001_ReadBareKey()
        {
            global::GitSharp.Core.Config c = parse("[foo]\nbar\n");
            Assert.AreEqual(true, c.getBoolean("foo", null, "bar", false));
            Assert.AreEqual(string.Empty, c.getString("foo", null, "bar"));
        }

        [Test]
        public void test002_ReadWithSubsection()
        {
            global::GitSharp.Core.Config c = parse("[foo \"zip\"]\nbar\n[foo \"zap\"]\nbar=false\nn=3\n");
            Assert.AreEqual(true, c.getBoolean("foo", "zip", "bar", false));
            Assert.AreEqual(string.Empty, c.getString("foo", "zip", "bar"));
            Assert.AreEqual(false, c.getBoolean("foo", "zap", "bar", true));
            Assert.AreEqual("false", c.getString("foo", "zap", "bar"));
            Assert.AreEqual(3, c.getInt("foo", "zap", "n", 4));
            Assert.AreEqual(4, c.getInt("foo", "zap", "m", 4));
        }

        [Test]
        public void test003_PutRemote()
        {
            global::GitSharp.Core.Config c = new global::GitSharp.Core.Config();
            c.setString("sec", "ext", "name", "value");
            c.setString("sec", "ext", "name2", "value2");
            string expText = "[sec \"ext\"]\n\tname = value\n\tname2 = value2\n";
            Assert.AreEqual(expText, c.toText());
        }

        [Test]
        public void test004_PutGetSimple()
        {
            global::GitSharp.Core.Config c = new global::GitSharp.Core.Config();
            c.setString("my", null, "somename", "false");
            Assert.AreEqual("false", c.getString("my", null, "somename"));
            Assert.AreEqual("[my]\n\tsomename = false\n", c.toText());
        }

        [Test]
        public void test005_PutGetStringList()
        {
            global::GitSharp.Core.Config c = new global::GitSharp.Core.Config();
            List<string> values = new List<string>();
            values.Add("value1");
            values.Add("value2");
            c.setStringList("my", null, "somename", values);

            object[] expArr = values.ToArray();
            string[] actArr = c.getStringList("my", null, "somename");
            Assert.IsTrue(expArr.SequenceEqual(actArr));

            string expText = "[my]\n\tsomename = value1\n\tsomename = value2\n";
            Assert.AreEqual(expText, c.toText());
        }

        [Test]
        public void test006_readCaseInsensitive()
        {
            global::GitSharp.Core.Config c = parse("[Foo]\nBar\n");
            Assert.AreEqual(true, c.getBoolean("foo", null, "bar", false));
            Assert.AreEqual(string.Empty, c.getString("foo", null, "bar"));
        }

        [Test]
        public void test007_readUserConfig()
        {
            MockSystemReader mockSystemReader = new MockSystemReader();
            SystemReader.setInstance(mockSystemReader);
            string hostname = mockSystemReader.getHostname();
            global::GitSharp.Core.Config userGitConfig = mockSystemReader.openUserConfig();
            global::GitSharp.Core.Config localConfig = new global::GitSharp.Core.Config(userGitConfig);
            mockSystemReader.clearProperties();

            string authorName;
            string authorEmail;

            // no values defined nowhere
            authorName = localConfig.get(UserConfig.KEY).getAuthorName();
            authorEmail = localConfig.get(UserConfig.KEY).getAuthorEmail();
            Assert.AreEqual(Constants.UNKNOWN_USER_DEFAULT, authorName);
            Assert.AreEqual(Constants.UNKNOWN_USER_DEFAULT + "@" + hostname, authorEmail);

            // the system user name is defined
            mockSystemReader.setProperty(Constants.OS_USER_NAME_KEY, "os user name");
            localConfig.uncache(UserConfig.KEY);
            authorName = localConfig.get(UserConfig.KEY).getAuthorName();
            Assert.AreEqual("os user name", authorName);

            if (hostname != null && hostname.Length != 0)
            {
                authorEmail = localConfig.get(UserConfig.KEY).getAuthorEmail();
                Assert.AreEqual("os user name@" + hostname, authorEmail);
            }

            // the git environment variables are defined
            mockSystemReader.setProperty(Constants.GIT_AUTHOR_NAME_KEY, "git author name");
            mockSystemReader.setProperty(Constants.GIT_AUTHOR_EMAIL_KEY, "author@email");
            localConfig.uncache(UserConfig.KEY);
            authorName = localConfig.get(UserConfig.KEY).getAuthorName();
            authorEmail = localConfig.get(UserConfig.KEY).getAuthorEmail();
            Assert.AreEqual("git author name", authorName);
            Assert.AreEqual("author@email", authorEmail);

            // the values are defined in the global configuration
            userGitConfig.setString("user", null, "name", "global username");
            userGitConfig.setString("user", null, "email", "author@globalemail");
            authorName = localConfig.get(UserConfig.KEY).getAuthorName();
            authorEmail = localConfig.get(UserConfig.KEY).getAuthorEmail();
            Assert.AreEqual("global username", authorName);
            Assert.AreEqual("author@globalemail", authorEmail);

            // the values are defined in the local configuration
            localConfig.setString("user", null, "name", "local username");
            localConfig.setString("user", null, "email", "author@localemail");
            authorName = localConfig.get(UserConfig.KEY).getAuthorName();
            authorEmail = localConfig.get(UserConfig.KEY).getAuthorEmail();
            Assert.AreEqual("local username", authorName);
            Assert.AreEqual("author@localemail", authorEmail);

            authorName = localConfig.get(UserConfig.KEY).getCommitterName();
            authorEmail = localConfig.get(UserConfig.KEY).getCommitterEmail();
            Assert.AreEqual("local username", authorName);
            Assert.AreEqual("author@localemail", authorEmail);
        }

        [Test]
        public void testReadBoolean_TrueFalse1()
        {
            global::GitSharp.Core.Config c = parse("[s]\na = true\nb = false\n");
            Assert.AreEqual("true", c.getString("s", null, "a"));
            Assert.AreEqual("false", c.getString("s", null, "b"));

            Assert.IsTrue(c.getBoolean("s", "a", false));
            Assert.IsFalse(c.getBoolean("s", "b", true));
        }

        [Test]
        public void testReadBoolean_TrueFalse2()
        {
            global::GitSharp.Core.Config c = parse("[s]\na = TrUe\nb = fAlSe\n");
            Assert.AreEqual("TrUe", c.getString("s", null, "a"));
            Assert.AreEqual("fAlSe", c.getString("s", null, "b"));

            Assert.IsTrue(c.getBoolean("s", "a", false));
            Assert.IsFalse(c.getBoolean("s", "b", true));
        }

        [Test]
        public void testReadBoolean_YesNo1()
        {
            global::GitSharp.Core.Config c = parse("[s]\na = yes\nb = no\n");
            Assert.AreEqual("yes", c.getString("s", null, "a"));
            Assert.AreEqual("no", c.getString("s", null, "b"));

            Assert.IsTrue(c.getBoolean("s", "a", false));
            Assert.IsFalse(c.getBoolean("s", "b", true));
        }

        [Test]
        public void testReadBoolean_YesNo2()
        {
            global::GitSharp.Core.Config c = parse("[s]\na = yEs\nb = NO\n");
            Assert.AreEqual("yEs", c.getString("s", null, "a"));
            Assert.AreEqual("NO", c.getString("s", null, "b"));

            Assert.IsTrue(c.getBoolean("s", "a", false));
            Assert.IsFalse(c.getBoolean("s", "b", true));
        }

        [Test]
        public void testReadBoolean_OnOff1()
        {
            global::GitSharp.Core.Config c = parse("[s]\na = on\nb = off\n");
            Assert.AreEqual("on", c.getString("s", null, "a"));
            Assert.AreEqual("off", c.getString("s", null, "b"));

            Assert.IsTrue(c.getBoolean("s", "a", false));
            Assert.IsFalse(c.getBoolean("s", "b", true));
        }

        [Test]
        public void testReadBoolean_OnOff2()
        {
            global::GitSharp.Core.Config c = parse("[s]\na = ON\nb = OFF\n");
            Assert.AreEqual("ON", c.getString("s", null, "a"));
            Assert.AreEqual("OFF", c.getString("s", null, "b"));

            Assert.IsTrue(c.getBoolean("s", "a", false));
            Assert.IsFalse(c.getBoolean("s", "b", true));
        }

        [Test]
        public void testReadLong()
        {
            assertReadLong(1L);
            assertReadLong(-1L);
            assertReadLong(long.MinValue);
            assertReadLong(long.MaxValue);
            assertReadLong(4L * 1024 * 1024 * 1024, "4g");
            assertReadLong(3L * 1024 * 1024, "3 m");
            assertReadLong(8L * 1024, "8 k");

            try
            {
                assertReadLong(-1, "1.5g");
                Assert.Fail("incorrectly accepted 1.5g");
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual("Invalid long value: s.a=1.5g", e.Message);
            }
        }

        [Test]
        public void testBooleanWithNoValue()
        {
            global::GitSharp.Core.Config c = parse("[my]\n\tempty\n");
            Assert.AreEqual("", c.getString("my", null, "empty"));
            Assert.AreEqual(1, c.getStringList("my", null, "empty").Length);
            Assert.AreEqual("", c.getStringList("my", null, "empty")[0]);
            Assert.IsTrue(c.getBoolean("my", "empty", false));
            Assert.AreEqual("[my]\n\tempty\n", c.toText());
        }

        [Test]
        public void testEmptyString()
        {
            global::GitSharp.Core.Config c = parse("[my]\n\tempty =\n");
            Assert.IsNull(c.getString("my", null, "empty"));

            String[] values = c.getStringList("my", null, "empty");
            Assert.IsNotNull(values);
            Assert.AreEqual(1, values.Length);
            Assert.IsNull(values[0]);

            // always matches the default, because its non-boolean
            Assert.IsTrue(c.getBoolean("my", "empty", true));
            Assert.IsFalse(c.getBoolean("my", "empty", false));

            Assert.AreEqual("[my]\n\tempty =\n", c.toText());

            c = new global::GitSharp.Core.Config();
            c.setStringList("my", null, "empty", values.ToList());
            Assert.AreEqual("[my]\n\tempty =\n", c.toText());
        }

        [Test]
        public void testUnsetBranchSection()
        {
            global::GitSharp.Core.Config c = parse(""
            + "[branch \"keep\"]\n"
            + "  merge = master.branch.to.keep.in.the.file\n"
            + "\n"
            + "[branch \"remove\"]\n"
            + "  merge = this.will.get.deleted\n"
            + "  remote = origin-for-some-long-gone-place\n"
            + "\n"
            + "[core-section-not-to-remove-in-test]\n"
            + "  packedGitLimit = 14\n");
            c.unsetSection("branch", "does.not.exist");
            c.unsetSection("branch", "remove");
            Assert.AreEqual("" //
                    + "[branch \"keep\"]\n"
                    + "  merge = master.branch.to.keep.in.the.file\n"
                    + "\n"
                    + "[core-section-not-to-remove-in-test]\n"
                    + "  packedGitLimit = 14\n", c.toText());
        }

        [Test]
        public void testUnsetSingleSection()
        {
            global::GitSharp.Core.Config c = parse("" //
                    + "[branch \"keep\"]\n"
                    + "  merge = master.branch.to.keep.in.the.file\n"
                    + "\n"
                    + "[single]\n"
                    + "  merge = this.will.get.deleted\n"
                    + "  remote = origin-for-some-long-gone-place\n"
                    + "\n"
                    + "[core-section-not-to-remove-in-test]\n"
                    + "  packedGitLimit = 14\n");
            c.unsetSection("single", null);
            Assert.AreEqual("" //
                    + "[branch \"keep\"]\n"
                    + "  merge = master.branch.to.keep.in.the.file\n"
                    + "\n"
                    + "[core-section-not-to-remove-in-test]\n"
                    + "  packedGitLimit = 14\n", c.toText());
        }
        private void assertReadLong(long exp)
        {
            assertReadLong(exp, exp.ToString());
        }

        private void assertReadLong(long exp, string act)
        {
            global::GitSharp.Core.Config c = parse("[s]\na = " + act + "\n");
            Assert.AreEqual(exp, c.getLong("s", null, "a", 0L));
        }

        private global::GitSharp.Core.Config parse(string content)
        {
            var c = new global::GitSharp.Core.Config(null);
            c.fromText(content);
            return c;
        }
    }
}