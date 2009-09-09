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

using GitSharp.Transport;
using Xunit;

namespace GitSharp.Tests.Transport
{
    public class RefSpecTests
    {
        
        [Fact]
        public void test000_MasterMaster()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec(sn + ":" + sn);
            Assert.False(rs.Force);
            Assert.False(rs.Wildcard);
            Assert.Equal(sn, rs.Source);
            Assert.Equal(sn, rs.Destination);
            Assert.Equal(sn + ":" + sn, rs.ToString());
            Assert.Equal(rs, new RefSpec(rs.ToString()));

            Ref r = new Ref(Ref.Storage.Loose, sn, null);
            Assert.True(rs.MatchSource(r));
            Assert.True(rs.MatchDestination(r));
            Assert.Same(rs, rs.ExpandFromSource(r));

            r = new Ref(Ref.Storage.Loose, sn + "-and-more", null);
            Assert.False(rs.MatchSource(r));
            Assert.False(rs.MatchDestination(r));
        }

        [Fact]
        public void test001_SplitLastColon()
        {
            string lhs = ":m:a:i:n:t";
            string rhs = "refs/heads/maint";
            RefSpec rs = new RefSpec(lhs + ":" + rhs);
            Assert.False(rs.Force);
            Assert.False(rs.Wildcard);
            Assert.Equal(lhs, rs.Source);
            Assert.Equal(rhs, rs.Destination);
            Assert.Equal(lhs + ":" + rhs, rs.ToString());
            Assert.Equal(rs, new RefSpec(rs.ToString()));
        }

        [Fact]
        public void test002_ForceMasterMaster()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec("+" + sn + ":" + sn);
            Assert.True(rs.Force);
            Assert.False(rs.Wildcard);
            Assert.Equal(sn, rs.Source);
            Assert.Equal(sn, rs.Destination);
            Assert.Equal("+" + sn + ":" + sn, rs.ToString());
            Assert.Equal(rs, new RefSpec(rs.ToString()));

            Ref r = new Ref(Ref.Storage.Loose, sn, null);
            Assert.True(rs.MatchSource(r));
            Assert.True(rs.MatchDestination(r));
            Assert.Same(rs, rs.ExpandFromSource(r));

            r = new Ref(Ref.Storage.Loose, sn + "-and-more", null);
            Assert.False(rs.MatchSource(r));
            Assert.False(rs.MatchDestination(r));
        }

        [Fact]
        public void test003_Master()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec(sn);
            Assert.False(rs.Force);
            Assert.False(rs.Wildcard);
            Assert.Equal(sn, rs.Source);
            Assert.Null(rs.Destination);
            Assert.Equal(sn, rs.ToString());
            Assert.Equal(rs, new RefSpec(rs.ToString()));

            Ref r = new Ref(Ref.Storage.Loose, sn, null);
            Assert.True(rs.MatchSource(r));
            Assert.False(rs.MatchDestination(r));
            Assert.Same(rs, rs.ExpandFromSource(r));

            r = new Ref(Ref.Storage.Loose, sn + "-and-more", null);
            Assert.False(rs.MatchSource(r));
            Assert.False(rs.MatchDestination(r));
        }

        [Fact]
        public void test004_ForceMaster()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec("+" + sn);
            Assert.True(rs.Force);
            Assert.False(rs.Wildcard);
            Assert.Equal(sn, rs.Source);
            Assert.Null(rs.Destination);
            Assert.Equal("+" + sn, rs.ToString());
            Assert.Equal(rs, new RefSpec(rs.ToString()));

            Ref r = new Ref(Ref.Storage.Loose, sn, null);
            Assert.True(rs.MatchSource(r));
            Assert.False(rs.MatchDestination(r));
            Assert.Same(rs, rs.ExpandFromSource(r));

            r = new Ref(Ref.Storage.Loose, sn + "-and-more", null);
            Assert.False(rs.MatchSource(r));
            Assert.False(rs.MatchDestination(r));
        }

        [Fact]
        public void test005_DeleteMaster()
        {
            string sn = "refs/heads/master";
            RefSpec rs = new RefSpec(":" + sn);
            Assert.False(rs.Force);
            Assert.False(rs.Wildcard);
            Assert.Equal(sn, rs.Destination);
            Assert.Null(rs.Source);
            Assert.Equal(":" + sn, rs.ToString());
            Assert.Equal(rs, new RefSpec(rs.ToString()));

            Ref r = new Ref(Ref.Storage.Loose, sn, null);
            Assert.False(rs.MatchSource(r));
            Assert.True(rs.MatchDestination(r));
            Assert.Same(rs, rs.ExpandFromSource(r));

            r = new Ref(Ref.Storage.Loose, sn + "-and-more", null);
            Assert.False(rs.MatchSource(r));
            Assert.False(rs.MatchDestination(r));
        }

        [Fact]
        public void test006_ForceRemotesOrigin()
        {
            string srcn = "refs/heads/*";
            string dstn = "refs/remotes/origin/*";
            RefSpec rs = new RefSpec("+" + srcn + ":" + dstn);
            Assert.True(rs.Force);
            Assert.True(rs.Wildcard);
            Assert.Equal(srcn, rs.Source);
            Assert.Equal(dstn, rs.Destination);
            Assert.Equal("+" + srcn + ":" + dstn, rs.ToString());
            Assert.Equal(rs, new RefSpec(rs.ToString()));

            Ref r;
            RefSpec expanded;

            r = new Ref(Ref.Storage.Loose, "refs/heads/master", null);
            Assert.True(rs.MatchSource(r));
            Assert.False(rs.MatchDestination(r));
            expanded = rs.ExpandFromSource(r);
            Assert.NotSame(rs, expanded);
            Assert.True(expanded.Force);
            Assert.False(expanded.Wildcard);
            Assert.Equal(r.Name, expanded.Source);
            Assert.Equal("refs/remotes/origin/master", expanded.Destination);

            r = new Ref(Ref.Storage.Loose, "refs/remotes/origin/next", null);
            Assert.False(rs.MatchSource(r));
            Assert.True(rs.MatchDestination(r));

            r = new Ref(Ref.Storage.Loose, "refs/tags/v1.0", null);
            Assert.False(rs.MatchSource(r));
            Assert.False(rs.MatchDestination(r));
        }

        [Fact]
        public void test007_CreateEmpty()
        {
            RefSpec rs = new RefSpec();
            Assert.False(rs.Force);
            Assert.False(rs.Wildcard);
            Assert.Equal("HEAD", rs.Source);
            Assert.Null(rs.Destination);
            Assert.Equal("HEAD", rs.ToString());
        }

        [Fact]
        public void test008_SetForceUpdate()
        {
            string s = "refs/heads/*:refs/remotes/origin/*";
            RefSpec a = new RefSpec(s);
            Assert.False(a.Force);
            RefSpec b = a.SetForce(true);
            Assert.NotSame(a, b);
            Assert.False(a.Force);
            Assert.True(b.Force);
            Assert.Equal(s, a.ToString());
            Assert.Equal("+" + s, b.ToString());
        }

        [Fact]
        public void test009_SetSource()
        {
            RefSpec a = new RefSpec();
            RefSpec b = a.SetSource("refs/heads/master");
            Assert.NotSame(a, b);
            Assert.Equal("HEAD", a.ToString());
            Assert.Equal("refs/heads/master", b.ToString());
        }

        [Fact]
        public void test010_SetDestination()
        {
            RefSpec a = new RefSpec();
            RefSpec b = a.SetDestination("refs/heads/master");
            Assert.NotSame(a, b);
            Assert.Equal("HEAD", a.ToString());
            Assert.Equal("HEAD:refs/heads/master", b.ToString());
        }

        [Fact]
        public void test010_SetDestination_SourceNull()
        {
            RefSpec a = new RefSpec();
            RefSpec b;

            b = a.SetDestination("refs/heads/master");
            b = b.SetSource(null);
            Assert.NotSame(a, b);
            Assert.Equal("HEAD", a.ToString());
            Assert.Equal(":refs/heads/master", b.ToString());
        }

        [Fact]
        public void test011_SetSourceDestination()
        {
            RefSpec a = new RefSpec();
            RefSpec b;
            b = a.SetSourceDestination("refs/heads/*", "refs/remotes/origin/*");
            Assert.NotSame(a, b);
            Assert.Equal("HEAD", a.ToString());
            Assert.Equal("refs/heads/*:refs/remotes/origin/*", b.ToString());
        }

        [Fact]
        public void test012_ExpandFromDestination_NonWildcard()
        {
            string src = "refs/heads/master";
            string dst = "refs/remotes/origin/master";
            RefSpec a = new RefSpec(src + ":" + dst);
            RefSpec r = a.ExpandFromDestination(dst);
            Assert.Same(a, r);
            Assert.False(r.Wildcard);
            Assert.Equal(src, r.Source);
            Assert.Equal(dst, r.Destination);
        }

        [Fact]
        public void test012_ExpandFromDestination_Wildcard()
        {
            string src = "refs/heads/master";
            string dst = "refs/remotes/origin/master";
            RefSpec a = new RefSpec("refs/heads/*:refs/remotes/origin/*");
            RefSpec r = a.ExpandFromDestination(dst);
            Assert.NotSame(a, r);
            Assert.False(r.Wildcard);
            Assert.Equal(src, r.Source);
            Assert.Equal(dst, r.Destination);
        }

    }

}