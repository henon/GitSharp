/*
 * Copyright (C) 2010, Google Inc.
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
using System.IO;
using GitSharp.Core;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Tests.Util;
using GitSharp.Core.Util;
using GitSharp.Tests.GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core
{
    [TestFixture]
    public class RefDirectoryTest : LocalDiskRepositoryTestCase
    {
        private global::GitSharp.Core.Repository diskRepo;

        private TestRepository repo;

        private RefDirectory refdir;

        private RevCommit A;

        private RevCommit B;

        private RevTag v1_0;

        [SetUp]
        public override void setUp()
        {
            base.setUp();

            diskRepo = createBareRepository();
            refdir = (RefDirectory)diskRepo.RefDatabase;

            repo = new TestRepository(diskRepo);
            A = repo.commit().create();
            B = repo.commit(repo.getRevWalk().parseCommit(A));
            v1_0 = repo.tag("v1_0", B);
            repo.getRevWalk().parseBody(v1_0);
        }

        [Test]
        public void testCreate()
        {
            // setUp above created the directory. We just have to test it.
            DirectoryInfo d = diskRepo.Directory;
            Assert.AreSame(diskRepo, refdir.getRepository());

            Assert.IsTrue(PathUtil.CombineDirectoryPath(d, "refs").IsDirectory());
            Assert.IsTrue(PathUtil.CombineDirectoryPath(d, "logs").IsDirectory());
            Assert.IsTrue(PathUtil.CombineDirectoryPath(d, "logs/refs").IsDirectory());
            Assert.IsFalse(PathUtil.CombineFilePath(d, "packed-refs").Exists);

            Assert.IsTrue(PathUtil.CombineDirectoryPath(d, "refs/heads").IsDirectory());
            Assert.IsTrue(PathUtil.CombineDirectoryPath(d, "refs/tags").IsDirectory());
            Assert.AreEqual(2, PathUtil.CombineDirectoryPath(d, "refs").GetFileSystemInfos().Length);
            Assert.AreEqual(0, PathUtil.CombineDirectoryPath(d, "refs/heads").GetFileSystemInfos().Length);
            Assert.AreEqual(0, PathUtil.CombineDirectoryPath(d, "refs/tags").GetFileSystemInfos().Length);

            Assert.IsTrue(PathUtil.CombineDirectoryPath(d, "logs/refs/heads").IsDirectory());
            Assert.IsFalse(PathUtil.CombineFilePath(d, "logs/HEAD").Exists);
            Assert.AreEqual(0, PathUtil.CombineDirectoryPath(d, "logs/refs/heads").GetFileSystemInfos().Length);

            Assert.AreEqual("ref: refs/heads/master\n", read(PathUtil.CombineFilePath(d, Constants.HEAD)));
        }

        [Test]
        public void testGetRefs_EmptyDatabase()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;

            all = refdir.getRefs(RefDatabase.ALL);
            Assert.IsTrue(all.isEmpty(), "no references");

            all = refdir.getRefs(Constants.R_HEADS);
            Assert.IsTrue(all.isEmpty(), "no references");

            all = refdir.getRefs(Constants.R_TAGS);
            Assert.IsTrue(all.isEmpty(), "no references");
        }

        [Test]
        public void testGetRefs_HeadOnOneBranch()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;
            global::GitSharp.Core.Ref head, master;

            writeLooseRef("refs/heads/master", A);

            all = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(2, all.size());
            Assert.IsTrue(all.ContainsKey(Constants.HEAD), "has HEAD");
            Assert.IsTrue(all.ContainsKey("refs/heads/master"), "has master");

            head = all.get(Constants.HEAD);
            master = all.get("refs/heads/master");

            Assert.AreEqual(Constants.HEAD, head.Name);
            Assert.IsTrue(head.isSymbolic());
            Assert.AreSame(Storage.Loose, head.StorageFormat);
            Assert.AreSame(master, head.getTarget(), "uses same ref as target");

            Assert.AreEqual("refs/heads/master", master.Name);
            Assert.IsFalse(master.isSymbolic());
            Assert.AreSame(Storage.Loose, master.StorageFormat);
            Assert.AreEqual(A, master.ObjectId);
        }

        [Test]
        public void testGetRefs_DeatchedHead1()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;
            global::GitSharp.Core.Ref head;

            writeLooseRef(Constants.HEAD, A);
            BUG_WorkAroundRacyGitIssues(Constants.HEAD);

            all = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(1, all.size());
            Assert.IsTrue(all.ContainsKey(Constants.HEAD), "has HEAD");

            head = all.get(Constants.HEAD);

            Assert.AreEqual(Constants.HEAD, head.Name);
            Assert.IsFalse(head.isSymbolic());
            Assert.AreSame(global::GitSharp.Core.Storage.Loose, head.StorageFormat);
            Assert.AreEqual(A, head.ObjectId);
        }

        [Test]
        public void testGetRefs_DeatchedHead2()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;
            global::GitSharp.Core.Ref head, master;

            writeLooseRef(Constants.HEAD, A);
            writeLooseRef("refs/heads/master", B);
            BUG_WorkAroundRacyGitIssues(Constants.HEAD);

            all = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(2, all.size());

            head = all.get(Constants.HEAD);
            master = all.get("refs/heads/master");

            Assert.AreEqual(Constants.HEAD, head.getName());
            Assert.IsFalse(head.isSymbolic());
            Assert.AreSame(Storage.Loose, head.getStorage());
            Assert.AreEqual(A, head.getObjectId());

            Assert.AreEqual("refs/heads/master", master.getName());
            Assert.IsFalse(master.isSymbolic());
            Assert.AreSame(Storage.Loose, master.getStorage());
            Assert.AreEqual(B, master.getObjectId());
        }

        [Test]
        public void testGetRefs_DeeplyNestedBranch()
        {
            string name = "refs/heads/a/b/c/d/e/f/g/h/i/j/k";
            IDictionary<string, global::GitSharp.Core.Ref> all;
            global::GitSharp.Core.Ref r;

            writeLooseRef(name, A);

            all = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(1, all.size());

            r = all.get(name);
            Assert.AreEqual(name, r.getName());
            Assert.IsFalse(r.isSymbolic());
            Assert.AreSame(Storage.Loose, r.getStorage());
            Assert.AreEqual(A, r.getObjectId());
        }

        [Test]
        public void testGetRefs_HeadBranchNotBorn()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;
            global::GitSharp.Core.Ref a, b;

            writeLooseRef("refs/heads/A", A);
            writeLooseRef("refs/heads/B", B);

            all = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(2, all.size());
            Assert.IsFalse(all.ContainsKey(Constants.HEAD), "no HEAD");

            a = all.get("refs/heads/A");
            b = all.get("refs/heads/B");

            Assert.AreEqual(A, a.getObjectId());
            Assert.AreEqual(B, b.getObjectId());

            Assert.AreEqual("refs/heads/A", a.getName());
            Assert.AreEqual("refs/heads/B", b.getName());
        }

        [Test]
        public void testGetRefs_LooseOverridesPacked()
        {
            IDictionary<string, global::GitSharp.Core.Ref> heads;
            global::GitSharp.Core.Ref a;

            writeLooseRef("refs/heads/master", B);
            writePackedRef("refs/heads/master", A);

            heads = refdir.getRefs(Constants.R_HEADS);
            Assert.AreEqual(1, heads.size());

            a = heads.get("master");
            Assert.AreEqual("refs/heads/master", a.getName());
            Assert.AreEqual(B, a.getObjectId());
        }

        [Test]
        public void testGetRefs_IgnoresGarbageRef1()
        {
            IDictionary<string, global::GitSharp.Core.Ref> heads;
            global::GitSharp.Core.Ref a;

            writeLooseRef("refs/heads/A", A);
            write(PathUtil.CombineFilePath(diskRepo.Directory, "refs/heads/bad"), "FAIL\n");

            heads = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(1, heads.size());

            a = heads.get("refs/heads/A");
            Assert.AreEqual("refs/heads/A", a.getName());
            Assert.AreEqual(A, a.getObjectId());
        }

        [Test]
        public void testGetRefs_IgnoresGarbageRef2()
        {
            IDictionary<string, global::GitSharp.Core.Ref> heads;
            global::GitSharp.Core.Ref a;

            writeLooseRef("refs/heads/A", A);
            write(PathUtil.CombineFilePath(diskRepo.Directory, "refs/heads/bad"), "");

            heads = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(1, heads.size());

            a = heads.get("refs/heads/A");
            Assert.AreEqual("refs/heads/A", a.getName());
            Assert.AreEqual(A, a.getObjectId());
        }

        [Test]
        public void testGetRefs_IgnoresGarbageRef3()
        {
            IDictionary<string, global::GitSharp.Core.Ref> heads;
            global::GitSharp.Core.Ref a;

            writeLooseRef("refs/heads/A", A);
            write(PathUtil.CombineFilePath(diskRepo.Directory, "refs/heads/bad"), "\n");

            heads = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(1, heads.size());

            a = heads.get("refs/heads/A");
            Assert.AreEqual("refs/heads/A", a.getName());
            Assert.AreEqual(A, a.getObjectId());
        }

        [Test]
        public void testGetRefs_IgnoresGarbageRef4()
        {
            IDictionary<string, global::GitSharp.Core.Ref> heads;
            global::GitSharp.Core.Ref a, b, c;

            writeLooseRef("refs/heads/A", A);
            writeLooseRef("refs/heads/B", B);
            writeLooseRef("refs/heads/C", A);
            heads = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(3, heads.size());
            Assert.IsTrue(heads.ContainsKey("refs/heads/A"));
            Assert.IsTrue(heads.ContainsKey("refs/heads/B"));
            Assert.IsTrue(heads.ContainsKey("refs/heads/C"));

            writeLooseRef("refs/heads/B", "FAIL\n");
            BUG_WorkAroundRacyGitIssues("refs/heads/B");

            heads = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(2, heads.size());

            a = heads.get("refs/heads/A");
            b = heads.get("refs/heads/B");
            c = heads.get("refs/heads/C");

            Assert.AreEqual("refs/heads/A", a.getName());
            Assert.AreEqual(A, a.getObjectId());

            Assert.IsNull(b, "no refs/heads/B");

            Assert.AreEqual("refs/heads/C", c.getName());
            Assert.AreEqual(A, c.getObjectId());
        }

        [Test]
        public void testGetRefs_InvalidName()
        {
            writeLooseRef("refs/heads/A", A);

            Assert.IsTrue(refdir.getRefs("refs/heads").isEmpty(), "empty refs/heads");
            Assert.IsTrue(refdir.getRefs("objects").isEmpty(), "empty objects");
            Assert.IsTrue(refdir.getRefs("objects/").isEmpty(), "empty objects/");
        }

        [Test]
        public void testGetRefs_HeadsOnly_AllLoose()
        {
            IDictionary<string, global::GitSharp.Core.Ref> heads;
            global::GitSharp.Core.Ref a, b;

            writeLooseRef("refs/heads/A", A);
            writeLooseRef("refs/heads/B", B);
            writeLooseRef("refs/tags/v1.0", v1_0);

            heads = refdir.getRefs(Constants.R_HEADS);
            Assert.AreEqual(2, heads.size());

            a = heads.get("A");
            b = heads.get("B");

            Assert.AreEqual("refs/heads/A", a.getName());
            Assert.AreEqual("refs/heads/B", b.getName());

            Assert.AreEqual(A, a.getObjectId());
            Assert.AreEqual(B, b.getObjectId());
        }

        [Test]
        public void testGetRefs_HeadsOnly_AllPacked1()
        {
            IDictionary<string, global::GitSharp.Core.Ref> heads;
            global::GitSharp.Core.Ref a;

            deleteLooseRef(Constants.HEAD);
            writePackedRef("refs/heads/A", A);

            heads = refdir.getRefs(Constants.R_HEADS);
            Assert.AreEqual(1, heads.size());

            a = heads.get("A");

            Assert.AreEqual("refs/heads/A", a.getName());
            Assert.AreEqual(A, a.getObjectId());
        }

        [Test]
        public void testGetRefs_HeadsOnly_SymrefToPacked()
        {
            IDictionary<string, global::GitSharp.Core.Ref> heads;
            global::GitSharp.Core.Ref master, other;

            writeLooseRef("refs/heads/other", "ref: refs/heads/master\n");
            writePackedRef("refs/heads/master", A);

            heads = refdir.getRefs(Constants.R_HEADS);
            Assert.AreEqual(2, heads.size());

            master = heads.get("master");
            other = heads.get("other");

            Assert.AreEqual("refs/heads/master", master.getName());
            Assert.AreEqual(A, master.getObjectId());

            Assert.AreEqual("refs/heads/other", other.getName());
            Assert.AreEqual(A, other.getObjectId());
            Assert.AreSame(master, other.getTarget());
        }

        [Test]
        public void testGetRefs_HeadsOnly_Mixed()
        {
            IDictionary<string, global::GitSharp.Core.Ref> heads;
            global::GitSharp.Core.Ref a, b;

            writeLooseRef("refs/heads/A", A);
            writeLooseRef("refs/heads/B", B);
            writePackedRef("refs/tags/v1.0", v1_0);

            heads = refdir.getRefs(Constants.R_HEADS);
            Assert.AreEqual(2, heads.size());

            a = heads.get("A");
            b = heads.get("B");

            Assert.AreEqual("refs/heads/A", a.getName());
            Assert.AreEqual("refs/heads/B", b.getName());

            Assert.AreEqual(A, a.getObjectId());
            Assert.AreEqual(B, b.getObjectId());
        }

        [Test]
        public void testGetRefs_TagsOnly_AllLoose()
        {
            IDictionary<string, global::GitSharp.Core.Ref> tags;
            global::GitSharp.Core.Ref a;

            writeLooseRef("refs/heads/A", A);
            writeLooseRef("refs/tags/v1.0", v1_0);

            tags = refdir.getRefs(Constants.R_TAGS);
            Assert.AreEqual(1, tags.size());

            a = tags.get("v1.0");

            Assert.AreEqual("refs/tags/v1.0", a.getName());
            Assert.AreEqual(v1_0, a.getObjectId());
        }

        [Test]
        public void testGetRefs_TagsOnly_AllPacked()
        {
            IDictionary<string, global::GitSharp.Core.Ref> tags;
            global::GitSharp.Core.Ref a;

            deleteLooseRef(Constants.HEAD);
            writePackedRef("refs/tags/v1.0", v1_0);

            tags = refdir.getRefs(Constants.R_TAGS);
            Assert.AreEqual(1, tags.size());

            a = tags.get("v1.0");

            Assert.AreEqual("refs/tags/v1.0", a.getName());
            Assert.AreEqual(v1_0, a.getObjectId());
        }

        [Test]
        public void testGetRefs_DiscoversNewLoose1()
        {
            IDictionary<string, global::GitSharp.Core.Ref> orig, next;
            global::GitSharp.Core.Ref orig_r, next_r;

            writeLooseRef("refs/heads/master", A);
            orig = refdir.getRefs(RefDatabase.ALL);

            writeLooseRef("refs/heads/next", B);
            next = refdir.getRefs(RefDatabase.ALL);

            Assert.AreEqual(2, orig.size());
            Assert.AreEqual(3, next.size());

            Assert.IsFalse(orig.ContainsKey("refs/heads/next"));
            Assert.IsTrue(next.ContainsKey("refs/heads/next"));

            orig_r = orig.get("refs/heads/master");
            next_r = next.get("refs/heads/master");
            Assert.AreEqual(A, orig_r.getObjectId());
            Assert.AreSame(orig_r, next_r, "uses cached instance");
            Assert.AreSame(orig_r, orig.get(Constants.HEAD).getTarget(), "same HEAD");
            Assert.AreSame(orig_r, next.get(Constants.HEAD).getTarget(), "same HEAD");

            next_r = next.get("refs/heads/next");
            Assert.AreSame(Storage.Loose, next_r.getStorage());
            Assert.AreEqual(B, next_r.getObjectId());
        }

        [Test]
        public void testGetRefs_DiscoversNewLoose2()
        {
            IDictionary<string, global::GitSharp.Core.Ref> orig, next, news;

            writeLooseRef("refs/heads/pu", A);
            orig = refdir.getRefs(RefDatabase.ALL);

            writeLooseRef("refs/heads/new/B", B);
            news = refdir.getRefs("refs/heads/new/");
            next = refdir.getRefs(RefDatabase.ALL);

            Assert.AreEqual(1, orig.size());
            Assert.AreEqual(2, next.size());
            Assert.AreEqual(1, news.size());

            Assert.IsTrue(orig.ContainsKey("refs/heads/pu"));
            Assert.IsTrue(next.ContainsKey("refs/heads/pu"));
            Assert.IsFalse(news.ContainsKey("refs/heads/pu"));

            Assert.IsFalse(orig.ContainsKey("refs/heads/new/B"));
            Assert.IsTrue(next.ContainsKey("refs/heads/new/B"));
            Assert.IsTrue(news.ContainsKey("B"));
        }

        [Test]
        public void testGetRefs_DiscoversModifiedLoose()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;

            writeLooseRef("refs/heads/master", A);
            all = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(A, all.get(Constants.HEAD).getObjectId());

            writeLooseRef("refs/heads/master", B);
            BUG_WorkAroundRacyGitIssues("refs/heads/master");
            all = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(B, all.get(Constants.HEAD).getObjectId());
        }

        [Test]
        public void testGetRef_DiscoversModifiedLoose()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;

            writeLooseRef("refs/heads/master", A);
            all = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(A, all.get(Constants.HEAD).getObjectId());

            writeLooseRef("refs/heads/master", B);
            BUG_WorkAroundRacyGitIssues("refs/heads/master");

            global::GitSharp.Core.Ref master = refdir.getRef("refs/heads/master");
            Assert.AreEqual(B, master.getObjectId());
        }

        [Test]
        public void testGetRefs_DiscoversDeletedLoose1()
        {
            IDictionary<string, global::GitSharp.Core.Ref> orig, next;
            global::GitSharp.Core.Ref orig_r, next_r;

            writeLooseRef("refs/heads/B", B);
            writeLooseRef("refs/heads/master", A);
            orig = refdir.getRefs(RefDatabase.ALL);

            deleteLooseRef("refs/heads/B");
            next = refdir.getRefs(RefDatabase.ALL);

            Assert.AreEqual(3, orig.size());
            Assert.AreEqual(2, next.size());

            Assert.IsTrue(orig.ContainsKey("refs/heads/B"));
            Assert.IsFalse(next.ContainsKey("refs/heads/B"));

            orig_r = orig.get("refs/heads/master");
            next_r = next.get("refs/heads/master");
            Assert.AreEqual(A, orig_r.getObjectId());
            Assert.AreSame(orig_r, next_r, "uses cached instance");
            Assert.AreSame(orig_r, orig.get(Constants.HEAD).getTarget(), "same HEAD");
            Assert.AreSame(orig_r, next.get(Constants.HEAD).getTarget(), "same HEAD");

            orig_r = orig.get("refs/heads/B");
            Assert.AreSame(Storage.Loose, orig_r.getStorage());
            Assert.AreEqual(B, orig_r.getObjectId());
        }

        [Test]
        public void testGetRef_DiscoversDeletedLoose()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;

            writeLooseRef("refs/heads/master", A);
            all = refdir.getRefs(RefDatabase.ALL);
            Assert.AreEqual(A, all.get(Constants.HEAD).getObjectId());

            deleteLooseRef("refs/heads/master");
            Assert.IsNull(refdir.getRef("refs/heads/master"));
            Assert.IsTrue(refdir.getRefs(RefDatabase.ALL).isEmpty());
        }

        [Test]
        public void testGetRefs_DiscoversDeletedLoose2()
        {
            IDictionary<string, global::GitSharp.Core.Ref> orig, next;

            writeLooseRef("refs/heads/master", A);
            writeLooseRef("refs/heads/pu", B);
            orig = refdir.getRefs(RefDatabase.ALL);

            deleteLooseRef("refs/heads/pu");
            next = refdir.getRefs(RefDatabase.ALL);

            Assert.AreEqual(3, orig.size());
            Assert.AreEqual(2, next.size());

            Assert.IsTrue(orig.ContainsKey("refs/heads/pu"));
            Assert.IsFalse(next.ContainsKey("refs/heads/pu"));
        }

        [Test]
        public void testGetRefs_DiscoversDeletedLoose3()
        {
            IDictionary<string, global::GitSharp.Core.Ref> orig, next;

            writeLooseRef("refs/heads/master", A);
            writeLooseRef("refs/heads/next", B);
            writeLooseRef("refs/heads/pu", B);
            writeLooseRef("refs/tags/v1.0", v1_0);
            orig = refdir.getRefs(RefDatabase.ALL);

            deleteLooseRef("refs/heads/pu");
            deleteLooseRef("refs/heads/next");
            next = refdir.getRefs(RefDatabase.ALL);

            Assert.AreEqual(5, orig.size());
            Assert.AreEqual(3, next.size());

            Assert.IsTrue(orig.ContainsKey("refs/heads/pu"));
            Assert.IsTrue(orig.ContainsKey("refs/heads/next"));
            Assert.IsFalse(next.ContainsKey("refs/heads/pu"));
            Assert.IsFalse(next.ContainsKey("refs/heads/next"));
        }

        [Test]
        public void testGetRefs_DiscoversDeletedLoose4()
        {
            IDictionary<string, global::GitSharp.Core.Ref> orig, next;
            global::GitSharp.Core.Ref orig_r, next_r;

            writeLooseRef("refs/heads/B", B);
            writeLooseRef("refs/heads/master", A);
            orig = refdir.getRefs(RefDatabase.ALL);

            deleteLooseRef("refs/heads/master");
            next = refdir.getRefs("refs/heads/");

            Assert.AreEqual(3, orig.size());
            Assert.AreEqual(1, next.size());

            Assert.IsTrue(orig.ContainsKey("refs/heads/B"));
            Assert.IsTrue(orig.ContainsKey("refs/heads/master"));
            Assert.IsTrue(next.ContainsKey("B"));
            Assert.IsFalse(next.ContainsKey("master"));

            orig_r = orig.get("refs/heads/B");
            next_r = next.get("B");
            Assert.AreEqual(B, orig_r.getObjectId());
            Assert.AreSame(orig_r, next_r, "uses cached instance");
        }

        [Test]
        public void testGetRefs_DiscoversDeletedLoose5()
        {
            IDictionary<string, global::GitSharp.Core.Ref> orig, next;

            writeLooseRef("refs/heads/master", A);
            writeLooseRef("refs/heads/pu", B);
            orig = refdir.getRefs(RefDatabase.ALL);

            deleteLooseRef("refs/heads/pu");
            writeLooseRef("refs/tags/v1.0", v1_0);
            next = refdir.getRefs(RefDatabase.ALL);

            Assert.AreEqual(3, orig.size());
            Assert.AreEqual(3, next.size());

            Assert.IsTrue(orig.ContainsKey("refs/heads/pu"));
            Assert.IsFalse(orig.ContainsKey("refs/tags/v1.0"));
            Assert.IsFalse(next.ContainsKey("refs/heads/pu"));
            Assert.IsTrue(next.ContainsKey("refs/tags/v1.0"));
        }

        [Test]
        public void testGetRefs_SkipsLockFiles()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;

            writeLooseRef("refs/heads/master", A);
            writeLooseRef("refs/heads/pu.lock", B);
            all = refdir.getRefs(RefDatabase.ALL);

            Assert.AreEqual(2, all.size());

            Assert.IsTrue(all.ContainsKey(Constants.HEAD));
            Assert.IsTrue(all.ContainsKey("refs/heads/master"));
            Assert.IsFalse(all.ContainsKey("refs/heads/pu.lock"));
        }

        [Test]
        public void testGetRefs_CycleInSymbolicRef()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;
            global::GitSharp.Core.Ref r;

            writeLooseRef("refs/1", "ref: refs/2\n");
            writeLooseRef("refs/2", "ref: refs/3\n");
            writeLooseRef("refs/3", "ref: refs/4\n");
            writeLooseRef("refs/4", "ref: refs/5\n");
            writeLooseRef("refs/5", "ref: refs/end\n");
            writeLooseRef("refs/end", A);

            all = refdir.getRefs(RefDatabase.ALL);
            r = all.get("refs/1");
            Assert.IsNotNull(r, "has 1");

            Assert.AreEqual("refs/1", r.getName());
            Assert.AreEqual(A, r.getObjectId());
            Assert.IsTrue(r.isSymbolic());

            r = r.getTarget();
            Assert.AreEqual("refs/2", r.getName());
            Assert.AreEqual(A, r.getObjectId());
            Assert.IsTrue(r.isSymbolic());

            r = r.getTarget();
            Assert.AreEqual("refs/3", r.getName());
            Assert.AreEqual(A, r.getObjectId());
            Assert.IsTrue(r.isSymbolic());

            r = r.getTarget();
            Assert.AreEqual("refs/4", r.getName());
            Assert.AreEqual(A, r.getObjectId());
            Assert.IsTrue(r.isSymbolic());

            r = r.getTarget();
            Assert.AreEqual("refs/5", r.getName());
            Assert.AreEqual(A, r.getObjectId());
            Assert.IsTrue(r.isSymbolic());

            r = r.getTarget();
            Assert.AreEqual("refs/end", r.getName());
            Assert.AreEqual(A, r.getObjectId());
            Assert.IsFalse(r.isSymbolic());

            writeLooseRef("refs/5", "ref: refs/6\n");
            writeLooseRef("refs/6", "ref: refs/end\n");
            BUG_WorkAroundRacyGitIssues("refs/5");
            all = refdir.getRefs(RefDatabase.ALL);
            r = all.get("refs/1");
            Assert.IsNull(r, "mising 1 due to cycle");
        }

        [Test]
        public void testGetRefs_PackedNotPeeled_Sorted()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;

            writePackedRefs("" + //
                            A.Name + " refs/heads/master\n" + //
                            B.Name + " refs/heads/other\n" + //
                            v1_0.Name + " refs/tags/v1.0\n");
            all = refdir.getRefs(RefDatabase.ALL);

            Assert.AreEqual(4, all.size());
            global::GitSharp.Core.Ref head = all.get(Constants.HEAD);
            global::GitSharp.Core.Ref master = all.get("refs/heads/master");
            global::GitSharp.Core.Ref other = all.get("refs/heads/other");
            global::GitSharp.Core.Ref tag = all.get("refs/tags/v1.0");

            Assert.AreEqual(A, master.getObjectId());
            Assert.IsFalse(master.isPeeled());
            Assert.IsNull(master.getPeeledObjectId());

            Assert.AreEqual(B, other.getObjectId());
            Assert.IsFalse(other.isPeeled());
            Assert.IsNull(other.getPeeledObjectId());

            Assert.AreSame(master, head.getTarget());
            Assert.AreEqual(A, head.getObjectId());
            Assert.IsFalse(head.isPeeled());
            Assert.IsNull(head.getPeeledObjectId());

            Assert.AreEqual(v1_0, tag.getObjectId());
            Assert.IsFalse(tag.isPeeled());
            Assert.IsNull(tag.getPeeledObjectId());
        }

        [Test]
        public void testGetRef_PackedNotPeeled_WrongSort()
        {
            writePackedRefs("" + //
                            v1_0.Name + " refs/tags/v1.0\n" + //
                            B.Name + " refs/heads/other\n" + //
                            A.Name + " refs/heads/master\n");

            global::GitSharp.Core.Ref head = refdir.getRef(Constants.HEAD);
            global::GitSharp.Core.Ref master = refdir.getRef("refs/heads/master");
            global::GitSharp.Core.Ref other = refdir.getRef("refs/heads/other");
            global::GitSharp.Core.Ref tag = refdir.getRef("refs/tags/v1.0");

            Assert.AreEqual(A, master.getObjectId());
            Assert.IsFalse(master.isPeeled());
            Assert.IsNull(master.getPeeledObjectId());

            Assert.AreEqual(B, other.getObjectId());
            Assert.IsFalse(other.isPeeled());
            Assert.IsNull(other.getPeeledObjectId());

            Assert.AreSame(master, head.getTarget());
            Assert.AreEqual(A, head.getObjectId());
            Assert.IsFalse(head.isPeeled());
            Assert.IsNull(head.getPeeledObjectId());

            Assert.AreEqual(v1_0, tag.getObjectId());
            Assert.IsFalse(tag.isPeeled());
            Assert.IsNull(tag.getPeeledObjectId());
        }

        [Test]
        public void testGetRefs_PackedWithPeeled()
        {
            IDictionary<string, global::GitSharp.Core.Ref> all;

            writePackedRefs("# pack-refs with: peeled \n" + //
                            A.Name + " refs/heads/master\n" + //
                            B.Name + " refs/heads/other\n" + //
                            v1_0.Name + " refs/tags/v1.0\n" + //
                            "^" + v1_0.getObject().Name + "\n");
            all = refdir.getRefs(RefDatabase.ALL);

            Assert.AreEqual(4, all.size());
            global::GitSharp.Core.Ref head = all.get(Constants.HEAD);
            global::GitSharp.Core.Ref master = all.get("refs/heads/master");
            global::GitSharp.Core.Ref other = all.get("refs/heads/other");
            global::GitSharp.Core.Ref tag = all.get("refs/tags/v1.0");

            Assert.AreEqual(A, master.getObjectId());
            Assert.IsTrue(master.isPeeled());
            Assert.IsNull(master.getPeeledObjectId());

            Assert.AreEqual(B, other.getObjectId());
            Assert.IsTrue(other.isPeeled());
            Assert.IsNull(other.getPeeledObjectId());

            Assert.AreSame(master, head.getTarget());
            Assert.AreEqual(A, head.getObjectId());
            Assert.IsTrue(head.isPeeled());
            Assert.IsNull(head.getPeeledObjectId());

            Assert.AreEqual(v1_0, tag.getObjectId());
            Assert.IsTrue(tag.isPeeled());
            Assert.AreEqual(v1_0.getObject(), tag.getPeeledObjectId());
        }

        [Test]
        public void testGetRef_EmptyDatabase()
        {
            global::GitSharp.Core.Ref r;

            r = refdir.getRef(Constants.HEAD);
            Assert.IsTrue(r.isSymbolic());
            Assert.AreSame(Storage.Loose, r.getStorage());
            Assert.AreEqual("refs/heads/master", r.getTarget().getName());
            Assert.AreSame(Storage.New, r.getTarget().getStorage());
            Assert.IsNull(r.getTarget().getObjectId());

            Assert.IsNull(refdir.getRef("refs/heads/master"));
            Assert.IsNull(refdir.getRef("refs/tags/v1.0"));
            Assert.IsNull(refdir.getRef("FETCH_HEAD"));
            Assert.IsNull(refdir.getRef("NOT.A.REF.NAME"));
            Assert.IsNull(refdir.getRef("master"));
            Assert.IsNull(refdir.getRef("v1.0"));
        }

        [Test]
        public void testGetRef_FetchHead()
        {
            // This is an odd special case where we need to make sure we read
            // exactly the first 40 bytes of the file and nothing further on
            // that line, or the remainder of the file.
            write(PathUtil.CombineFilePath(diskRepo.Directory, "FETCH_HEAD"), A.Name
                                                                              + "\tnot-for-merge"
                                                                              + "\tbranch 'master' of git://egit.eclipse.org/jgit\n");

            global::GitSharp.Core.Ref r = refdir.getRef("FETCH_HEAD");
            Assert.IsFalse(r.isSymbolic());
            Assert.AreEqual(A, r.getObjectId());
            Assert.AreEqual("FETCH_HEAD", r.getName());
            Assert.IsFalse(r.isPeeled());
            Assert.IsNull(r.getPeeledObjectId());
        }

        [Test]
        public void testGetRef_AnyHeadWithGarbage()
        {
            write(PathUtil.CombineFilePath(diskRepo.Directory, "refs/heads/A"), A.Name
                                                                                + "012345 . this is not a standard reference\n"
                                                                                + "#and even more junk\n");

            global::GitSharp.Core.Ref r = refdir.getRef("refs/heads/A");
            Assert.IsFalse(r.isSymbolic());
            Assert.AreEqual(A, r.getObjectId());
            Assert.AreEqual("refs/heads/A", r.getName());
            Assert.IsFalse(r.isPeeled());
            Assert.IsNull(r.getPeeledObjectId());
        }

        [Test]
        public void testGetRefs_CorruptSymbolicReference()
        {
            string name = "refs/heads/A";
            writeLooseRef(name, "ref: \n");
            Assert.IsTrue(refdir.getRefs(RefDatabase.ALL).isEmpty());
        }

        [Test]
        public void testGetRef_CorruptSymbolicReference()
        {
            string name = "refs/heads/A";
            writeLooseRef(name, "ref: \n");
            try
            {
                refdir.getRef(name);
                Assert.Fail("read an invalid reference");
            }
            catch (IOException err)
            {
                string msg = err.Message;
                Assert.AreEqual("Not a ref: " + name + ": ref:", msg);
            }
        }

        [Test]
        public void testGetRefs_CorruptObjectIdReference()
        {
            string name = "refs/heads/A";
            string content = "zoo" + A.Name;
            writeLooseRef(name, content + "\n");
            Assert.IsTrue(refdir.getRefs(RefDatabase.ALL).isEmpty());
        }

        [Test]
        public void testGetRef_CorruptObjectIdReference()
        {
            string name = "refs/heads/A";
            string content = "zoo" + A.Name;
            writeLooseRef(name, content + "\n");
            try
            {
                refdir.getRef(name);
                Assert.Fail("read an invalid reference");
            }
            catch (IOException err)
            {
                string msg = err.Message;
                Assert.AreEqual("Not a ref: " + name + ": " + content, msg);
            }
        }

        [Test]
        public void testIsNameConflicting()
        {
            writeLooseRef("refs/heads/a/b", A);
            writePackedRef("refs/heads/q", B);

            // new references cannot replace an existing container
            Assert.IsTrue(refdir.isNameConflicting("refs"));
            Assert.IsTrue(refdir.isNameConflicting("refs/heads"));
            Assert.IsTrue(refdir.isNameConflicting("refs/heads/a"));

            // existing reference is not conflicting
            Assert.IsFalse(refdir.isNameConflicting("refs/heads/a/b"));

            // new references are not conflicting
            Assert.IsFalse(refdir.isNameConflicting("refs/heads/a/d"));
            Assert.IsFalse(refdir.isNameConflicting("refs/heads/master"));

            // existing reference must not be used as a container
            Assert.IsTrue(refdir.isNameConflicting("refs/heads/a/b/c"));
            Assert.IsTrue(refdir.isNameConflicting("refs/heads/q/master"));
        }

        [Test]
        public void testPeelLooseTag()
        {
            writeLooseRef("refs/tags/v1_0", v1_0);
            writeLooseRef("refs/tags/current", "ref: refs/tags/v1_0\n");

            global::GitSharp.Core.Ref tag = refdir.getRef("refs/tags/v1_0");
            global::GitSharp.Core.Ref cur = refdir.getRef("refs/tags/current");

            Assert.AreEqual(v1_0, tag.getObjectId());
            Assert.IsFalse(tag.isSymbolic());
            Assert.IsFalse(tag.isPeeled());
            Assert.IsNull(tag.getPeeledObjectId());

            Assert.AreEqual(v1_0, cur.getObjectId());
            Assert.IsTrue(cur.isSymbolic());
            Assert.IsFalse(cur.isPeeled());
            Assert.IsNull(cur.getPeeledObjectId());

            global::GitSharp.Core.Ref tag_p = refdir.peel(tag);
            global::GitSharp.Core.Ref cur_p = refdir.peel(cur);

            Assert.AreNotSame(tag, tag_p);
            Assert.IsFalse(tag_p.isSymbolic());
            Assert.IsTrue(tag_p.isPeeled());
            Assert.AreEqual(v1_0, tag_p.getObjectId());
            Assert.AreEqual(v1_0.getObject(), tag_p.getPeeledObjectId());
            Assert.AreSame(tag_p, refdir.peel(tag_p));

            Assert.AreNotSame(cur, cur_p);
            Assert.AreEqual("refs/tags/current", cur_p.getName());
            Assert.IsTrue(cur_p.isSymbolic());
            Assert.AreEqual("refs/tags/v1_0", cur_p.getTarget().getName());
            Assert.IsTrue(cur_p.isPeeled());
            Assert.AreEqual(v1_0, cur_p.getObjectId());
            Assert.AreEqual(v1_0.getObject(), cur_p.getPeeledObjectId());

            // reuses cached peeling later, but not immediately due to
            // the implementation so we have to fetch it once.
            global::GitSharp.Core.Ref tag_p2 = refdir.getRef("refs/tags/v1_0");
            Assert.IsFalse(tag_p2.isSymbolic());
            Assert.IsTrue(tag_p2.isPeeled());
            Assert.AreEqual(v1_0, tag_p2.getObjectId());
            Assert.AreEqual(v1_0.getObject(), tag_p2.getPeeledObjectId());

            Assert.AreSame(tag_p2, refdir.getRef("refs/tags/v1_0"));
            Assert.AreSame(tag_p2, refdir.getRef("refs/tags/current").getTarget());
            Assert.AreSame(tag_p2, refdir.peel(tag_p2));
        }

        [Test]
        public void testPeelCommit()
        {
            writeLooseRef("refs/heads/master", A);

            global::GitSharp.Core.Ref master = refdir.getRef("refs/heads/master");
            Assert.AreEqual(A, master.getObjectId());
            Assert.IsFalse(master.isPeeled());
            Assert.IsNull(master.getPeeledObjectId());

            global::GitSharp.Core.Ref master_p = refdir.peel(master);
            Assert.AreNotSame(master, master_p);
            Assert.AreEqual(A, master_p.getObjectId());
            Assert.IsTrue(master_p.isPeeled());
            Assert.IsNull(master_p.getPeeledObjectId());

            // reuses cached peeling later, but not immediately due to
            // the implementation so we have to fetch it once.
            global::GitSharp.Core.Ref master_p2 = refdir.getRef("refs/heads/master");
            Assert.AreNotSame(master, master_p2);
            Assert.AreEqual(A, master_p2.getObjectId());
            Assert.IsTrue(master_p2.isPeeled());
            Assert.IsNull(master_p2.getPeeledObjectId());
            Assert.AreSame(master_p2, refdir.peel(master_p2));
        }

        private void writeLooseRef(string name, AnyObjectId id)
        {
            writeLooseRef(name, id.Name + "\n");
        }

        private void writeLooseRef(string name, string content)
        {
            write(PathUtil.CombineFilePath(diskRepo.Directory, name), content);
        }

        private void writePackedRef(string name, AnyObjectId id)
        {
            writePackedRefs(id.Name + " " + name + "\n");
        }

        private void writePackedRefs(string content)
        {
            FileInfo pr = PathUtil.CombineFilePath(diskRepo.Directory, "packed-refs");
            write(pr, content);
        }

        private void deleteLooseRef(string name)
        {
            FileInfo path = PathUtil.CombineFilePath(diskRepo.Directory, name);
            Assert.IsTrue(path.DeleteFile(), "deleted " + name);
        }

        /*
     * Kick the timestamp of a local file.
     * <p>
     * We shouldn't have to make these method calls. The cache is using file
     * system timestamps, and on many systems unit tests run faster than the
     * modification clock. Dumping the cache after we make an edit behind
     * RefDirectory's back allows the tests to pass.
     *
     * @param name
     *            the file in the repository to force a time change on.
     */
        private void BUG_WorkAroundRacyGitIssues(string name)
        {
            FileInfo path = PathUtil.CombineFilePath(diskRepo.Directory, name);
            long old = path.lastModified();
            long set = 1250379778668L; // Sat Aug 15 20:12:58 GMT-03:30 2009
            path.LastWriteTime = (set.MillisToUtcDateTime());
            Assert.IsTrue(old != path.lastModified(), "time changed");
        }
    }
}