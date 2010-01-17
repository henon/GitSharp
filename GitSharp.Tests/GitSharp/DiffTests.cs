/*
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
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp
{
    [TestFixture]
    public class DiffTests : ApiTestCase
    {
        const string TEXT = @"Player Queen:
Both here and hence pursue me lasting strife,
If once I be a widow, ever I be a wife!

Player King:
'Tis deeply sworn. Sweet, leave me here a while,
My spirits grow dull, and fain I would beguile
The tedious day with sleep.

Player Queen:
Sleep rock thy brain,
And never come mischance between us twain!

Hamlet:
Madam, how like you this play?

Queen:
The lady doth protest too much, methinks.";

        private static readonly byte[] RAW_TEXT = Encoding.UTF8.GetBytes(TEXT);

        [Test]
        public void EmptyDiffTest()
        {
            var diff = new Diff("", "");
            Assert.IsFalse(diff.HasDifferences);
            Assert.AreEqual(0, diff.Sections.Count());
        }

        [Test]
        public void UnchangedTest()
        {
            var diff = new Diff(TEXT, TEXT);
            Assert.IsFalse(diff.HasDifferences);
            Assert.AreEqual(1, diff.Sections.Count());
            var section = diff.Sections.First();
            Assert.AreEqual(Diff.SectionStatus.Unchanged, section.Status);
            Assert.AreEqual(0, section.BeginA);
            Assert.AreEqual(0, section.BeginB);
            Assert.AreEqual(RAW_TEXT.Length, section.EndA);
            Assert.AreEqual(RAW_TEXT.Length, section.EndB);
        }

        [Test]
        public void DifferenceTest()
        {
            var diff = new Diff(TEXT, TEXT+"X");
            foreach (var s in diff.Sections)
            {
                Console.WriteLine("==== A ====");
                Console.WriteLine(s.TextA);
                Console.WriteLine("==== B ====");
                Console.WriteLine(s.TextB);
                Console.WriteLine("-------------------\n");
            }
            Assert.IsTrue(diff.HasDifferences);
            Assert.AreEqual(2, diff.Sections.Count());
            var section = diff.Sections.First();
            Assert.AreEqual(Diff.SectionStatus.Unchanged, section.Status);
            Assert.AreEqual(0, section.BeginA);
            Assert.AreEqual(0, section.BeginB);
            //Assert.AreEqual(RAW_TEXT.Length, section.EndA);
            //Assert.AreEqual(RAW_TEXT.Length, section.EndB);
        }
    }
}
