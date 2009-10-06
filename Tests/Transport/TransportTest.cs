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

using GitSharp.Core;
using GitSharp.Core.Transport;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace GitSharp.Tests.Transport
{
    [TestFixture]
    public class TransportTest : RepositoryTestCase
    {
        private GitSharp.Core.Transport.Transport transport;
        private RemoteConfig remoteConfig;

        public override void setUp()
        {
            base.setUp();
            RepositoryConfig config = db.Config;
            remoteConfig = new RemoteConfig(config, "test");
            remoteConfig.AddURI(new URIish("http://everyones.loves.git/u/2"));
            transport = null;
        }

        protected new void tearDown()
        {
            if (transport != null)
            {
                transport.close();
                transport = null;
            }
            base.tearDown();
        }

        [Test]
        public void testFindRemoteRefUpdatesNoWildcardNoTracking()
        {
            transport = GitSharp.Core.Transport.Transport.Open(db, remoteConfig);
            ICollection<RemoteRefUpdate> result =
                transport.findRemoteRefUpdatesFor(new List<RefSpec> {new RefSpec("refs/heads/master:refs/heads/x")});

            Assert.AreEqual(1, result.Count);
            RemoteRefUpdate rru = result.ToArray()[0];
            Assert.AreEqual(null, rru.ExpectedOldObjectId);
            Assert.IsFalse(rru.ForceUpdate);
            Assert.AreEqual("refs/heads/master", rru.SourceRef);
            Assert.AreEqual(db.Resolve("refs/heads/master"), rru.NewObjectId);
            Assert.AreEqual("refs/heads/x", rru.RemoteName);
        }

        [Test]
        public void testFindRemoteRefUpdatesNoWildcardNoDestination()
        {
            transport = GitSharp.Core.Transport.Transport.Open(db, remoteConfig);
            ICollection<RemoteRefUpdate> result =
                transport.findRemoteRefUpdatesFor(new List<RefSpec> {new RefSpec("+refs/heads/master")});

            Assert.AreEqual(1, result.Count);
            RemoteRefUpdate rru = result.ToArray()[0];
            Assert.AreEqual(null, rru.ExpectedOldObjectId);
            Assert.IsTrue(rru.ForceUpdate);
            Assert.AreEqual("refs/heads/master", rru.SourceRef);
            Assert.AreEqual(db.Resolve("refs/heads/master"), rru.NewObjectId);
            Assert.AreEqual("refs/heads/master", rru.RemoteName);
        }

        [Test]
        public void testFindRemoteRefUpdatesWildcardNoTracking()
        {
            transport = GitSharp.Core.Transport.Transport.Open(db, remoteConfig);
            ICollection<RemoteRefUpdate> result =
                transport.findRemoteRefUpdatesFor(new List<RefSpec> { new RefSpec("+refs/heads/*:refs/heads/test/*") });

            Assert.AreEqual(12, result.Count);
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
            Assert.IsTrue(foundA);
            Assert.IsTrue(foundB);
        }

        [Test]
        public void testFindRemoteRefUpdatesTwoRefSpecs()
        {
            transport = GitSharp.Core.Transport.Transport.Open(db, remoteConfig);
            RefSpec specA = new RefSpec("+refs/heads/a:refs/heads/b");
            RefSpec specC = new RefSpec("+refs/heads/c:refs/heads/d");
            List<RefSpec> specs = new List<RefSpec>{specA, specC};
            ICollection<RemoteRefUpdate> result = transport.findRemoteRefUpdatesFor(specs);

            Assert.AreEqual(2, result.Count);
            bool foundA = false;
            bool foundC = false;
            foreach (RemoteRefUpdate rru in result)
            {
                if ("refs/heads/a".Equals(rru.SourceRef) && "refs/heads/b".Equals(rru.RemoteName))
                    foundA = true;
                if ("refs/heads/c".Equals(rru.SourceRef) && "refs/heads/d".Equals(rru.RemoteName))
                    foundC = true;
            }
            Assert.IsTrue(foundA);
            Assert.IsTrue(foundC);
        }

        [Test]
        public void testFindRemoteRefUpdatesTrackingRef()
        {
            remoteConfig.AddFetchRefSpec(new RefSpec("refs/heads/*:refs/remotes/test/*"));
            transport = GitSharp.Core.Transport.Transport.Open(db, remoteConfig);
            ICollection<RemoteRefUpdate> result =
                transport.findRemoteRefUpdatesFor(new List<RefSpec> {new RefSpec("+refs/heads/a:refs/heads/a")});

            Assert.AreEqual(1, result.Count);
            TrackingRefUpdate tru = result.ToArray()[0].TrackingRefUpdate;
            Assert.AreEqual("refs/remotes/test/a", tru.LocalName);
            Assert.AreEqual("refs/heads/a", tru.RemoteName);
            Assert.AreEqual(db.Resolve("refs/heads/a"), tru.NewObjectId);
            Assert.AreEqual(null, tru.OldObjectId);
        }
    }
}
