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

using System.IO;
using GitSharp.Tests.Util;
using GitSharp.Transport;
using Xunit;

namespace GitSharp.Tests.Transport
{
	public class RemoteConfigTests : RepositoryTestCase
    {
        private void WriteConfig(string dat)
        {
            var f = new FileInfo(Path.Combine(db.Directory.ToString(), "config"));
            var stream = new FileStream(f.ToString(), System.IO.FileMode.Append);
            try
            {
                byte[] data = Constants.CHARSET.GetBytes(dat);
                stream.Write(data, 0, data.Length);
            }
            finally
            {
                stream.Close();
            }
            db.Config.load();
        }

        [Fact]
        public void test000_Simple()
        {
            WriteConfig("[remote \"spearce\"]\n" + "url = http://www.spearce.org/egit.git\n" +
                        "fetch = +refs/heads/*:refs/remotes/spearce/*\n");

            var rc = new RemoteConfig(db.Config, "spearce");
            System.Collections.Generic.List<URIish> allURIs = rc.URIs;

        	Assert.Equal("spearce", rc.Name);
            Assert.NotNull(allURIs);
            Assert.NotNull(rc.Fetch);
            Assert.NotNull(rc.Push);
            Assert.NotNull(rc.TagOpt);
            Assert.Same(TagOpt.AUTO_FOLLOW, rc.TagOpt);

            Assert.Equal(1, allURIs.Count);
            Assert.Equal("http://www.spearce.org/egit.git", allURIs[0].ToString());

            Assert.Equal(1, rc.Fetch.Count);
            RefSpec spec = rc.Fetch[0];
            Assert.True(spec.Force);
            Assert.True(spec.Wildcard);
            Assert.Equal("refs/heads/*", spec.Source);
            Assert.Equal("refs/remotes/spearce/*", spec.Destination);

            Assert.Equal(0, rc.Push.Count);
        }

        [Fact]
        public void test001_SimpleNoTags()
        {
            WriteConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n"
                        + "tagopt = --no-tags\n");
            var rc = new RemoteConfig(db.Config, "spearce");
            Assert.Same(TagOpt.NO_TAGS, rc.TagOpt);
        }

        [Fact]
        public void test002_SimpleAlwaysTags()
        {
            WriteConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n"
                        + "tagopt = --tags\n");
            var rc = new RemoteConfig(db.Config, "spearce");
            Assert.Same(TagOpt.FETCH_TAGS, rc.TagOpt);
        }

        [Fact]
        public void test003_Mirror()
        {
            WriteConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "fetch = +refs/heads/*:refs/heads/*\n"
                        + "fetch = refs/tags/*:refs/tags/*\n");

            var rc = new RemoteConfig(db.Config, "spearce");
            System.Collections.Generic.List<URIish> allURIs = rc.URIs;

        	Assert.Equal("spearce", rc.Name);
            Assert.NotNull(allURIs);
            Assert.NotNull(rc.Fetch);
            Assert.NotNull(rc.Push);

            Assert.Equal(1, allURIs.Count);
            Assert.Equal("http://www.spearce.org/egit.git", allURIs[0].ToString());

            Assert.Equal(2, rc.Fetch.Count);

            RefSpec spec = rc.Fetch[0];
            Assert.True(spec.Force);
            Assert.True(spec.Wildcard);
            Assert.Equal("refs/heads/*", spec.Source);
            Assert.Equal("refs/heads/*", spec.Destination);

            spec = rc.Fetch[1];
            Assert.False(spec.Force);
            Assert.True(spec.Wildcard);
            Assert.Equal("refs/tags/*", spec.Source);
            Assert.Equal("refs/tags/*", spec.Destination);

            Assert.Equal(0, rc.Push.Count);
        }

        [Fact]
        public void test004_Backup()
        {
            WriteConfig("[remote \"backup\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "url = user@repo.or.cz:/srv/git/egit.git\n"
                        + "push = +refs/heads/*:refs/heads/*\n"
                        + "push = refs/tags/*:refs/tags/*\n");

            var rc = new RemoteConfig(db.Config, "backup");
            System.Collections.Generic.List<URIish> allURIs = rc.URIs;

        	Assert.Equal("backup", rc.Name);
            Assert.NotNull(allURIs);
            Assert.NotNull(rc.Fetch);
            Assert.NotNull(rc.Push);

            Assert.Equal(2, allURIs.Count);
            Assert.Equal("http://www.spearce.org/egit.git", allURIs[0].ToString());
            Assert.Equal("user@repo.or.cz:/srv/git/egit.git", allURIs[1].ToString());

            Assert.Equal(0, rc.Fetch.Count);

            Assert.Equal(2, rc.Push.Count);
            RefSpec spec = rc.Push[0];
            Assert.True(spec.Force);
            Assert.True(spec.Wildcard);
            Assert.Equal("refs/heads/*", spec.Source);
            Assert.Equal("refs/heads/*", spec.Destination);

            spec = rc.Push[1];
            Assert.False(spec.Force);
            Assert.True(spec.Wildcard);
            Assert.Equal("refs/tags/*", spec.Source);
            Assert.Equal("refs/tags/*", spec.Destination);
        }

        [Fact]
        public void test005_UploadPack()
        {
            WriteConfig("[remote \"example\"]\n"
                        + "url = user@example.com:egit.git\n"
                        + "fetch = +refs/heads/*:refs/remotes/example/*\n"
                        + "uploadpack = /path/to/git/git-upload-pack\n"
                        + "receivepack = /path/to/git/git-receive-pack\n");

            var rc = new RemoteConfig(db.Config, "example");
            System.Collections.Generic.List<URIish> allURIs = rc.URIs;

        	Assert.Equal("example", rc.Name);
            Assert.NotNull(allURIs);
            Assert.NotNull(rc.Fetch);
            Assert.NotNull(rc.Push);

            Assert.Equal(1, allURIs.Count);
            Assert.Equal("user@example.com:egit.git", allURIs[0].ToString());

            Assert.Equal(1, rc.Fetch.Count);
            RefSpec spec = rc.Fetch[0];
            Assert.True(spec.Force);
            Assert.True(spec.Wildcard);
            Assert.Equal("refs/heads/*", spec.Source);
            Assert.Equal("refs/remotes/example/*", spec.Destination);

            Assert.Equal(0, rc.Push.Count);

            Assert.Equal("/path/to/git/git-upload-pack", rc.UploadPack);
            Assert.Equal("/path/to/git/git-receive-pack", rc.ReceivePack);
        }

        [Fact]
        public void test006_Unknown()
        {
            WriteConfig(string.Empty);

            var rc = new RemoteConfig(db.Config, "backup");
            Assert.Equal(0, rc.URIs.Count);
            Assert.Equal(0, rc.Fetch.Count);
            Assert.Equal(0, rc.Push.Count);
            Assert.Equal("git-upload-pack", rc.UploadPack);
            Assert.Equal("git-receive-pack", rc.ReceivePack);
        }

        [Fact]
        public void test007_AddURI()
        {
            WriteConfig(string.Empty);

            var uri = new URIish("/some/dir");
            var rc = new RemoteConfig(db.Config, "backup");
            Assert.Equal(0, rc.URIs.Count);

            Assert.True(rc.AddURI(uri));
            Assert.Equal(1, rc.URIs.Count);
            Assert.Same(uri, rc.URIs[0]);

            Assert.False(rc.AddURI(new URIish(uri.ToString())));
            Assert.Equal(1, rc.URIs.Count);
        }

        [Fact]
        public void test008_RemoveFirstURI()
        {
            WriteConfig(string.Empty);

            var a = new URIish("/some/dir");
            var b = new URIish("/another/dir");
            var c = new URIish("/more/dirs");
            var rc = new RemoteConfig(db.Config, "backup");
            Assert.True(rc.AddURI(a));
            Assert.True(rc.AddURI(b));
            Assert.True(rc.AddURI(c));

            Assert.Equal(3, rc.URIs.Count);
            Assert.Same(a, rc.URIs[0]);
            Assert.Same(b, rc.URIs[1]);
            Assert.Equal(c, rc.URIs[2]);

            Assert.True(rc.RemoveURI(a));
            Assert.Same(b, rc.URIs[0]);
            Assert.Same(c, rc.URIs[1]);
        }

        [Fact]
        public void test009_RemoveMiddleURI()
        {
            WriteConfig(string.Empty);

            var a = new URIish("/some/dir");
            var b = new URIish("/another/dir");
            var c = new URIish("/more/dirs");
            var rc = new RemoteConfig(db.Config, "backup");
            Assert.True(rc.AddURI(a));
            Assert.True(rc.AddURI(b));
            Assert.True(rc.AddURI(c));

            Assert.Equal(3, rc.URIs.Count);
            Assert.Same(a, rc.URIs[0]);
            Assert.Same(b, rc.URIs[1]);
            Assert.Equal(c, rc.URIs[2]);

            Assert.True(rc.RemoveURI(b));
            Assert.Equal(2, rc.URIs.Count);
            Assert.Same(a, rc.URIs[0]);
            Assert.Same(c, rc.URIs[1]);
        }

        [Fact]
        public void test010_RemoveLastURI()
        {
            WriteConfig(string.Empty);

            var a = new URIish("/some/dir");
            var b = new URIish("/another/dir");
            var c = new URIish("/more/dirs");
            var rc = new RemoteConfig(db.Config, "backup");
            Assert.True(rc.AddURI(a));
            Assert.True(rc.AddURI(b));
            Assert.True(rc.AddURI(c));

            Assert.Equal(3, rc.URIs.Count);
            Assert.Same(a, rc.URIs[0]);
            Assert.Same(b, rc.URIs[1]);
            Assert.Equal(c, rc.URIs[2]);

            Assert.True(rc.RemoveURI(c));
            Assert.Equal(2, rc.URIs.Count);
            Assert.Same(a, rc.URIs[0]);
            Assert.Same(b, rc.URIs[1]);
        }

        [Fact]
        public void test011_RemoveOnlyURI()
        {
            WriteConfig(string.Empty);

            var a = new URIish("/some/dir");
            var rc = new RemoteConfig(db.Config, "backup");
            Assert.True(rc.AddURI(a));

            Assert.Equal(1, rc.URIs.Count);
            Assert.Same(a, rc.URIs[0]);

            Assert.True(rc.RemoveURI(a));
            Assert.Equal(0, rc.URIs.Count);
        }

        [Fact]
        public void test012_CreateOrigin()
        {
            var rc = new RemoteConfig(db.Config, "origin");
            rc.AddURI(new URIish("/some/dir"));
            rc.AddFetchRefSpec(new RefSpec("+refs/heads/*:refs/remotes/" + rc.Name + "/*"));
            rc.Update(db.Config);
            db.Config.save();

            checkFile(db.Config.getFile(),
                      "[core]\n"
                      + "\trepositoryformatversion = 0\n" +
                      "\tfilemode = true\n"
                      + "[remote \"origin\"]\n" +
                      "\turl = /some/dir\n"
                      +
                      "\tfetch = +refs/heads/*:refs/remotes/origin/*\n");
        }

        [Fact]
        public void test013_SaveAddURI()
        {
            WriteConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n");

            var rc = new RemoteConfig(db.Config, "spearce");
            rc.AddURI(new URIish("/some/dir"));
            Assert.Equal(2, rc.URIs.Count);
            rc.Update(db.Config);
            db.Config.save();

            checkFile(db.Config.getFile(),
                      "[core]\n"
                      + "\trepositoryformatversion = 0\n" + "\tfilemode = true\n"
                      + "[remote \"spearce\"]\n"
                      + "\turl = http://www.spearce.org/egit.git\n"
                      + "\turl = /some/dir\n"
                      + "\tfetch = +refs/heads/*:refs/remotes/spearce/*\n");
        }

        [Fact]
        public void test014_SaveRemoveLastURI()
        {
            WriteConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "url = /some/dir\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n");

            var rc = new RemoteConfig(db.Config, "spearce");
            Assert.Equal(2, rc.URIs.Count);
            rc.RemoveURI(new URIish("/some/dir"));
            Assert.Equal(1, rc.URIs.Count);
            rc.Update(db.Config);
            db.Config.save();

            checkFile(db.Config.getFile(),
                      "[core]\n"
                      + "\trepositoryformatversion = 0\n" + "\tfilemode = true\n"
                      + "[remote \"spearce\"]\n"
                      + "\turl = http://www.spearce.org/egit.git\n"
                      + "\tfetch = +refs/heads/*:refs/remotes/spearce/*\n");
        }

        [Fact]
        public void test015_SaveRemoveFirstURI()
        {
            WriteConfig("[remote \"spearce\"]\n"
                        + "url = http://www.spearce.org/egit.git\n"
                        + "url = /some/dir\n"
                        + "fetch = +refs/heads/*:refs/remotes/spearce/*\n");

            var rc = new RemoteConfig(db.Config, "spearce");
            Assert.Equal(2, rc.URIs.Count);
            rc.RemoveURI(new URIish("http://www.spearce.org/egit.git"));
            Assert.Equal(1, rc.URIs.Count);
            rc.Update(db.Config);
            db.Config.save();

            checkFile(db.Config.getFile(),
                      "[core]\n"
                      + "\trepositoryformatversion = 0\n" + "\tfilemode = true\n"
                      + "[remote \"spearce\"]\n" + "\turl = /some/dir\n"
                      + "\tfetch = +refs/heads/*:refs/remotes/spearce/*\n");
        }

        [Fact]
        public void test016_SaveNoTags()
        {
            var rc = new RemoteConfig(db.Config, "origin");
            rc.AddURI(new URIish("/some/dir"));
            rc.AddFetchRefSpec(new RefSpec("+refs/heads/*:refs/remotes/" + rc.Name + "/*"));
            rc.SetTagOpt(TagOpt.NO_TAGS);
            rc.Update(db.Config);
            db.Config.save();

            checkFile(db.Config.getFile(),
                      "[core]\n"
                      + "\trepositoryformatversion = 0\n" + "\tfilemode = true\n"
                      + "[remote \"origin\"]\n" + "\turl = /some/dir\n"
                      + "\tfetch = +refs/heads/*:refs/remotes/origin/*\n"
                      + "\ttagopt = --no-tags\n");
        }

        [Fact]
        public void test017_SaveAllTags()
        {
            var rc = new RemoteConfig(db.Config, "origin");
            rc.AddURI(new URIish("/some/dir"));
            rc.AddFetchRefSpec(new RefSpec("+refs/heads/*:refs/remotes/" + rc.Name + "/*"));
            rc.SetTagOpt(TagOpt.FETCH_TAGS);
            rc.Update(db.Config);
            db.Config.save();

            checkFile(db.Config.getFile(),
                      "[core]\n"
                      + "\trepositoryformatversion = 0\n" + "\tfilemode = true\n"
                      + "[remote \"origin\"]\n" + "\turl = /some/dir\n"
                      + "\tfetch = +refs/heads/*:refs/remotes/origin/*\n"
                      + "\ttagopt = --tags\n");
        }
    }
}