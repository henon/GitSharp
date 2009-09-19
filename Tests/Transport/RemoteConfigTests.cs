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
using NUnit.Framework;

namespace GitSharp.Tests.Transport
{

    [TestFixture]
    public class RemoteConfigTests : RepositoryTestCase
    {
        private Config config;

        public override void setUp()
        {
            base.setUp();
            config = new Config();
        }

        private void readConfig(string dat)
        {
            config = new Config();
            config.fromText(dat);
        }

        private void checkConfig(string exp)
        {
            Assert.AreEqual(exp, config.toText());
        }

        [Test]
        public void test000_Simple()
        {
            readConfig("[remote \"spearce\"]\n" + "url = http://www.spearce.org/egit.git\n" +
                        "fetch = +refs/heads/*:refs/remotes/spearce/*\n");

            RemoteConfig rc = new RemoteConfig(config, "spearce");
            System.Collections.Generic.List<URIish> allURIs = rc.URIs;

            Assert.AreEqual("spearce", rc.Name);
            Assert.IsNotNull(allURIs);
            Assert.IsNotNull(rc.Fetch);
            Assert.IsNotNull(rc.Push);
            Assert.IsNotNull(rc.TagOpt);
            Assert.AreEqual(0, rc.Timeout);
            Assert.AreSame(TagOpt.AUTO_FOLLOW, rc.TagOpt);

            Assert.AreEqual(1, allURIs.Count);
            Assert.AreEqual("http://www.spearce.org/egit.git", allURIs[0].ToString());

            Assert.AreEqual(1, rc.Fetch.Count);
            RefSpec spec = rc.Fetch[0];
            Assert.IsTrue(spec.Force);
            Assert.IsTrue(spec.Wildcard);
            Assert.AreEqual("refs/heads/*", spec.Source);
            Assert.AreEqual("refs/remotes/spearce/*", spec.Destination);

            Assert.AreEqual(0, rc.Push.Count);
        }

        [Test]
        public void test001_SimpleNoTags()
        {
            readConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n"
                        + "tagopt = --no-tags\n");
            RemoteConfig rc = new RemoteConfig(config, "spearce");
            Assert.AreSame(TagOpt.NO_TAGS, rc.TagOpt);
        }

        [Test]
        public void test002_SimpleAlwaysTags()
        {
            readConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n"
                        + "tagopt = --tags\n");
            RemoteConfig rc = new RemoteConfig(config, "spearce");
            Assert.AreSame(TagOpt.FETCH_TAGS, rc.TagOpt);
        }

        [Test]
        public void test003_Mirror()
        {
            readConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "fetch = +refs/heads/*:refs/heads/*\n"
                        + "fetch = refs/tags/*:refs/tags/*\n");

            RemoteConfig rc = new RemoteConfig(config, "spearce");
            System.Collections.Generic.List<URIish> allURIs = rc.URIs;

            Assert.AreEqual("spearce", rc.Name);
            Assert.IsNotNull(allURIs);
            Assert.IsNotNull(rc.Fetch);
            Assert.IsNotNull(rc.Push);

            Assert.AreEqual(1, allURIs.Count);
            Assert.AreEqual("http://www.spearce.org/egit.git", allURIs[0].ToString());

            Assert.AreEqual(2, rc.Fetch.Count);

            RefSpec spec = rc.Fetch[0];
            Assert.IsTrue(spec.Force);
            Assert.IsTrue(spec.Wildcard);
            Assert.AreEqual("refs/heads/*", spec.Source);
            Assert.AreEqual("refs/heads/*", spec.Destination);

            spec = rc.Fetch[1];
            Assert.IsFalse(spec.Force);
            Assert.IsTrue(spec.Wildcard);
            Assert.AreEqual("refs/tags/*", spec.Source);
            Assert.AreEqual("refs/tags/*", spec.Destination);

            Assert.AreEqual(0, rc.Push.Count);
        }

        [Test]
        public void test004_Backup()
        {
            readConfig("[remote \"backup\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "url = user@repo.or.cz:/srv/git/egit.git\n"
                        + "push = +refs/heads/*:refs/heads/*\n"
                        + "push = refs/tags/*:refs/tags/*\n");

            RemoteConfig rc = new RemoteConfig(config, "backup");
            System.Collections.Generic.List<URIish> allURIs = rc.URIs;

            Assert.AreEqual("backup", rc.Name);
            Assert.IsNotNull(allURIs);
            Assert.IsNotNull(rc.Fetch);
            Assert.IsNotNull(rc.Push);

            Assert.AreEqual(2, allURIs.Count);
            Assert.AreEqual("http://www.spearce.org/egit.git", allURIs[0].ToString());
            Assert.AreEqual("user@repo.or.cz:/srv/git/egit.git", allURIs[1].ToString());

            Assert.AreEqual(0, rc.Fetch.Count);

            Assert.AreEqual(2, rc.Push.Count);
            RefSpec spec = rc.Push[0];
            Assert.IsTrue(spec.Force);
            Assert.IsTrue(spec.Wildcard);
            Assert.AreEqual("refs/heads/*", spec.Source);
            Assert.AreEqual("refs/heads/*", spec.Destination);

            spec = rc.Push[1];
            Assert.IsFalse(spec.Force);
            Assert.IsTrue(spec.Wildcard);
            Assert.AreEqual("refs/tags/*", spec.Source);
            Assert.AreEqual("refs/tags/*", spec.Destination);
        }

        [Test]
        public void test005_UploadPack()
        {
            readConfig("[remote \"example\"]\n"
                        + "url = user@example.com:egit.git\n"
                        + "fetch = +refs/heads/*:refs/remotes/example/*\n"
                        + "uploadpack = /path/to/git/git-upload-pack\n"
                        + "receivepack = /path/to/git/git-receive-pack\n");

            RemoteConfig rc = new RemoteConfig(config, "example");
            System.Collections.Generic.List<URIish> allURIs = rc.URIs;

            Assert.AreEqual("example", rc.Name);
            Assert.IsNotNull(allURIs);
            Assert.IsNotNull(rc.Fetch);
            Assert.IsNotNull(rc.Push);

            Assert.AreEqual(1, allURIs.Count);
            Assert.AreEqual("user@example.com:egit.git", allURIs[0].ToString());

            Assert.AreEqual(1, rc.Fetch.Count);
            RefSpec spec = rc.Fetch[0];
            Assert.IsTrue(spec.Force);
            Assert.IsTrue(spec.Wildcard);
            Assert.AreEqual("refs/heads/*", spec.Source);
            Assert.AreEqual("refs/remotes/example/*", spec.Destination);

            Assert.AreEqual(0, rc.Push.Count);

            Assert.AreEqual("/path/to/git/git-upload-pack", rc.UploadPack);
            Assert.AreEqual("/path/to/git/git-receive-pack", rc.ReceivePack);
        }

        [Test]
        public void test006_Unknown()
        {
            readConfig(string.Empty);

            RemoteConfig rc = new RemoteConfig(config, "backup");
            Assert.AreEqual(0, rc.URIs.Count);
            Assert.AreEqual(0, rc.Fetch.Count);
            Assert.AreEqual(0, rc.Push.Count);
            Assert.AreEqual("git-upload-pack", rc.UploadPack);
            Assert.AreEqual("git-receive-pack", rc.ReceivePack);
        }

        [Test]
        public void test007_AddURI()
        {
            readConfig(string.Empty);

            URIish uri = new URIish("/some/dir");
            RemoteConfig rc = new RemoteConfig(config, "backup");
            Assert.AreEqual(0, rc.URIs.Count);

            Assert.IsTrue(rc.AddURI(uri));
            Assert.AreEqual(1, rc.URIs.Count);
            Assert.AreSame(uri, rc.URIs[0]);

            Assert.IsFalse(rc.AddURI(new URIish(uri.ToString())));
            Assert.AreEqual(1, rc.URIs.Count);
        }

        [Test]
        public void test008_RemoveFirstURI()
        {
            readConfig(string.Empty);

            URIish a = new URIish("/some/dir");
            URIish b = new URIish("/another/dir");
            URIish c = new URIish("/more/dirs");
            RemoteConfig rc = new RemoteConfig(config, "backup");
            Assert.IsTrue(rc.AddURI(a));
            Assert.IsTrue(rc.AddURI(b));
            Assert.IsTrue(rc.AddURI(c));

            Assert.AreEqual(3, rc.URIs.Count);
            Assert.AreSame(a, rc.URIs[0]);
            Assert.AreSame(b, rc.URIs[1]);
            Assert.AreEqual(c, rc.URIs[2]);

            Assert.IsTrue(rc.RemoveURI(a));
            Assert.AreSame(b, rc.URIs[0]);
            Assert.AreSame(c, rc.URIs[1]);
        }

        [Test]
        public void test009_RemoveMiddleURI()
        {
            readConfig(string.Empty);

            URIish a = new URIish("/some/dir");
            URIish b = new URIish("/another/dir");
            URIish c = new URIish("/more/dirs");
            RemoteConfig rc = new RemoteConfig(config, "backup");
            Assert.IsTrue(rc.AddURI(a));
            Assert.IsTrue(rc.AddURI(b));
            Assert.IsTrue(rc.AddURI(c));

            Assert.AreEqual(3, rc.URIs.Count);
            Assert.AreSame(a, rc.URIs[0]);
            Assert.AreSame(b, rc.URIs[1]);
            Assert.AreEqual(c, rc.URIs[2]);

            Assert.IsTrue(rc.RemoveURI(b));
            Assert.AreEqual(2, rc.URIs.Count);
            Assert.AreSame(a, rc.URIs[0]);
            Assert.AreSame(c, rc.URIs[1]);
        }

        [Test]
        public void test010_RemoveLastURI()
        {
            readConfig(string.Empty);

            URIish a = new URIish("/some/dir");
            URIish b = new URIish("/another/dir");
            URIish c = new URIish("/more/dirs");
            RemoteConfig rc = new RemoteConfig(config, "backup");
            Assert.IsTrue(rc.AddURI(a));
            Assert.IsTrue(rc.AddURI(b));
            Assert.IsTrue(rc.AddURI(c));

            Assert.AreEqual(3, rc.URIs.Count);
            Assert.AreSame(a, rc.URIs[0]);
            Assert.AreSame(b, rc.URIs[1]);
            Assert.AreEqual(c, rc.URIs[2]);

            Assert.IsTrue(rc.RemoveURI(c));
            Assert.AreEqual(2, rc.URIs.Count);
            Assert.AreSame(a, rc.URIs[0]);
            Assert.AreSame(b, rc.URIs[1]);
        }

        [Test]
        public void test011_RemoveOnlyURI()
        {
            readConfig(string.Empty);

            URIish a = new URIish("/some/dir");
            RemoteConfig rc = new RemoteConfig(config, "backup");
            Assert.IsTrue(rc.AddURI(a));

            Assert.AreEqual(1, rc.URIs.Count);
            Assert.AreSame(a, rc.URIs[0]);

            Assert.IsTrue(rc.RemoveURI(a));
            Assert.AreEqual(0, rc.URIs.Count);
        }

        [Test]
        public void test012_CreateOrigin()
        {
            RemoteConfig rc = new RemoteConfig(config, "origin");
            rc.AddURI(new URIish("/some/dir"));
            rc.AddFetchRefSpec(new RefSpec("+refs/heads/*:refs/remotes/" + rc.Name + "/*"));
            rc.Update(config);

            checkConfig("[remote \"origin\"]\n" + "\turl = /some/dir\n"
                    + "\tfetch = +refs/heads/*:refs/remotes/origin/*\n");
        }

        [Test]
        public void test013_SaveAddURI()
        {
            readConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n");

            RemoteConfig rc = new RemoteConfig(config, "spearce");
            rc.AddURI(new URIish("/some/dir"));
            Assert.AreEqual(2, rc.URIs.Count);
            rc.Update(config);

            checkConfig("[remote \"spearce\"]\n"
                    + "\turl = http://www.spearce.org/egit.git\n"
                    + "\turl = /some/dir\n"
                    + "\tfetch = +refs/heads/*:refs/remotes/spearce/*\n");
        }

        [Test]
        public void test014_SaveRemoveLastURI()
        {
            readConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "url = /some/dir\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n");

            RemoteConfig rc = new RemoteConfig(config, "spearce");
            Assert.AreEqual(2, rc.URIs.Count);
            rc.RemoveURI(new URIish("/some/dir"));
            Assert.AreEqual(1, rc.URIs.Count);
            rc.Update(config);

            checkConfig("[remote \"spearce\"]\n"
                    + "\turl = http://www.spearce.org/egit.git\n"
                    + "\tfetch = +refs/heads/*:refs/remotes/spearce/*\n");
        }

        [Test]
        public void test015_SaveRemoveFirstURI()
        {
            readConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "url = /some/dir\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n");

            RemoteConfig rc = new RemoteConfig(config, "spearce");
            Assert.AreEqual(2, rc.URIs.Count);
            rc.RemoveURI(new URIish("http://www.spearce.org/egit.git"));
            Assert.AreEqual(1, rc.URIs.Count);
            rc.Update(config);

            checkConfig("[remote \"spearce\"]\n" + "\turl = /some/dir\n"
                    + "\tfetch = +refs/heads/*:refs/remotes/spearce/*\n");
        }

        [Test]
        public void test016_SaveNoTags()
        {
            RemoteConfig rc = new RemoteConfig(config, "origin");
            rc.AddURI(new URIish("/some/dir"));
            rc.AddFetchRefSpec(new RefSpec("+refs/heads/*:refs/remotes/" + rc.Name + "/*"));
            rc.SetTagOpt(TagOpt.NO_TAGS);
            rc.Update(config);

            checkConfig("[remote \"origin\"]\n" + "\turl = /some/dir\n"
                    + "\tfetch = +refs/heads/*:refs/remotes/origin/*\n"
                    + "\ttagopt = --no-tags\n");
        }

        [Test]
        public void test017_SaveAllTags()
        {
            RemoteConfig rc = new RemoteConfig(config, "origin");
            rc.AddURI(new URIish("/some/dir"));
            rc.AddFetchRefSpec(new RefSpec("+refs/heads/*:refs/remotes/" + rc.Name + "/*"));
            rc.SetTagOpt(TagOpt.FETCH_TAGS);
            rc.Update(config);

            checkConfig("[remote \"origin\"]\n" + "\turl = /some/dir\n"
                    + "\tfetch = +refs/heads/*:refs/remotes/origin/*\n"
                    + "\ttagopt = --tags\n");
        }

        [Test]
        public void test018_SimpleTimeout()
        {
            readConfig("[remote \"spearce\"]\n"
                    + "url = http://www.spearce.org/egit.git\n"
                    + "fetch = +refs/heads/*:refs/remotes/spearce/*\n"
                    + "timeout = 12\n");

            RemoteConfig rc = new RemoteConfig(config, "spearce");
            Assert.AreEqual(12, rc.Timeout);
        }

        [Test]
        public void test019_SaveTimeout()
        {
            RemoteConfig rc = new RemoteConfig(config, "origin");
            rc.AddURI(new URIish("/some/dir"));
            rc.AddFetchRefSpec(new RefSpec("+refs/heads/*:refs/remotes/" + rc.Name + "/*"));
            rc.Timeout = 60;
            rc.Update(config);
            checkConfig("[remote \"origin\"]\n" + "\turl = /some/dir\n"
                    + "\tfetch = +refs/heads/*:refs/remotes/origin/*\n"
                    + "\ttimeout = 60\n");
        }
    }

}