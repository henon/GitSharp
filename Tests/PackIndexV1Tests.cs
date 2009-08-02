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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GitSharp;
using GitSharp.Exceptions;
using NUnit.Framework;
using GitSharp.Tests.Util;

namespace GitSharp.Tests
{
    [TestFixture]
    public class PackIndexV1Test : PackIndexTestCase
    {

        public override FileInfo getFileForPack34be9032()
        {
            return new FileInfo("Resources/pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f.idx");
        }


        public override FileInfo getFileForPackdf2982f28()
        {
            return new FileInfo("Resources/pack-df2982f284bbabb6bdb59ee3fcc6eb0983e20371.idx");
        }

        /**
         * Verify CRC32 - V1 should not index anything.
         *
         */
        [Test]
        public override void testCRC32()
        {
            Assert.IsFalse(smallIdx.HasCRC32Support);
            try
            {
                smallIdx.FindCRC32(ObjectId.FromString("4b825dc642cb6eb9a060e54bf8d69288fbee4904"));
                Assert.Fail("index V1 shouldn't support CRC");
            }
            catch (NotSupportedException)
            {
                // expected
            }
        }
    }

}
