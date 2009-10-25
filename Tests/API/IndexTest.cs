/*
 * Copyright (C) 2009, nulltoken <emeric.fermas@gmail.com>
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
using GitSharp.Tests;
using System.IO;

namespace Git.Tests
{
    [TestFixture]
    public class IndexTest : ApiTestCase
    {
        [Test]
        public void IndexAdd()
        {
            var workingDirectory = Path.Combine(trash.FullName, "test");
            var repo = Repository.Init(workingDirectory);
            var index_path = Path.Combine(repo.Directory, "index");
            var old_index = Path.Combine(repo.Directory, "old_index");
            var index = repo.Index;
            index.Write(); // write empty index
            new FileInfo(index_path).CopyTo(old_index);
            string filepath = Path.Combine(workingDirectory, "for henon.txt");
            File.WriteAllText(filepath, "Weißbier");
            repo.Index.Add(filepath);
            // now verify
            Assert.IsTrue(new FileInfo(index_path).Exists);
            var new_index = new Repository(repo.Directory).Index;
            Assert.AreNotEqual(File.ReadAllBytes(old_index), File.ReadAllBytes(index_path));

            // make another addition
            var index_1 = Path.Combine(repo.Directory, "index_1");
            new FileInfo(index_path).CopyTo(index_1);
            string filepath1 = Path.Combine(workingDirectory, "for nulltoken.txt");
            File.WriteAllText(filepath1, "Rotwein");
            index = new Index(repo);
            index.Add(filepath1);
            Assert.AreNotEqual(File.ReadAllBytes(index_1), File.ReadAllBytes(index_path));
            Assert.DoesNotThrow(() => repo.Index.Read());
            // todo: get changes and verify that for henon.txt has been added
        }

        [Test]
        public void Read_write_empty_index()
        {
            var repo = GetTrashRepository();
            var index_path = Path.Combine(repo.Directory, "index");
            var old_index = Path.Combine(repo.Directory, "old_index");
            var index = repo.Index;
            index.Write(); // write empty index
            Assert.IsTrue(new FileInfo(index_path).Exists);
            new FileInfo(index_path).MoveTo(old_index);
            Assert.IsFalse(new FileInfo(index_path).Exists);
            var new_index = new Repository(repo.Directory).Index;
            new_index.Write(); // see if the read index is rewritten identitcally
            Assert.IsTrue(new FileInfo(index_path).Exists);
            Assert.AreEqual(File.ReadAllBytes(old_index), File.ReadAllBytes(index_path));
        }
    }
}
