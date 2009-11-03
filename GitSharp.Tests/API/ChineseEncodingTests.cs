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
    public class ChineseEncodingTests : ApiTestCase
    {
        [Test]
        public void Commit_Chinese()
        {
            var workingDirectory = Path.Combine(trash.FullName, "汉语repo");
            using (var repo = Repository.Init(workingDirectory))
            {
                var index_path = Path.Combine(repo.Directory, "index");
                var index = repo.Index;
                index.Write(); // write empty index
                string filepath = Path.Combine(workingDirectory, "欢迎 henon.txt");
                File.WriteAllText(filepath, "我爱写汉字。");
                string filepath1 = Path.Combine(workingDirectory, "好喝的啤酒.txt");
                File.WriteAllText(filepath1, "青岛啤酒");
                index.Add(filepath, filepath1);

                var status = repo.Status;
                Assert.IsTrue(status.Added.Contains("欢迎 henon.txt"));
                Assert.IsTrue(status.Added.Contains("好喝的啤酒.txt"));

                var c=repo.Commit("中国很大。长城很长。上海很漂亮。", new Author("汉语学生", "meinrad.recheis@gmail.com"));
                var commit = new Commit(repo, c.Hash);
                Assert.AreEqual("中国很大。长城很长。上海很漂亮。", commit.Message);
                Assert.AreEqual("汉语学生", commit.Author.Name);
                var dict=commit.Tree.Leaves.ToDictionary(leaf => leaf.Name);
                Assert.AreEqual("我爱写汉字。", dict["欢迎 henon.txt"].Data);
                Assert.AreEqual("青岛啤酒", dict["好喝的啤酒.txt"].Data);
            }
        }
    }
}

