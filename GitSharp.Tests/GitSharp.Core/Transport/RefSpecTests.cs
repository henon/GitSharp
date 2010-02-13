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

using GitSharp.Core;
using GitSharp.Core.Transport;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Transport
{
    [TestFixture]
    public class RefSpecTests
    {
        
        [Test]
        public void testMasterMaster()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec(sn + ":" + sn);
            Assert.IsFalse(rs.Force);
            Assert.IsFalse(rs.Wildcard);
            Assert.AreEqual(sn, rs.Source);
            Assert.AreEqual(sn, rs.Destination);
            Assert.AreEqual(sn + ":" + sn, rs.ToString());
            Assert.AreEqual(rs, new RefSpec(rs.ToString()));

            Core.Ref r = new Unpeeled(Storage.Loose, sn, null);
            Assert.IsTrue(rs.MatchSource(r));
            Assert.IsTrue(rs.MatchDestination(r));
            Assert.AreSame(rs, rs.ExpandFromSource(r));

            r = new Unpeeled(Storage.Loose, sn + "-and-more", null);
            Assert.IsFalse(rs.MatchSource(r));
            Assert.IsFalse(rs.MatchDestination(r));
        }

        [Test]
        public void testSplitLastColon()
        {
            string lhs = ":m:a:i:n:t";
            string rhs = "refs/heads/maint";
            RefSpec rs = new RefSpec(lhs + ":" + rhs);
            Assert.IsFalse(rs.Force);
            Assert.IsFalse(rs.Wildcard);
            Assert.AreEqual(lhs, rs.Source);
            Assert.AreEqual(rhs, rs.Destination);
            Assert.AreEqual(lhs + ":" + rhs, rs.ToString());
            Assert.AreEqual(rs, new RefSpec(rs.ToString()));
        }

        [Test]
        public void testForceMasterMaster()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec("+" + sn + ":" + sn);
            Assert.IsTrue(rs.Force);
            Assert.IsFalse(rs.Wildcard);
            Assert.AreEqual(sn, rs.Source);
            Assert.AreEqual(sn, rs.Destination);
            Assert.AreEqual("+" + sn + ":" + sn, rs.ToString());
            Assert.AreEqual(rs, new RefSpec(rs.ToString()));

            Core.Ref r = new Unpeeled(Storage.Loose, sn, null);
            Assert.IsTrue(rs.MatchSource(r));
            Assert.IsTrue(rs.MatchDestination(r));
            Assert.AreSame(rs, rs.ExpandFromSource(r));

            r = new Unpeeled(Storage.Loose, sn + "-and-more", null);
            Assert.IsFalse(rs.MatchSource(r));
            Assert.IsFalse(rs.MatchDestination(r));
        }

        [Test]
        public void testMaster()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec(sn);
            Assert.IsFalse(rs.Force);
            Assert.IsFalse(rs.Wildcard);
            Assert.AreEqual(sn, rs.Source);
            Assert.IsNull(rs.Destination);
            Assert.AreEqual(sn, rs.ToString());
            Assert.AreEqual(rs, new RefSpec(rs.ToString()));

            Core.Ref r = new Unpeeled(Storage.Loose, sn, null);
            Assert.IsTrue(rs.MatchSource(r));
            Assert.IsFalse(rs.MatchDestination(r));
            Assert.AreSame(rs, rs.ExpandFromSource(r));

            r = new Unpeeled(Storage.Loose, sn + "-and-more", null);
            Assert.IsFalse(rs.MatchSource(r));
            Assert.IsFalse(rs.MatchDestination(r));
        }

        [Test]
        public void testForceMaster()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec("+" + sn);
            Assert.IsTrue(rs.Force);
            Assert.IsFalse(rs.Wildcard);
            Assert.AreEqual(sn, rs.Source);
            Assert.IsNull(rs.Destination);
            Assert.AreEqual("+" + sn, rs.ToString());
            Assert.AreEqual(rs, new RefSpec(rs.ToString()));

            Core.Ref r = new Unpeeled(Storage.Loose, sn, null);
            Assert.IsTrue(rs.MatchSource(r));
            Assert.IsFalse(rs.MatchDestination(r));
            Assert.AreSame(rs, rs.ExpandFromSource(r));

            r = new Unpeeled(Storage.Loose, sn + "-and-more", null);
            Assert.IsFalse(rs.MatchSource(r));
            Assert.IsFalse(rs.MatchDestination(r));
        }

        [Test]
        public void testDeleteMaster()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec(":" + sn);
            Assert.IsFalse(rs.Force);
            Assert.IsFalse(rs.Wildcard);
            Assert.AreEqual(sn, rs.Destination);
            Assert.IsNull(rs.Source);
            Assert.AreEqual(":" + sn, rs.ToString());
            Assert.AreEqual(rs, new RefSpec(rs.ToString()));

            Core.Ref r = new Unpeeled(Storage.Loose, sn, null);
            Assert.IsFalse(rs.MatchSource(r));
            Assert.IsTrue(rs.MatchDestination(r));
            Assert.AreSame(rs, rs.ExpandFromSource(r));

            r = new Unpeeled(Storage.Loose, sn + "-and-more", null);
            Assert.IsFalse(rs.MatchSource(r));
            Assert.IsFalse(rs.MatchDestination(r));
        }

        [Test]
        public void testForceRemotesOrigin()
        {
            string srcn = "refs/heads/*";
            string dstn = "refs/remotes/origin/*";
            RefSpec rs = new RefSpec("+" + srcn + ":" + dstn);
            Assert.IsTrue(rs.Force);
            Assert.IsTrue(rs.Wildcard);
            Assert.AreEqual(srcn, rs.Source);
            Assert.AreEqual(dstn, rs.Destination);
            Assert.AreEqual("+" + srcn + ":" + dstn, rs.ToString());
            Assert.AreEqual(rs, new RefSpec(rs.ToString()));

            Core.Ref r;
            RefSpec expanded;

            r = new Unpeeled(Storage.Loose, "refs/heads/master", null);
            Assert.IsTrue(rs.MatchSource(r));
            Assert.IsFalse(rs.MatchDestination(r));
            expanded = rs.ExpandFromSource(r);
            Assert.AreNotSame(rs, expanded);
            Assert.IsTrue(expanded.Force);
            Assert.IsFalse(expanded.Wildcard);
            Assert.AreEqual(r.Name, expanded.Source);
            Assert.AreEqual("refs/remotes/origin/master", expanded.Destination);

            r = new Unpeeled(Storage.Loose, "refs/remotes/origin/next", null);
            Assert.IsFalse(rs.MatchSource(r));
            Assert.IsTrue(rs.MatchDestination(r));

            r = new Unpeeled(Storage.Loose, "refs/tags/v1.0", null);
            Assert.IsFalse(rs.MatchSource(r));
            Assert.IsFalse(rs.MatchDestination(r));
        }

        [Test]
        public void testCreateEmpty()
        {
            RefSpec rs = new RefSpec();
            Assert.IsFalse(rs.Force);
            Assert.IsFalse(rs.Wildcard);
            Assert.AreEqual("HEAD", rs.Source);
            Assert.IsNull(rs.Destination);
            Assert.AreEqual("HEAD", rs.ToString());
        }

        [Test]
        public void testSetForceUpdate()
        {
            string s = "refs/heads/*:refs/remotes/origin/*";
            RefSpec a = new RefSpec(s);
            Assert.IsFalse(a.Force);
            RefSpec b = a.SetForce(true);
            Assert.AreNotSame(a, b);
            Assert.IsFalse(a.Force);
            Assert.IsTrue(b.Force);
            Assert.AreEqual(s, a.ToString());
            Assert.AreEqual("+" + s, b.ToString());
        }

        [Test]
        public void testSetSource()
        {
            RefSpec a = new RefSpec();
            RefSpec b = a.SetSource("refs/heads/master");
            Assert.AreNotSame(a, b);
            Assert.AreEqual("HEAD", a.ToString());
            Assert.AreEqual("refs/heads/master", b.ToString());
        }

        [Test]
        public void testSetDestination()
        {
            RefSpec a = new RefSpec();
            RefSpec b = a.SetDestination("refs/heads/master");
            Assert.AreNotSame(a, b);
            Assert.AreEqual("HEAD", a.ToString());
            Assert.AreEqual("HEAD:refs/heads/master", b.ToString());
        }

        [Test]
        public void testSetDestination_SourceNull()
        {
            RefSpec a = new RefSpec();
            RefSpec b;

            b = a.SetDestination("refs/heads/master");
            b = b.SetSource(null);
            Assert.AreNotSame(a, b);
            Assert.AreEqual("HEAD", a.ToString());
            Assert.AreEqual(":refs/heads/master", b.ToString());
        }

        [Test]
        public void testSetSourceDestination()
        {
            RefSpec a = new RefSpec();
            RefSpec b;
            b = a.SetSourceDestination("refs/heads/*", "refs/remotes/origin/*");
            Assert.AreNotSame(a, b);
            Assert.AreEqual("HEAD", a.ToString());
            Assert.AreEqual("refs/heads/*:refs/remotes/origin/*", b.ToString());
        }

        [Test]
        public void testExpandFromDestination_NonWildcard()
        {
            string src = "refs/heads/master";
            string dst = "refs/remotes/origin/master";
            RefSpec a = new RefSpec(src + ":" + dst);
            RefSpec r = a.ExpandFromDestination(dst);
            Assert.AreSame(a, r);
            Assert.IsFalse(r.Wildcard);
            Assert.AreEqual(src, r.Source);
            Assert.AreEqual(dst, r.Destination);
        }

        [Test]
        public void testExpandFromDestination_Wildcard()
        {
            string src = "refs/heads/master";
            string dst = "refs/remotes/origin/master";
            RefSpec a = new RefSpec("refs/heads/*:refs/remotes/origin/*");
            RefSpec r = a.ExpandFromDestination(dst);
            Assert.AreNotSame(a, r);
            Assert.IsFalse(r.Wildcard);
            Assert.AreEqual(src, r.Source);
            Assert.AreEqual(dst, r.Destination);
        }
    }
}