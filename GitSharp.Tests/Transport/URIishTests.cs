/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using GitSharp.Core.Transport;
using NUnit.Framework;

namespace GitSharp.Tests.Transport
{
    [TestFixture]
    public class URIishTest
    {
        [Test]
        public void testUnixFile()
        {
            const string str = "/home/m y";
            var u = new URIish(str);
            Assert.IsNull(u.Scheme);
            Assert.IsFalse(u.IsRemote);
            Assert.AreEqual(str, u.Path);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testWindowsFile()
        {
            const string str = "D:/m y";
            URIish u = new URIish(str);
            Assert.IsNull(u.Scheme);
            Assert.IsFalse(u.IsRemote);
            Assert.AreEqual(str, u.Path);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testWindowsFile2()
        {
            const string str = "D:\\m y";
            var u = new URIish(str);
            Assert.IsNull(u.Scheme);
            Assert.IsFalse(u.IsRemote);
            Assert.AreEqual("D:/m y", u.Path);
            Assert.AreEqual("D:/m y", u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testUNC()
        {
            const string str = "\\\\some\\place";
            var u = new URIish(str);
            Assert.IsNull(u.Scheme);
            Assert.IsFalse(u.IsRemote);
            Assert.AreEqual("//some/place", u.Path);
            Assert.AreEqual("//some/place", u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testFileProtoUnix()
        {
            const string str = "file:///home/m y";
            var u = new URIish(str);
            Assert.AreEqual("file", u.Scheme);
            Assert.IsFalse(u.IsRemote);
            Assert.AreEqual("/home/m y", u.Path);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testFileProtoWindows()
        {
            const string str = "file:///D:/m y";
            var u = new URIish(str);
            Assert.AreEqual("file", u.Scheme);
            Assert.IsFalse(u.IsRemote);
            Assert.AreEqual("D:/m y", u.Path);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testGitProtoUnix()
        {
            const string str = "git://example.com/home/m y";
            var u = new URIish(str);
            Assert.AreEqual("git", u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual("/home/m y", u.Path);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testGitProtoUnixPort()
        {
            const string str = "git://example.com:333/home/m y";
            var u = new URIish(str);
            Assert.AreEqual("git", u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual("/home/m y", u.Path);
            Assert.AreEqual(333, u.Port);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testGitProtoWindowsPort()
        {
            const string str = "git://example.com:338/D:/m y";
            var u = new URIish(str);
            Assert.AreEqual("git", u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("D:/m y", u.Path);
            Assert.AreEqual(338, u.Port);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testGitProtoWindows()
        {
            const string str = "git://example.com/D:/m y";
            var u = new URIish(str);
            Assert.AreEqual("git", u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("D:/m y", u.Path);
            Assert.AreEqual(-1, u.Port);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testScpStyleWithoutUser()
        {
            const string str = "example.com:some/p ath";
            var u = new URIish(str);
            Assert.IsNull(u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("some/p ath", u.Path);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual(-1, u.Port);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testScpStyleWithUser()
        {
            const string str = "user@example.com:some/p ath";
            var u = new URIish(str);
            Assert.IsNull(u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("some/p ath", u.Path);
            Assert.AreEqual("user", u.User);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual(-1, u.Port);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testGitSshProto()
        {
            const string str = "git+ssh://example.com/some/p ath";
            var u = new URIish(str);
            Assert.AreEqual("git+ssh", u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("/some/p ath", u.Path);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual(-1, u.Port);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testSshGitProto()
        {
            const string str = "ssh+git://example.com/some/p ath";
            var u = new URIish(str);
            Assert.AreEqual("ssh+git", u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("/some/p ath", u.Path);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual(-1, u.Port);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testSshProto()
        {
            const string str = "ssh://example.com/some/p ath";
            var u = new URIish(str);
            Assert.AreEqual("ssh", u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("/some/p ath", u.Path);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual(-1, u.Port);
            Assert.AreEqual(str, u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testSshProtoWithUserAndPort()
        {
            const string str = "ssh://user@example.com:33/some/p ath";
            var u = new URIish(str);
            Assert.AreEqual("ssh", u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("/some/p ath", u.Path);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual("user", u.User);
            Assert.IsNull(u.Pass);
            Assert.AreEqual(33, u.Port);
            Assert.AreEqual(str, u.ToPrivateString());
            Assert.AreEqual(u, new URIish(str));
        }

        [Test]
        public void testSshProtoWithUserPassAndPort()
        {
            const string str = "ssh://user:pass@example.com:33/some/p ath";
            var u = new URIish(str);
            Assert.AreEqual("ssh", u.Scheme);
            Assert.IsTrue(u.IsRemote);
            Assert.AreEqual("/some/p ath", u.Path);
            Assert.AreEqual("example.com", u.Host);
            Assert.AreEqual("user", u.User);
            Assert.AreEqual("pass", u.Pass);
            Assert.AreEqual(33, u.Port);
            Assert.AreEqual(str, u.ToPrivateString());
            Assert.AreEqual(u.SetPass(null).ToPrivateString(), u.ToString());
            Assert.AreEqual(u, new URIish(str));
        }
    }
}