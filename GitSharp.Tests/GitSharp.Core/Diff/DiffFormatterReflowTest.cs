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
using GitSharp.Core.Diff;
using GitSharp.Core.Patch;
using GitSharp.Core.Tests.Patch;
using GitSharp.Core.Util;
using GitSharp.Core.Tests.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Diff
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
			Init("X");
		    AssertHelper.Throws<ArgumentException>(() => fmt.setContext(-1));
		}

		[Test]
		public void testContext0()
		{
			Init("X");
			fmt.setContext(0);
            AssertFormatted("testContext0.out");
		}

		[Test]
		public void testContext1()
		{
			Init("X");
			fmt.setContext(1);
            AssertFormatted("testContext1.out");
		}

		[Test]
		public void testContext3()
		{
			Init("X");
			fmt.setContext(3);
            AssertFormatted("testContext3.out");
		}

		[Test]
		public void testContext5()
		{
			Init("X");
			fmt.setContext(5);
            AssertFormatted("testContext5.out");
		}

		[Test]
		public void testContext10()
		{
			Init("X");
			fmt.setContext(10);
            AssertFormatted("testContext10.out");
		}

		[Test]
		public void testContext100()
		{
			Init("X");
			fmt.setContext(100);
            AssertFormatted("testContext100.out");
		}

		[Test]
		public void testEmpty1()
		{
			Init("E");
			AssertFormatted("E.patch");
		}

		[Test]
		public void testNoNewLine1()
		{
			Init("Y");
			AssertFormatted("Y.patch");
		}

		[Test]
		public void testNoNewLine2()
		{
			Init("Z");
			AssertFormatted("Z.patch");
		}

		private void Init(string name)
		{
			a = new RawText(ReadFile(name + "_PreImage"));
			b = new RawText(ReadFile(name + "_PostImage"));
			file = ParseTestPatchFile(DiffsDir + name + ".patch").getFiles()[0];
		}

		private void AssertFormatted(string name)
		{
			fmt.format(memoryStream, file, a, b);
			string exp = RawParseUtils.decode(ReadFile(name));
			Assert.AreEqual(exp, RawParseUtils.decode(memoryStream.ToArray()));
		}

		private static byte[] ReadFile(string patchFile)
		{
            return File.ReadAllBytes(DiffsDir + patchFile);
		}
	}
}