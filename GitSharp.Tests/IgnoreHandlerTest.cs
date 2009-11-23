/*
 * Copyright (C) 2009, Stefan Schake <caytchen@gmail.com>
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
using NUnit.Framework;

namespace GitSharp.Tests
{

    [TestFixture]
    public class IgnoreHandlerTests : RepositoryTestCase
    {
        private IgnoreHandler _handler;

        [Test]
        public void HonorsExcludeFile()
        {
            WriteExclude("*.html");
            _handler = new IgnoreHandler(db);

            Assert.IsTrue(_handler.IsIgnored("test.html"));
        }

        [Test]
        public void HonorsConfigExcludes()
        {
            WriteConfigExcludes("ignoreHandler", "*.a");
            _handler = new IgnoreHandler(db);

            Assert.IsTrue(_handler.IsIgnored("test.a"));
        }

        [Test]
        public void HonorsTopLevelIgnore()
        {
            WriteIgnore(".", "*.o");
            _handler = new IgnoreHandler(db);

            Assert.IsTrue(_handler.IsIgnored("test.o"));
        }

        [Test]
        public void TestNegated()
        {
            WriteIgnore(".", "*.o");
            WriteIgnore("test", "!*.o");
            _handler = new IgnoreHandler(db);

            Assert.IsFalse(_handler.IsIgnored("test/test.o"));
        }

        private void WriteExclude(string data)
        {
            writeTrashFile(".git/info/exclude", data);
        }

        private void WriteConfigExcludes(string path, string data)
        {
            db.Config.setString("core", null, "excludesfile", path);
            writeTrashFile(path, data);
        }

        private void WriteIgnore(string dir, string data)
        {
            writeTrashFile(Path.Combine(dir, Constants.GITIGNORE_FILENAME), data);
        }
    }

}