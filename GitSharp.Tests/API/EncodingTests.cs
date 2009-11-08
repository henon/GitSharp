/*
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

namespace GitSharp.Tests.API
{
    [TestFixture]
    public class EncodingTests : ApiTestCase
    {
        [Test]
        public void Chinese_UTF8()
        {
            var workingDirectory = Path.Combine(trash.FullName, "汉语repo"); // a chinese repository
            using (var repo = Repository.Init(workingDirectory))
            {
                var index = repo.Index;
                string filepath = Path.Combine(workingDirectory, "henon喜欢什么.txt"); // what henon likes.txt
                File.WriteAllText(filepath, "我爱写汉字。"); // i love writing chinese characters.
                string filepath1 = Path.Combine(workingDirectory, "nulltoken喜欢什么.txt"); // what nulltoken likes.txt
                File.WriteAllText(filepath1, "他爱法国红酒。"); // he loves french red wine.
                var dir = Directory.CreateDirectory(Path.Combine(workingDirectory, "啤酒")).FullName; // creating a folder called "beer" 
                string filepath2 = Path.Combine(dir, "好喝的啤酒.txt"); // creating a file called "good beer.txt" in folder "beer"
                File.WriteAllText(filepath2, "青岛啤酒"); // the file contains a chinese beer called QingDao beer 

                // adding the files and directories we created.
                index.Add(filepath, filepath1, dir);

                // checking index
                var status = repo.Status;
                Assert.IsTrue(status.Added.Contains("henon喜欢什么.txt"));
                Assert.IsTrue(status.Added.Contains("nulltoken喜欢什么.txt"));
                Assert.IsTrue(status.Added.Contains("啤酒/好喝的啤酒.txt"));

                // committing, with the message; "China is very large. The great wall is very long. Shanghai is very pretty.", Author: "a student of the chinese language"
                var c = repo.Commit("中国很大。长城很长。上海很漂亮。", new Author("汉语学生", "meinrad.recheis@gmail.com"));

                // loading the commit from the repository and inspecting its contents
                var commit = new Commit(repo, c.Hash);
                Assert.AreEqual("中国很大。长城很长。上海很漂亮。", commit.Message);
                Assert.AreEqual("汉语学生", commit.Author.Name);
                var dict = commit.Tree.Leaves.ToDictionary(leaf => leaf.Name);
                Assert.AreEqual("我爱写汉字。", dict["henon喜欢什么.txt"].Data);
                Assert.AreEqual("他爱法国红酒。", dict["nulltoken喜欢什么.txt"].Data);
                Tree tree = commit.Tree.Trees.First();
                Assert.AreEqual("啤酒", tree.Name);
                Leaf good_beer = tree.Leaves.First();
                Assert.AreEqual("好喝的啤酒.txt", good_beer.Name);
                Assert.AreEqual(Encoding.UTF8.GetBytes("青岛啤酒"), good_beer.RawData);
            }
        }


        [Test]
        public void French_UTF8()
        {
            var workingDirectory = Path.Combine(trash.FullName, "repo français"); // a french repository
            using (var repo = Repository.Init(workingDirectory))
            {
                var index = repo.Index;
                string filepath = Path.Combine(workingDirectory, "Émeric.txt"); // Emeric.txt
                File.WriteAllText(filepath, "était ici..."); // was here.
                var dir = Directory.CreateDirectory(Path.Combine(workingDirectory, "À moins")).FullName; // unless... 
                string filepath2 = Path.Combine(dir, "qu'il ne fût là.txt"); // he's been there.txt
                File.WriteAllText(filepath2, "éèçàù"); // the file contains a some random letters with accents 

                // adding the files and directories we created.
                index.Add(filepath, dir);

                // checking index
                var status = repo.Status;
                Assert.IsTrue(status.Added.Contains("Émeric.txt"));
                Assert.IsTrue(status.Added.Contains("À moins/qu'il ne fût là.txt"));

                // committing, with the message; "A little french touch.", Author: "Emeric"
                var c = repo.Commit("Une petite note française.", new Author("Émeric", "emeric.fermas@gmail.com"));

                // loading the commit from the repository and inspecting its contents
                var commit = new Commit(repo, c.Hash);
                Assert.AreEqual("Une petite note française.", commit.Message);
                Assert.AreEqual("Émeric", commit.Author.Name);
                var dict = commit.Tree.Leaves.ToDictionary(leaf => leaf.Name);
                Assert.AreEqual("était ici...", dict["Émeric.txt"].Data);
                Tree tree = commit.Tree.Trees.First();
                Assert.AreEqual("À moins", tree.Name);
                Leaf file3 = tree.Leaves.First();
                Assert.AreEqual("qu'il ne fût là.txt", file3.Name);
                Assert.AreEqual(Encoding.UTF8.GetBytes("éèçàù"), file3.RawData);
            }
        }

        [Test]
        public void Japanese_ShiftJIS()
        {
            var workingDirectory = Path.Combine(trash.FullName, "Shift_JIS_Repo");
            using (Repository repo = Repository.Init(workingDirectory))
            {
                //repo.PreferredEncoding = Encoding.GetEncoding("Shift_JIS");
                Encoding shiftJIS = Encoding.GetEncoding("Shift_JIS");

                var e = Encoding.Default;

                //var rabbit = UTF8_to_ShiftJIS_filename("ウサギちゃん/Rabbitはウサギです.txt");
                //var filepath = Path.Combine(workingDirectory, rabbit);
                //Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                //File.Copy(Path.Combine(@"Resources\encodingTestData\Shift_JIS", rabbit), filepath);

                // Adding an encoded file to the index without relying on the filesystem
                repo.Index.Add(UTF8_to_ShiftJIS("ウサギちゃん/Rabbitはウサギです.txt"), new byte[0]);

                var shinjuku_sanchome = UTF8_to_ShiftJIS_filename("東京都/新宿三丁目.txt");
                var filepath1 = Path.Combine(workingDirectory, shinjuku_sanchome);
                Directory.CreateDirectory(Path.GetDirectoryName(filepath1));
                File.WriteAllText(filepath1, "ラビットis usagi desu.", shiftJIS);

                // Adding an encoded file to the index from the filesystem
                repo.Index.Add(filepath1);

                var msg = "Hello World!日本からShift_JISのためをコミットしました";
                var name = "ポウルス";
                var commit = repo.Commit(msg, new Author(name, "paupaw@tokyo-dome.com"));

                // TODO: set the breakpoint here and check out the test repository. Is it readable on your system with msysgit?
            }
        }

        [Test]
        public void CanReadFromMsysGitJapaneseRepository()
        {
            //setup of .git directory
            var resource =
                new DirectoryInfo(Path.Combine(Path.Combine(Environment.CurrentDirectory, "Resources"),
                                               "JapaneseRepo"));
            var tempRepository =
                new DirectoryInfo(Path.Combine(trash.FullName, "JapaneseRepo" + Path.GetRandomFileName()));
            CopyDirectory(resource.FullName, tempRepository.FullName);

            var repositoryPath = new DirectoryInfo(Path.Combine(tempRepository.FullName, ".git"));
            Directory.Move(repositoryPath.FullName + "ted", repositoryPath.FullName);
            using (Repository repo = new Repository(tempRepository.FullName))
            {
                string commitHash = "24ed0e20ceff5e2cdf768345b6853213f840ff8f";

                var commit = new Commit(repo, commitHash);
                Assert.AreEqual("コミットのメッセージも日本語でやてみました。\n", commit.Message);
            }
        }

        [Test]
        public void CommitsHonorsConfigCommitEncoding()
        {
            string workingDirectory = Path.Combine(trash.FullName, Path.GetRandomFileName());
            
            // Creating a new repo
            using (var repo = Repository.Init(workingDirectory))
            {
                Core.Repository coreRepo = repo;
                
                // Changing the commitencoding configuration entry
                coreRepo.Config.setString("i18n", null, "commitencoding", "ISO-8859-1");
                coreRepo.Config.save();
            }

            // Loading the repo (along with config change)
            using (var repo = new Repository(workingDirectory))
            {
                // Adding a new file to the filesystem
                string filepath = Path.Combine(workingDirectory, "dummy here.txt");
                File.WriteAllText(filepath, "dummy there too...");

                // Adding the new file to index
                repo.Index.Add(filepath);

                // Committing
                Commit c = repo.Commit("Commit with ISO-8859-1 encoding.", new Author("nulltoken", "emeric.fermas@gmail.com"));

                // Loading the commit
                var commit = new Commit(repo, c.Hash);
                Assert.AreEqual("ISO-8859-1", commit.Encoding.WebName.ToUpperInvariant());
            }
        }

/* ... [henon] commented out because the shiftJIS encoded resource filenames are not portable accross cultures 
        [Test]
        public void Commit_into_empty_repository_forShiftJIS()
        {
            var workingDirectory = Path.Combine(trash.FullName, "Shift_JISEncodingTest");
            using (Repository repo = Repository.Init(workingDirectory))
            {
                Encoding shiftJISEncoding = Encoding.GetEncoding("Shift_JIS");

                string filepath = Path.Combine(workingDirectory, @"Resources\encodingTestData\Shift_JIS\ウサギちゃん\Rabbitはウサギです.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filepath));
                System.IO.File.Copy(@"Resources\encodingTestData\Shift_JIS\ウサギちゃん\Rabbitはウサギです.txt", filepath);
                repo.Index.Add(filepath); //Add using UTF-8 params, but the file itself is Shift_JIS.. heh!?

                string filepath1 = Path.Combine(workingDirectory, @"Resources\encodingTestData\Shift_JIS\東京都\新宿三丁目.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filepath1));
                System.IO.File.Copy(@"Resources\encodingTestData\Shift_JIS\東京都\新宿三丁目.txt", filepath1);
                repo.Index.Add(filepath1); //Add using UTF-8 params, but the file itself is Shift_JIS.. heh!?

                var commit = repo.Commit("Hello World!日本からShift_JISのためをコミットしました", new Author("ポウルス", "paupaw@tokyo-dome.com"));
                Assert.NotNull(commit);
                Assert.IsTrue(commit.IsCommit);
                Assert.IsNull(commit.Parent);
                Assert.AreEqual("ポウルス", commit.Author.Name);
                Assert.AreEqual("paupaw@tokyo-dome.com", commit.Author.EmailAddress);
                Assert.AreEqual("Hello World!日本からShift_JISのためをコミットしました", commit.Message);
                // TODO: check if tree contains for henon and for nulltoken, get the blobs and check  the content.
                Assert.AreEqual(commit, repo.Head.CurrentCommit);
                var changes = commit.Changes.ToDictionary(change => change.Name);
                Assert.AreEqual(ChangeType.Added, changes["Rabbitはウサギです.txt"].ChangeType);
                Assert.AreEqual(ChangeType.Added, changes["新宿三丁目.txt"].ChangeType);
                Assert.AreEqual(Encoding.UTF8.GetBytes("ラビットis usagi desu."), Encoding.Convert(shiftJISEncoding, Encoding.UTF8, (changes["Rabbitはウサギです.txt"].ComparedObject as Blob).RawData)); //Convert from Shift_JIS to UTF-8
                Assert.AreEqual(Encoding.UTF8.GetBytes("電車で行きます。"), Encoding.Convert(shiftJISEncoding, Encoding.UTF8, (changes["新宿三丁目.txt"].ComparedObject as Blob).RawData)); //Convert from Shift_JIS to UTF-8
                Assert.AreEqual(2, changes.Count);
            }
        }

 */
        //[Test]
        //public void Commit_into_empty_repository_forShiftJis1()
        //{
        //    var workingDirectory = Path.Combine(trash.FullName, "test1");
        //    using (Repository repo = Repository.Init(workingDirectory))
        //    {
        //        //GitSharp.Core.Constants.setCHARSET("Shift_JIS");
        //        string filepath = Path.Combine(workingDirectory, "for henon.txt");
        //        File.WriteAllText(filepath, "Weißbier");
        //        repo.Index.Add(filepath);
        //        string filepath1 = Path.Combine(workingDirectory, "for nulltoken.txt");
        //        File.WriteAllText(filepath1, "Rotwein");
        //        repo.Index.Add(filepath1);
        //        string filepath2 = Path.Combine(workingDirectory, "俺のためだ.txt");
        //        File.WriteAllText(filepath2, "西東京市");
        //        repo.Index.Add(filepath2);
        //        var commit = repo.Commit("Hello World!日本からShift_JISのためをコミットしました", new Author("ポウルス", "paupaw@tokyo-dome.com"));
        //        Assert.NotNull(commit);
        //        Assert.IsTrue(commit.IsCommit);
        //        Assert.IsNull(commit.Parent);
        //        Assert.AreEqual("ポウルス", commit.Author.Name);
        //        Assert.AreEqual("paupaw@tokyo-dome.com", commit.Author.EmailAddress);
        //        Assert.AreEqual("Hello World!日本からShift_JISのためをコミットしました", commit.Message);
        //        // TODO: check if tree contains for henon and for nulltoken, get the blobs and check  the content.
        //        Assert.AreEqual(commit, repo.Head.CurrentCommit);
        //        var changes = commit.Changes.ToDictionary(change => change.Name);
        //        Assert.AreEqual(ChangeType.Added, changes["for henon.txt"].ChangeType);
        //        Assert.AreEqual(ChangeType.Added, changes["for nulltoken.txt"].ChangeType);
        //        Assert.AreEqual(ChangeType.Added, changes["俺のためだ.txt"].ChangeType);
        //        Assert.AreEqual("Weißbier", (changes["for henon.txt"].ComparedObject as Blob).Data);
        //        Assert.AreEqual("Rotwein", (changes["for nulltoken.txt"].ComparedObject as Blob).Data);
        //        Assert.AreEqual("西東京市", (changes["俺のためだ.txt"].ComparedObject as Blob).Data);
        //        Assert.AreEqual(3, changes.Count);
        //    }
        //}

        private static string UTF8_to_ShiftJIS_filename(string utf8_japanese)
        {
            Encoding shiftJISEncoding = Encoding.GetEncoding("Shift_JIS");
            return Encoding.Default.GetString(Encoding.Convert(Encoding.UTF8, shiftJISEncoding, Encoding.UTF8.GetBytes(utf8_japanese)));
        }

        private static byte[] UTF8_to_ShiftJIS(string utf8_japanese)
        {
            return Encoding.Convert(Encoding.UTF8,  Encoding.GetEncoding("Shift_JIS"), Encoding.UTF8.GetBytes(utf8_japanese));
        }
    }
}

