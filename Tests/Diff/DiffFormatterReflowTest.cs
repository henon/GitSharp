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
using GitSharp.Diff;
using GitSharp.Patch;
using GitSharp.Tests.Patch;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests.Diff
{
	public class DiffFormatterReflowTest : BasePatchTest
	{
		private RawText _a;
		private RawText _b;
		private FileHeader _file;
		private MemoryStream _memoryStream;
		private DiffFormatter _fmt;

		protected override void SetUp()
		{
			_memoryStream = new MemoryStream();
			_fmt = new DiffFormatter();
		}

		[StrictFactAttribute]
		public void testNegativeContextFails()
		{
			Init("X");
		    Assert.Throws<ArgumentException>(() => _fmt.setContext(-1));
		}

		[StrictFactAttribute]
		public void testContext0()
		{
			Init("X");
			_fmt.setContext(0);
            AssertFormatted("testContext0.out");
		}

		[StrictFactAttribute]
		public void testContext1()
		{
			Init("X");
			_fmt.setContext(1);
            AssertFormatted("testContext1.out");
		}

		[StrictFactAttribute]
		public void testContext3()
		{
			Init("X");
			_fmt.setContext(3);
            AssertFormatted("testContext3.out");
		}

		[StrictFactAttribute]
		public void testContext5()
		{
			Init("X");
			_fmt.setContext(5);
            AssertFormatted("testContext5.out");
		}

		[StrictFactAttribute]
		public void testContext10()
		{
			Init("X");
			_fmt.setContext(10);
            AssertFormatted("testContext10.out");
		}

		[StrictFactAttribute]
		public void testContext100()
		{
			Init("X");
			_fmt.setContext(100);
            AssertFormatted("testContext100.out");
		}

		[StrictFactAttribute]
		public void testEmpty1()
		{
			Init("E");
			AssertFormatted("E.patch");
		}

		[StrictFactAttribute]
		public void testNoNewLine1()
		{
			Init("Y");
			AssertFormatted("Y.patch");
		}

		[StrictFactAttribute]
		public void testNoNewLine2()
		{
			Init("Z");
			AssertFormatted("Z.patch");
		}

		private void Init(string name)
		{
			_a = new RawText(ReadFile(name + "_PreImage"));
			_b = new RawText(ReadFile(name + "_PostImage"));
			_file = ParseTestPatchFile(DiffsDir + name + ".patch").getFiles()[0];
		}

		private void AssertFormatted(string name)
		{
			_fmt.format(_memoryStream, _file, _a, _b);
			string exp = RawParseUtils.decode(ReadFile(name));
			Assert.Equal(exp, RawParseUtils.decode(_memoryStream.ToArray()));
		}

		private static byte[] ReadFile(string patchFile)
		{
            return File.ReadAllBytes(DiffsDir + patchFile);
		}
	}
}