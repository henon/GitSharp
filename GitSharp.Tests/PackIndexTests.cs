/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

using GitSharp.Core;
using NUnit.Framework;
using System.IO;

namespace GitSharp.Tests
{
	[TestFixture]
	public class PackIndexTests
	{
		[Test]
		public void ObjectList()
		{
			var knownOffsets = new long[] { 370, 349, 304, 12, 175, 414 };
			var knownCrcs = new long[] { 1376555649, 3015185563, 2667925865, 914969567, 2706901546, 39477847 };
			var knownObjectIds = new[] { "1AFC38724D2B89264C7B3826D40B0655A95CFAB4", "557DB03DE997C86A4A028E1EBD3A1CEB225BE238", "67DC4302383B2715F4E0B8C41840EB05B1873697", "A48B402F61EB8ED445DACAA3AF80A2E9796DCB3B", "E41517D564000311F3D7A54F1390EE82F5F1A55B", "E965047AD7C57865823C7D992B1D046EA66EDF78" };

			var indexFile = new FileInfo("Resources\\sample.git" + Path.DirectorySeparatorChar + "objects"
										 + Path.DirectorySeparatorChar + "pack"
										 + Path.DirectorySeparatorChar + "pack-845b2ba3349cc201321e752b01c5ada8102a9a08.idx");

			var index = PackIndex.Open(indexFile);
	
			Assert.AreEqual(6, index.ObjectCount);
			Assert.IsTrue(index.HasCRC32Support);
			
			var i = 0;
			foreach (var item in index)
			{
				Assert.AreEqual(knownObjectIds[i], item.ToString().ToUpper(), "ObjectListId#" + i);
				Assert.AreEqual(knownOffsets[i], item.Offset, "ObjectListOffset#" + i);
				Assert.AreEqual(knownCrcs[i], index.FindCRC32(item.idBuffer), "ObjectListCRC#" + i);
				i++;
			}
		}
	}
}
