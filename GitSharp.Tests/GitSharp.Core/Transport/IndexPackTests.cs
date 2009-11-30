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

using System;
using System.IO;
using GitSharp.Core;
using GitSharp.Core.Transport;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Transport
{
    [TestFixture]
    public class IndexPackTest : SampleDataRepositoryTestCase
    {
		/// <summary>
		/// Test indexing one of the test packs in the egit repo. It has deltas.
		/// </summary>
        [Test]
		public void test1()
        {
			FileInfo packFile = GetPack("pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f.pack");

            using (Stream @is = packFile.Open(System.IO.FileMode.Open, FileAccess.Read))
            {
                var tmppack = new FileInfo(Path.Combine(trash.FullName, "tmp_pack1"));
                var pack = new IndexPack(db, @is, tmppack);
                pack.index(new TextProgressMonitor());

                var idxFile = new FileInfo(Path.Combine(trash.FullName, "tmp_pack1.idx"));
                var tmpPackFile = new FileInfo(Path.Combine(trash.FullName, "tmp_pack1.pack"));
                //return;
                using (var file = new PackFile(idxFile, tmpPackFile))
                {
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("4b825dc642cb6eb9a060e54bf8d69288fbee4904")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("540a36d136cf413e4b064c2b0e0a4db60f77feab")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("902d5476fa249b7abc9d84c611577a81381f0327")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("aabf2ffaec9b497f0950352b3e582d73035c2035")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("c59759f143fb1fe21c197981df75a7ee00290799")));
                }
            }
        }

		/// <summary>
		/// This is just another pack. It so happens that we have two convenient pack to
		/// test with in the repository.
		/// </summary>
        [Test]
		public void test2()
        {
            FileInfo packFile = GetPack("pack-df2982f284bbabb6bdb59ee3fcc6eb0983e20371.pack");

			using (Stream @is = packFile.Open(System.IO.FileMode.Open, FileAccess.Read))
			{

				var tmppack = new FileInfo(Path.Combine(trash.FullName, "tmp_pack2"));
				var pack = new IndexPack(db, @is, tmppack);
				pack.index(new TextProgressMonitor());

				var idxFile = new FileInfo(Path.Combine(trash.FullName, "tmp_pack2.idx"));
				var tmpPackFile = new FileInfo(Path.Combine(trash.FullName, "tmp_pack2.pack"));
                using (var file = new PackFile(idxFile, tmpPackFile))
                {
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("02ba32d3649e510002c21651936b7077aa75ffa9")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("0966a434eb1a025db6b71485ab63a3bfbea520b6")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("09efc7e59a839528ac7bda9fa020dc9101278680")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("0a3d7772488b6b106fb62813c4d6d627918d9181")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("1004d0d7ac26fbf63050a234c9b88a46075719d3")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("10da5895682013006950e7da534b705252b03be6")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("1203b03dc816ccbb67773f28b3c19318654b0bc8")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("15fae9e651043de0fd1deef588aa3fbf5a7a41c6")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("16f9ec009e5568c435f473ba3a1df732d49ce8c3")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("1fd7d579fb6ae3fe942dc09c2c783443d04cf21e")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("20a8ade77639491ea0bd667bf95de8abf3a434c8")));
                    Assert.IsTrue(file.HasObject(ObjectId.FromString("2675188fd86978d5bc4d7211698b2118ae3bf658")));
                    // and lots more...
                }
			}
        }

		private FileInfo GetPack(string name)
		{
			return new FileInfo(db.ObjectsDirectory + "/pack/" + name);
		}
    }
}