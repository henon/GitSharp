/*
 * Copyright (C) 2009, Google Inc.
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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GitSharp.Diff;
using GitSharp.Patch;
using GitSharp.Tests.Patch;
using GitSharp.Util;
using NUnit.Framework;

namespace GitSharp.Tests.Diff
{
	[TestFixture]
	public class DiffFormatterReflowTest : BasePatchTest
	{
		private RawText a;
		private RawText b;
		private FileHeader file;
		private MemoryStream memoryStream;
		private DiffFormatter fmt;

		[SetUp]
		protected void setUp()
		{
			memoryStream = new MemoryStream();
			fmt = new DiffFormatter();
		}

		[Test]
		public void testNegativeContextFails()
		{
			init("X");
		    AssertHelper.Throws<ArgumentException>(() => fmt.setContext(-1));
		}

		[Test]
		public void testContext0()
		{
			init("X");
			fmt.setContext(0);
			assertFormatted();
		}

		[Test]
		public void testContext1()
		{
			init("X");
			fmt.setContext(1);
			assertFormatted();
		}

		[Test]
		public void testContext3()
		{
			init("X");
			fmt.setContext(3);
			assertFormatted();
		}

		[Test]
		public void testContext5()
		{
			init("X");
			fmt.setContext(5);
			assertFormatted();
		}

		[Test]
		public void testContext10()
		{
			init("X");
			fmt.setContext(10);
			assertFormatted();
		}

		[Test]
		public void testContext100()
		{
			init("X");
			fmt.setContext(100);
			assertFormatted();
		}

		[Test]
		public void testEmpty1()
		{
			init("E");
			assertFormatted("E.patch");
		}

		[Test]
		public void testNoNewLine1()
		{
			init("Y");
			assertFormatted("Y.patch");
		}

		[Test]
		public void testNoNewLine2()
		{
			init("Z");
			assertFormatted("Z.patch");
		}

		private void init(string name)
		{
			a = new RawText(readFile(name + "_PreImage"));
			b = new RawText(readFile(name + "_PostImage"));
			file = parseTestPatchFile(DIFFS_DIR + name + ".patch").getFiles()[0];
		}

		private void assertFormatted()
		{
		    var methodName = new System.Diagnostics.StackTrace(false).GetFrame(1).GetMethod().Name;
            assertFormatted(methodName + ".out");
		}

		private void assertFormatted(string name)
		{
			fmt.format(memoryStream, file, a, b);
			string exp = RawParseUtils.decode(readFile(name));
			Assert.AreEqual(exp, RawParseUtils.decode(memoryStream.ToArray()));
		}

		private byte[] readFile(string patchFile)
		{
            return File.ReadAllBytes(DIFFS_DIR + patchFile);
		}
	}
}