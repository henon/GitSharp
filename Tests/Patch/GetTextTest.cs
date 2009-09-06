/*
 * Copyright (C) 2008, Google Inc.
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

using GitSharp.Patch;
using NUnit.Framework;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace GitSharp.Tests.Patch
{
    [TestFixture]
    public class GetTextTest : BasePatchTest
    {
        [Test]
        public void testGetText_BothISO88591()
        {
            Encoding cs = Encoding.GetEncoding("ISO-8859-1");
            GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testGetText_BothISO88591.patch");
            Assert.IsTrue(p.getErrors().Count == 0);
            Assert.AreEqual(1, p.getFiles().Count);
            FileHeader fh = p.getFiles()[0];
            Assert.AreEqual(2, fh.getHunks().Count);
            Assert.AreEqual(ReadTestPatchFile(cs), fh.getScriptText(cs, cs));
        }

        [Test]
        public void testGetText_NoBinary()
        {
            Encoding cs = Encoding.GetEncoding("ISO-8859-1");
            GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testGetText_NoBinary.patch");
            Assert.IsTrue(p.getErrors().Count == 0);
            Assert.AreEqual(1, p.getFiles().Count);
            FileHeader fh = p.getFiles()[0];
            Assert.AreEqual(0, fh.getHunks().Count);
            Assert.AreEqual(ReadTestPatchFile(cs), fh.getScriptText(cs, cs));
        }

        [Test]
        public void testGetText_Convert()
        {
            Encoding csOld = Encoding.GetEncoding("ISO-8859-1");
            Encoding csNew = Encoding.GetEncoding("UTF-8");
            GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testGetText_Convert.patch");
            Assert.IsTrue(p.getErrors().Count == 0);
            Assert.AreEqual(1, p.getFiles().Count);
            FileHeader fh = p.getFiles()[0];
            Assert.AreEqual(2, fh.getHunks().Count);

            // Read the original file as ISO-8859-1 and fix up the one place
            // where we changed the character encoding. That makes the exp
            // string match what we really expect to get back.
            //
            string exp = ReadTestPatchFile(csOld);
            exp = exp.Replace("\\303\\205ngstr\\303\\266m", "\u00c5ngstr\u00f6m");

            Assert.AreEqual(exp, fh.getScriptText(csOld, csNew));
        }

        [Test]
        public void testGetText_DiffCc()
        {
            Encoding csOld = Encoding.GetEncoding("ISO-8859-1");
            Encoding csNew = Encoding.GetEncoding("UTF-8");
            GitSharp.Patch.Patch p = parseTestPatchFile(PATCHS_DIR + "testGetText_DiffCc.patch");
            Assert.IsTrue(p.getErrors().Count == 0);
            Assert.AreEqual(1, p.getFiles().Count);
            var fh = (CombinedFileHeader)p.getFiles()[0];
            Assert.AreEqual(1, fh.getHunks().Count);

            // Read the original file as ISO-8859-1 and fix up the one place
            // where we changed the character encoding. That makes the exp
            // string match what we really expect to get back.
            //
            string exp = ReadTestPatchFile(csOld);
            exp = exp.Replace("\\303\\205ngstr\\303\\266m", "\u00c5ngstr\u00f6m");

            Assert.AreEqual(exp, fh.getScriptText(new[] { csNew, csOld, csNew }));
        }

        private static string ReadTestPatchFile(Encoding cs)
        {
            string patchFile = (new StackFrame(1, true)).GetMethod().Name + ".patch";
            Stream inStream = new FileStream(PATCHS_DIR + patchFile, System.IO.FileMode.Open);

            try
            {
                var r = new StreamReader(inStream, cs);
                var tmp = new char[2048];
                var s = new StringBuilder();
                int n;
                while ((n = r.Read(tmp, 0, 2048)) > 0)
                {
                    s.Append(tmp, 0, n);
                }
                return s.ToString();
            }
            finally
            {
                inStream.Close();
            }
        }
    }
}