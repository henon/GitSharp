/*
 * Copyright (C) 2009, Johannes E. Schindelin
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
using System.Text;
using GitSharp.Core.Diff;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Diff
{
    public class MyersDiffTest  {
        [Test]
        public void testAtEnd()
        {
            assertDiff("HELLO", "HELL", " -4,1 +4,0");
        }

        [Test]
        public void testA()
        {
            assertDiff("A", "a", " -0,1 +0,1");
        }

        [Test]
        public void testB()
        {
            assertDiff("a", "A", " -0,1 +0,1");
        }

        [Test]
        public void testC()
        {
            assertDiff("aA", "A", " -0,1 +0,0");
        }

        [Test]
        public void testD()
        {
            assertDiff("Aa", "A", " -1,1 +1,0");
        }

        [Test]
        public void testE()
        {
            assertDiff("ABCDEFG", "BDF", " -0,1 +0,0 -2,1 +1,0 -4,1 +2,0 -6,1 +3,0");
        }

        [Test]
        public void testAtStart() {
            assertDiff("Git", "JGit", " -0,0 +0,1");
        }

        [Test]
        public void testSimple()
        {
            assertDiff("HELLO WORLD", "LOW",
                       " -0,3 +0,0 -5,1 +2,0 -7,4 +3,0");
            // is ambiguous, could be this, too:
            // " -0,2 +0,0 -3,1 +1,0 -5,1 +2,0 -7,4 +3,0"
        }

        public void assertDiff(string a, string b, string edits) {
            MyersDiff diff = new MyersDiff(toCharArray(a), toCharArray(b));
            Assert.AreEqual(edits, toString(diff.getEdits()));
        }

        private static string toString(EditList list) {
            StringBuilder builder = new StringBuilder();
            foreach (Edit e in list)
                builder.Append(" -" + e.BeginA
                               + "," + (e.EndA - e.BeginA)
                               + " +" + e.BeginB + "," + (e.EndB - e.BeginB));
            return builder.ToString();
        }

        private static CharArray toCharArray(string s) {
            return new CharArray(s);
        }

        protected static string toString(Sequence seq, int begin, int end) {
            CharArray a = (CharArray)seq;
            return new string(a.array, begin, end - begin);
        }

        protected static string toString(CharArray a, CharArray b,
                                         int x, int k) {
            return "(" + x + "," + (k + x)
                   + (x < 0 ? '<' :
                                      (x >= a.array.Length ?
                                                               '>' : a.array[x]))
                   + (k + x < 0 ? '<' :
                                          (k + x >= b.array.Length ?
                                                                       '>' : b.array[k + x]))
                   + ")";
                                         }

        protected class CharArray : Sequence {
            internal char[] array;
            public CharArray(string s) { array = s.ToCharArray(); }
            public int size() { return array.Length; }
            public bool equals(int i, Sequence other, int j) {
                CharArray o = (CharArray)other;
                return array[i] == o.array[j];
            }
        }
    }
}