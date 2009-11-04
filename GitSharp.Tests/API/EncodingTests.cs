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
    }
}

