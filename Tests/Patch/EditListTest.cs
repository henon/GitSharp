/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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

using GitSharp.Diff;
using GitSharp.Patch;
using NUnit.Framework;

namespace GitSharp.Tests.Patch
{
    [TestFixture]
    public class EditListTest : BasePatchTest
    {
        [Test]
	    public void testHunkHeader()
        {
            GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testGetText_BothISO88591.patch");
		    FileHeader fh = p.getFiles()[0];

		    EditList list0 = fh.getHunks()[0].ToEditList();
		    Assert.AreEqual(1, list0.size());
		    Assert.AreEqual(new Edit(4 - 1, 5 - 1, 4 - 1, 5 - 1), list0.get(0));

		    EditList list1 = fh.getHunks()[1].ToEditList();
		    Assert.AreEqual(1, list1.size());
		    Assert.AreEqual(new Edit(16 - 1, 17 - 1, 16 - 1, 17 - 1), list1.get(0));
	    }

        [Test]
	    public void testFileHeader()
        {
            GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testGetText_BothISO88591.patch");
		    FileHeader fh = p.getFiles()[0];
		    EditList e = fh.ToEditList();
		    Assert.AreEqual(2, e.size());
		    Assert.AreEqual(new Edit(4 - 1, 5 - 1, 4 - 1, 5 - 1), e.get(0));
		    Assert.AreEqual(new Edit(16 - 1, 17 - 1, 16 - 1, 17 - 1), e.get(1));
	    }

        [Test]
	    public void testTypes()
        {
            GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testEditList_Types.patch");
		    FileHeader fh = p.getFiles()[0];
		    EditList e = fh.ToEditList();
            Assert.AreEqual(3, e.size());
            Assert.AreEqual(new Edit(3 - 1, 3 - 1, 3 - 1, 4 - 1), e.get(0));
            Assert.AreEqual(new Edit(17 - 1, 19 - 1, 18 - 1, 18 - 1), e.get(1));
            Assert.AreEqual(new Edit(23 - 1, 25 - 1, 22 - 1, 28 - 1), e.get(2));
	    }
    }
}