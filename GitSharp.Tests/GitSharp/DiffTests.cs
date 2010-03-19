/*
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using System.IO;
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

		private static readonly Text Text = new Text(TEXT);

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
			Assert.AreEqual(1, section.BeginA);
			Assert.AreEqual(1, section.BeginB);
			Assert.AreEqual(Text.NumberOfLines + 1, section.EndA);
			Assert.AreEqual(Text.NumberOfLines + 1, section.EndB);
		}

		[Test]
		public void NothingAgainstEverythingTest()
		{
			var diff = new Diff("", TEXT);
			Assert.IsTrue(diff.HasDifferences);
			Assert.AreEqual(1, diff.Sections.Count());
			var section = diff.Sections.First();
			Assert.AreEqual(Diff.SectionStatus.Different, section.Status);
			Assert.AreEqual(1, section.BeginA);
			Assert.AreEqual(1, section.BeginB);
			Assert.AreEqual(1, section.EndA);
			Assert.AreEqual(Text.NumberOfLines + 1, section.EndB);
			Assert.AreEqual("", section.TextA);
			Assert.AreEqual(TEXT, section.TextB);
		}

		[Test]
		public void DifferenceAtTheBeginning()
		{
			var diff = new Diff(TEXT, "Quote from Hamlet:\n\n" + TEXT);
			//DumpSections(diff);
			Assert.IsTrue(diff.HasDifferences);
			Assert.AreEqual(2, diff.Sections.Count());
			var section = diff.Sections.First();
			Assert.AreEqual(Diff.SectionStatus.Different, section.Status);
			Assert.AreEqual(1, section.BeginA);
			Assert.AreEqual(1, section.BeginB);
			Assert.AreEqual(1, section.EndA);
			Assert.AreEqual(3, section.EndB);
			section = diff.Sections.Skip(1).First();
			Assert.AreEqual(Diff.SectionStatus.Unchanged, section.Status);
			Assert.AreEqual(1, section.BeginA);
			Assert.AreEqual(3, section.BeginB);
			Assert.AreEqual(Text.NumberOfLines + 1, section.EndA);
			Assert.AreEqual(Text.NumberOfLines + 1 + 2, section.EndB);
		}

		[Test]
		public void DifferenceAtTheEnd()
		{
			var diff = new Diff(TEXT, TEXT + "X");
			//DumpSections(diff);
			Assert.IsTrue(diff.HasDifferences);
			Assert.AreEqual(2, diff.Sections.Count());
			var section = diff.Sections.First();
			Assert.AreEqual(Diff.SectionStatus.Unchanged, section.Status);
			Assert.AreEqual(1, section.BeginA);
			Assert.AreEqual(1, section.BeginB);
			Assert.AreEqual(Text.NumberOfLines, section.EndA);
			Assert.AreEqual(Text.NumberOfLines, section.EndB);
			section = diff.Sections.Skip(1).First();
			Assert.AreEqual(Diff.SectionStatus.Different, section.Status);
			Assert.AreEqual(Text.NumberOfLines, section.BeginA);
			Assert.AreEqual(Text.NumberOfLines, section.BeginB);
			Assert.AreEqual(Text.NumberOfLines + 1, section.EndA);
			Assert.AreEqual(Text.NumberOfLines + 1, section.EndB);
		}

		[Test]
		public void MultipleReplacements()
		{
			var blahblah = @"Player Queen:
blah blah

Player King:
blah blah
   
Player Queen:
blah blah

Hamlet:
blah blah

Queen:
blah blah.";
			var diff = new Diff(TEXT, blahblah);
			//DumpSections(diff);
			Assert.IsTrue(diff.HasDifferences);
			Assert.AreEqual(10, diff.Sections.Count());
			var secs = diff.Sections.ToArray();
			var section = secs[0];
			Assert.AreEqual(Diff.SectionStatus.Unchanged, section.Status);
			Assert.AreEqual("Player Queen:\r\n", section.TextA);
			Assert.AreEqual("Player Queen:\r\n", section.TextB);
			section = secs[1];
			Assert.AreEqual(Diff.SectionStatus.Different, section.Status);
			Assert.AreEqual("Both here and hence pursue me lasting strife,\r\nIf once I be a widow, ever I be a wife!\r\n", section.TextA);
			Assert.AreEqual("blah blah\r\n", section.TextB);
			section = secs[9];
			Assert.AreEqual(Diff.SectionStatus.Different, section.Status);
			Assert.AreEqual("The lady doth protest too much, methinks.", section.TextA);
			Assert.AreEqual("blah blah.", section.TextB);
		}

		[Test]
		public void EditType()
		{
			var a = "1\n2\n3\n5\n7\n8\n9\n";
			var b = "1\n3\n4\n5\n6\n8\n9\n";
			var diff = new Diff(a, b);
			DumpSections(diff);
			var secs = diff.Sections.ToArray();
			var section = secs[0];
			Assert.AreEqual(Diff.EditType.Unchanged, section.EditWithRespectToA);
			Assert.AreEqual(Diff.EditType.Unchanged, section.EditWithRespectToB);
			section = secs[1];
			Assert.AreEqual(Diff.EditType.Deleted, section.EditWithRespectToA);
			Assert.AreEqual(Diff.EditType.Inserted, section.EditWithRespectToB);
			section = secs[3];
			Assert.AreEqual(Diff.EditType.Inserted, section.EditWithRespectToA);
			Assert.AreEqual(Diff.EditType.Deleted, section.EditWithRespectToB);
			section = secs[5];
			Assert.AreEqual(Diff.EditType.Replaced, section.EditWithRespectToA);
			Assert.AreEqual(Diff.EditType.Replaced, section.EditWithRespectToB);
		}

		private static void DumpSections(Diff diff)
		{
			foreach (var s in diff.Sections)
			{
				Console.WriteLine(s.Status);
				Console.WriteLine("==== A ====");
				Console.WriteLine("\"" + s.TextA + "\"");
				Console.WriteLine("==== B ====");
				Console.WriteLine("\"" + s.TextB + "\"");
				Console.WriteLine("-------------------\n");
			}
		}

		[Test]
		public void IsBinary()
		{
			Assert.AreEqual(true, Diff.IsBinary("GitSharp.dll"));
			Assert.AreEqual(true, Diff.IsBinary("GitSharp.Core.dll"));
			Assert.AreEqual(true, Diff.IsBinary("Resources/pack-3280af9c07ee18a87705ef50b0cc4cd20266cf12.idx"));
			Assert.AreEqual(false, Diff.IsBinary("Resources/Diff/E.patch"));
		}
	}
}
