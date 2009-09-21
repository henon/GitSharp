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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;

namespace GitSharp.Tests
{
    [TestFixture]
    public class T0003_Basic_Config : RepositoryTestCase
    {
        [Test]
        public void test004_CheckNewConfig()
        {
            RepositoryConfig c = db.Config;
            Assert.IsNotNull(c);
            Assert.AreEqual("0", c.getString("core", null, "repositoryformatversion"));
            Assert.AreEqual("0", c.getString("CoRe", null, "REPOSITORYFoRmAtVeRsIoN"));
            Assert.AreEqual("true", c.getString("core", null, "filemode"));
            Assert.AreEqual("true", c.getString("cOrE", null, "fIlEModE"));
            Assert.IsNull(c.getString("notavalue", null, "reallyNotAValue"));
            c.load();
        }

        [Test]
        public void test005_ReadSimpleConfig()
        {
            RepositoryConfig c = db.Config;
            Assert.IsNotNull(c);
            c.load(); //  (nulltoken] Not sure about this, which doesn't appear in original jgit code. Howver removing thi statement would make test004 and test005 performing the same tests... :-|
            Assert.AreEqual("0", c.getString("core", null, "repositoryformatversion"));
            Assert.AreEqual("0", c.getString("CoRe", null, "REPOSITORYFoRmAtVeRsIoN"));
            Assert.AreEqual("true", c.getString("core", null, "filemode"));
            Assert.AreEqual("true", c.getString("cOrE", null, "fIlEModE"));
            Assert.IsNull(c.getString("notavalue", null, "reallyNotAValue"));
        }

        [Test]
        public void test006_ReadUglyConfig()
        {
            RepositoryConfig c = db.Config;
            string cfg = Path.Combine(db.Directory.FullName, "config");
            
            string configStr = "  [core];comment\n\tfilemode = yes\n"
                   + "[user]\n"
                   + "  email = A U Thor <thor@example.com> # Just an example...\n"
                   + " name = \"A  Thor \\\\ \\\"\\t \"\n"
                   + "    defaultCheckInComment = a many line\\n\\\ncomment\\n\\\n"
                   + " to test\n";
            File.WriteAllText(cfg, configStr);
            c.load();
            Assert.AreEqual("yes", c.getString("core", null, "filemode"));
            Assert.AreEqual("A U Thor <thor@example.com>", c
                    .getString("user", null, "email"));
            Assert.AreEqual("A  Thor \\ \"\t ", c.getString("user", null, "name"));
            Assert.AreEqual("a many line\ncomment\n to test", c.getString("user", null, "defaultCheckInComment"));
            c.save();
            var configStr1 = File.ReadAllText(cfg);
            Assert.AreEqual(configStr, configStr1);
        }

        [Test]
        public void test007_Open()
        {
            Repository db2 = new Repository(db.Directory);
            Assert.AreEqual(db.Directory, db2.Directory);
            Assert.AreEqual(db.ObjectsDirectory.FullName, db2.ObjectsDirectory.FullName);
            Assert.AreNotSame(db.Config, db2.Config);
        }

        [Test]
        public void test008_FailOnWrongVersion()
        {
            string cfg = db.Directory.FullName + "/config";

            string badvers = "ihopethisisneveraversion";
            string configStr = "[core]\n" + "\trepositoryFormatVersion="
                               + badvers + "\n";
            File.WriteAllText(cfg, configStr);

            var ioe = AssertHelper.Throws<IOException>(() =>
                                                           {
                                                               new Repository(db.Directory);
                                                               Assert.Fail("incorrectly opened a bad repository");
                                                           }
                );

            Assert.IsTrue(ioe.Message.IndexOf("format") > 0);
            Assert.IsTrue(ioe.Message.IndexOf(badvers) > 0);
        }
    }
}
