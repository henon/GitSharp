/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using System.IO;
using GitSharp.Core;
using NUnit.Framework;

namespace GitSharp.Tests
{
	[TestFixture]
    public class T0004_PackReader : RepositoryTestCase
	{
		private const string PackName = "pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f";
		private static readonly FileInfo TestPack = new FileInfo(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "Resources/"), GitSharp.Core.Transport.IndexPack.GetPackFileName(PackName)).Replace('/', Path.DirectorySeparatorChar));
		private static readonly FileInfo TestIdx = new FileInfo(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "Resources/"), GitSharp.Core.Transport.IndexPack.GetIndexFileName(PackName)).Replace('/', Path.DirectorySeparatorChar));

		[Test]
		public void test003_lookupCompressedObject()
		{
			ObjectId id = ObjectId.FromString("902d5476fa249b7abc9d84c611577a81381f0327");
			var pr = new PackFile(TestIdx, TestPack);
			PackedObjectLoader or = pr.Get(new WindowCursor(), id);
			Assert.IsNotNull(or);
			Assert.AreEqual(Constants.OBJ_TREE, or.Type);
			Assert.AreEqual(35, or.Size);
			Assert.AreEqual(7738, or.DataOffset);
			pr.Close();
		}

		[Test]
		public void test004_lookupDeltifiedObject()
		{
			ObjectId id = ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259");
			ObjectLoader or = db.OpenObject(id);
			Assert.IsNotNull(or);
			Assert.IsTrue(or is PackedObjectLoader);
			Assert.AreEqual(Constants.OBJ_BLOB, or.Type);
			Assert.AreEqual(18009, or.Size);
			Assert.AreEqual(537, ((PackedObjectLoader)or).DataOffset);
		}

        [Test]
        public void test005_todopack()
        {
            FileInfo todopack = new FileInfo("Resources/todopack");
            if (!todopack.Exists)
            {
                Assert.Ignore("Skipping " + "test005_todopack()" + ": no " + todopack.FullName);
                return;
            }

            var packDir = new DirectoryInfo(Path.Combine(db.ObjectsDirectory.FullName, "pack"));
            string packname = "pack-2e71952edc41f3ce7921c5e5dd1b64f48204cf35";
            
            new FileInfo(todopack.FullName + "/" + GitSharp.Core.Transport.IndexPack.GetPackFileName(packname)).CopyTo(packDir.FullName + "/" + GitSharp.Core.Transport.IndexPack.GetPackFileName(packname));
            new FileInfo(todopack.FullName + "/" + GitSharp.Core.Transport.IndexPack.GetIndexFileName(packname)).CopyTo(packDir.FullName + "/" + GitSharp.Core.Transport.IndexPack.GetIndexFileName(packname));

            Tree t;

            t = db
                    .MapTree(ObjectId.FromString(
                            "aac9df07f653dd18b935298deb813e02c32d2e6f"));
            Assert.IsNotNull(t);
            int count1 = t.MemberCount;

            t = db
                    .MapTree(ObjectId.FromString(
                            "6b9ffbebe7b83ac6a61c9477ab941d999f5d0c96"));
            Assert.IsNotNull(t);
            int count2 = t.MemberCount;
        }
	}
}