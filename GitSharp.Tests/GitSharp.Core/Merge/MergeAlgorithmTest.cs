/*
 * Copyright (C) 2009, Christian Halstrick <christian.halstrick@sap.com>
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
using GitSharp.Core.Diff;
using GitSharp.Core.Merge;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Merge
{
    public class MergeAlgorithmTest {
        MergeFormatter fmt=new MergeFormatter();

        // the texts which are used in this merge-tests are constructed by
        // concatenating fixed chunks of text defined by the string constants
        // A..Y. The common base text is always the text A+B+C+D+E+F+G+H+I+J.
        // The two texts being merged are constructed by deleting some chunks
        // or inserting new chunks. Some of the chunks are one-liners, others
        // contain more than one line.
        private static string A = "aaa\n";
        private static string B = "bbbbb\nbb\nbbb\n";
        private static string C = "c\n";
        private static string D = "dd\n";
        private static string E = "ee\n";
        private static string F = "fff\nff\n";
        private static string G = "gg\n";
        private static string H = "h\nhhh\nhh\n";
        private static string I = "iiii\n";
        private static string J = "jj\n";
        private static string Z = "zzz\n";
        private static string Y = "y\n";

        // constants which define how conflict-regions are expected to be reported.
        private static string XXX_0 = "<<<<<<< O\n";
        private static string XXX_1 = "=======\n";
        private static string XXX_2 = ">>>>>>> T\n";

        // the common base from which all merges texts derive from
        string @base=A+B+C+D+E+F+G+H+I+J;

        // the following constants define the merged texts. The name of the
        // constants describe how they are created out of the common base. E.g.
        // the constant named replace_XYZ_by_MNO stands for the text which is
        // created from common base by replacing first chunk X by chunk M, then
        // Y by N and then Z by O.
        string replace_C_by_Z=A+B+Z+D+E+F+G+H+I+J;
        string replace_A_by_Y=Y+B+C+D+E+F+G+H+I+J;
        string replace_A_by_Z=Z+B+C+D+E+F+G+H+I+J;
        string replace_J_by_Y=A+B+C+D+E+F+G+H+I+Y;
        string replace_J_by_Z=A+B+C+D+E+F+G+H+I+Z;
        string replace_BC_by_ZZ=A+Z+Z+D+E+F+G+H+I+J;
        string replace_BCD_by_ZZZ=A+Z+Z+Z+E+F+G+H+I+J;
        string replace_BD_by_ZZ=A+Z+C+Z+E+F+G+H+I+J;
        string replace_BCDEGI_by_ZZZZZZ=A+Z+Z+Z+Z+F+Z+H+Z+J;
        string replace_CEFGHJ_by_YYYYYY=A+B+Y+D+Y+Y+Y+Y+I+Y;
        string replace_BDE_by_ZZY=A+Z+C+Z+Y+F+G+H+I+J;

        /**
	 * Check for a conflict where the second text was changed similar to the
	 * first one, but the second texts modification covers one more line.
	 *
	 * @throws IOException
	 */
        [Test]
        public void testTwoConflictingModifications()
        {
            Assert.AreEqual(A + XXX_0 + B + Z + XXX_1 + Z + Z + XXX_2 + D + E + F + G
                            + H + I + J,
                            merge(@base, replace_C_by_Z, replace_BC_by_ZZ));
        }

        /**
	 * Test a case where we have three consecutive chunks. The first text
	 * modifies all three chunks. The second text modifies the first and the
	 * last chunk. This should be reported as one conflicting region.
	 *
	 * @throws IOException
	 */
        [Test]
        public void testOneAgainstTwoConflictingModifications()
        {
            Assert.AreEqual(A + XXX_0 + Z + Z + Z + XXX_1 + Z + C + Z + XXX_2 + E + F
                            + G + H + I + J,
                            merge(@base, replace_BCD_by_ZZZ, replace_BD_by_ZZ));
        }

        /**
	 * Test a merge where only the second text contains modifications. Expect as
	 * merge result the second text.
	 *
	 * @throws IOException
	 */
        [Test]
        public void testNoAgainstOneModification()
        {
            Assert.AreEqual(replace_BD_by_ZZ.ToString(),
                            merge(@base, @base, replace_BD_by_ZZ));
        }

        /**
	 * Both texts contain modifications but not on the same chunks. Expect a
	 * non-conflict merge result.
	 *
	 * @throws IOException
	 */
        [Test]
        public void testTwoNonConflictingModifications()
        {
            Assert.AreEqual(Y + B + Z + D + E + F + G + H + I + J,
                            merge(@base, replace_C_by_Z, replace_A_by_Y));
        }

        /**
	 * Merge two complicated modifications. The merge algorithm has to extend
	 * and combine conflicting regions to get to the expected merge result.
	 *
	 * @throws IOException
	 */
        [Test]
        public void testTwoComplicatedModifications()
        {
            Assert.AreEqual(A + XXX_0 + Z + Z + Z + Z + F + Z + H + XXX_1 + B + Y + D
                            + Y + Y + Y + Y + XXX_2 + Z + Y,
                            merge(@base,
                                  replace_BCDEGI_by_ZZZZZZ,
                                  replace_CEFGHJ_by_YYYYYY));
        }

        /**
	 * Test a conflicting region at the very start of the text.
	 *
	 * @throws IOException
	 */
        [Test]
        public void testConflictAtStart()
        {
            Assert.AreEqual(XXX_0 + Z + XXX_1 + Y + XXX_2 + B + C + D + E + F + G + H
                            + I + J, merge(@base, replace_A_by_Z, replace_A_by_Y));
        }

        /**
	 * Test a conflicting region at the very end of the text.
	 *
	 * @throws IOException
	 */
        [Test]
        public void testConflictAtEnd()  {
            Assert.AreEqual(A + B + C + D + E + F + G + H + I + XXX_0 + Z + XXX_1 + Y + XXX_2, merge(@base, replace_J_by_Z, replace_J_by_Y));
        }

        private string merge(string commonBase, string ours, string theirs)  {
            MergeResult r=MergeAlgorithm.merge(new RawText(Constants.encode(commonBase)), new RawText(Constants.encode(ours)), new RawText(Constants.encode(theirs)));
		
            using (var ms = new MemoryStream())
            using (var bo = new StreamWriter(ms, Charset.forName(Constants.CHARSET.WebName)))
            {
                fmt.formatMerge(bo, r, "B", "O", "T", Constants.CHARSET.WebName);
                return Constants.CHARSET.GetString(ms.ToArray());
            }
        }
    }
}