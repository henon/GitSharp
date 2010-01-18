/*
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
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
using GitSharp.Core.Tests;
using GitSharp.Commands;
using GitSharp.Tests.GitSharp;
using NUnit.Framework;

namespace GitSharp.API.Tests
{
    [TestFixture]
    class StatusTests : ApiTestCase
    {
        [Test]
        public void IsFileUntracked()
        {
            bool bare = false;
            var path = Path.Combine(trash.FullName, "test");
            using (var repo = Repository.Init(path, bare))
            {
                StatusCommand cmd = new StatusCommand();
                Assert.IsNotNull(cmd);
                cmd.Repository = repo;
                Assert.IsNotNull(cmd.Repository);
                //Verify the file is not already created
                string filename = "newfile.txt";
                StatusResults results = Git.Status(cmd);
                Assert.IsNotNull(results);
                Assert.IsFalse(results.Contains(filename, StatusState.Untracked));
                //Create the file and re-populate the status results
                string filepath = Path.Combine(repo.WorkingDirectory, filename);
                File.WriteAllText(filepath, "Just a simple test.");
                StatusResults results1 = Git.Status(cmd);
                Assert.IsNotNull(results1);
                Assert.IsTrue(results1.Contains(filename, StatusState.Untracked));
            }
        }

        [Test]
        public void IsFileModified()
        {
            //Requires the add command to implement.
            Assert.Ignore("This test has not been implemented yet.");
        }

        [Test]
        public void IsFileStaged()
        {
            //Requires the add command to implement.
            Assert.Ignore("This test has not been implemented yet.");
        }

        //Tests if a file can be staged and modified at the same time.
        //In git, this behavior is allowed. This behavior is still pending in GitSharp.
        public void IsFileStagedAndModified()
        {
            //Requires the add command to implement.
            Assert.Ignore("This test has not been implemented yet.");
        }
    }
}
