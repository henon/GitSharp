/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
using GitSharp.Tests.GitSharp.Core;
using NUnit.Framework;

namespace GitSharp.Core.Tests
{
    [TestFixture]
    public class PackIndexV2Tests : PackIndexTestCase
    {
    	protected override FileInfo GetFileForPack34Be9032()
        {
            return new FileInfo("Resources/pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f.idxV2");
        }

    	protected override FileInfo GetFileForPackdf2982F28()
        {
            return new FileInfo("Resources/pack-df2982f284bbabb6bdb59ee3fcc6eb0983e20371.idxV2");
        }

        /// <summary>
		/// Verify CRC32 indexing.
        /// </summary>
        [Test]
        public override void testCRC32()
        {
            Assert.IsTrue(smallIdx.HasCRC32Support);
            Assert.AreEqual(0x00000000C2B64258L, smallIdx.FindCRC32(ObjectId.FromString("4b825dc642cb6eb9a060e54bf8d69288fbee4904")));
            Assert.AreEqual(0x0000000072AD57C2L, smallIdx.FindCRC32(ObjectId.FromString("540a36d136cf413e4b064c2b0e0a4db60f77feab")));
            Assert.AreEqual(0x00000000FF10A479L, smallIdx.FindCRC32(ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259")));
            Assert.AreEqual(0x0000000034B27DDCL, smallIdx.FindCRC32(ObjectId.FromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3")));
            Assert.AreEqual(0x000000004743F1E4L, smallIdx.FindCRC32(ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7")));
            Assert.AreEqual(0x00000000640B358BL, smallIdx.FindCRC32(ObjectId.FromString("902d5476fa249b7abc9d84c611577a81381f0327")));
            Assert.AreEqual(0x000000002A17CB5EL, smallIdx.FindCRC32(ObjectId.FromString("aabf2ffaec9b497f0950352b3e582d73035c2035")));
            Assert.AreEqual(0x000000000B3B5BA6L, smallIdx.FindCRC32(ObjectId.FromString("c59759f143fb1fe21c197981df75a7ee00290799")));
        }
    }
}
