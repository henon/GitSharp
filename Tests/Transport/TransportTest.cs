/*
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

using System.Collections.Generic;
using System.Linq;
using GitSharp.Tests.Util;
using GitSharp.Transport;
using Xunit;

namespace GitSharp.Tests.Transport
{
    public class TransportTest : RepositoryTestCase
    {
        private GitSharp.Transport.Transport _transport;
        private RemoteConfig _remoteConfig;

        protected override void SetUp()
        {
            base.SetUp();
            RepositoryConfig config = db.Config;
            _remoteConfig = new RemoteConfig(config, "test");
            _remoteConfig.AddURI(new URIish("http://everyones.loves.git/u/2"));
            _transport = null;
        }

        protected override void TearDown()
        {
            if (_transport != null)
            {
                _transport.close();
                _transport = null;
            }

            base.TearDown();
        }

        [Fact]
        public void testFindRemoteRefUpdatesNoWilcardNoTracking()
        {
            _transport = GitSharp.Transport.Transport.Open(db, _remoteConfig);
            ICollection<RemoteRefUpdate> result =
                _transport.findRemoteRefUpdatesFor(new List<RefSpec> {new RefSpec("refs/heads/master:refs/heads/x")});

            Assert.Equal(1, result.Count);
            RemoteRefUpdate rru = result.ToArray()[0];
            Assert.Equal(null, rru.ExpectedOldObjectId);
            Assert.False(rru.ForceUpdate);
            Assert.Equal("refs/heads/master", rru.SourceRef);
            Assert.Equal(db.Resolve("refs/heads/master"), rru.NewObjectId);
            Assert.Equal("refs/heads/x", rru.RemoteName);
        }

        [Fact]
        public void testFindRemoteRefUpdatesNoWildcardNoDestination()
        {
            _transport = GitSharp.Transport.Transport.Open(db, _remoteConfig);
            ICollection<RemoteRefUpdate> result =
                _transport.findRemoteRefUpdatesFor(new List<RefSpec> {new RefSpec("+refs/heads/master")});

            Assert.Equal(1, result.Count);
            RemoteRefUpdate rru = result.ToArray()[0];
            Assert.Equal(null, rru.ExpectedOldObjectId);
            Assert.True(rru.ForceUpdate);
            Assert.Equal("refs/heads/master", rru.SourceRef);
            Assert.Equal(db.Resolve("refs/heads/master"), rru.NewObjectId);
            Assert.Equal("refs/heads/master", rru.RemoteName);
        }

        [Fact]
        public void testFindRemoteRefUpdatesWildcardNoTracking()
        {
            _transport = GitSharp.Transport.Transport.Open(db, _remoteConfig);
            ICollection<RemoteRefUpdate> result =
                _transport.findRemoteRefUpdatesFor(new List<RefSpec> { new RefSpec("+refs/heads/*:refs/heads/test/*") });

            Assert.Equal(12, result.Count);
            bool foundA = false;
            bool foundB = false;
            foreach (RemoteRefUpdate rru in result)
            {
                if ("refs/heads/a".Equals(rru.SourceRef) && "refs/heads/test/a".Equals(rru.RemoteName))
                {
                	foundA = true;
                }
                if ("refs/heads/b".Equals(rru.SourceRef) && "refs/heads/test/b".Equals(rru.RemoteName))
                {
                	foundB = true;
                }
            }
            Assert.True(foundA);
            Assert.True(foundB);
        }

        [Fact]
        public void testFindRemoteRefUpdatesTwoRefSpecs()
        {
            _transport = GitSharp.Transport.Transport.Open(db, _remoteConfig);
            var specA = new RefSpec("+refs/heads/a:refs/heads/b");
            var specC = new RefSpec("+refs/heads/c:refs/heads/d");
            var specs = new List<RefSpec>{specA, specC};
            ICollection<RemoteRefUpdate> result = _transport.findRemoteRefUpdatesFor(specs);

            Assert.Equal(2, result.Count);
            bool foundA = false;
            bool foundC = false;
            foreach (RemoteRefUpdate rru in result)
            {
                if ("refs/heads/a".Equals(rru.SourceRef) && "refs/heads/b".Equals(rru.RemoteName))
                    foundA = true;
                if ("refs/heads/c".Equals(rru.SourceRef) && "refs/heads/d".Equals(rru.RemoteName))
                    foundC = true;
            }
            Assert.True(foundA);
            Assert.True(foundC);
        }

        [Fact]
        public void testFindRemoteRefUpdatesTrackingRef()
        {
            _remoteConfig.AddFetchRefSpec(new RefSpec("refs/heads/*:refs/remotes/test/*"));
            _transport = GitSharp.Transport.Transport.Open(db, _remoteConfig);
            ICollection<RemoteRefUpdate> result =
                _transport.findRemoteRefUpdatesFor(new List<RefSpec> {new RefSpec("+refs/heads/a:refs/heads/a")});

            Assert.Equal(1, result.Count);
            TrackingRefUpdate tru = result.ToArray()[0].TrackingRefUpdate;
            Assert.Equal("refs/remotes/test/a", tru.LocalName);
            Assert.Equal("refs/heads/a", tru.RemoteName);
            Assert.Equal(db.Resolve("refs/heads/a"), tru.NewObjectId);
            Assert.Equal(null, tru.OldObjectId);
        }
    }
}
