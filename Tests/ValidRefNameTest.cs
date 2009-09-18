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

using Xunit;

namespace GitSharp.Tests
{
	public class ValidRefNameTest
	{
		private static void AssertValid(bool exp, string name)
		{
			Assert.Equal(exp, Repository.IsValidRefName(name));
		}

		[StrictFactAttribute]
		public void testEmptyString()
		{
			AssertValid(false, string.Empty);
			AssertValid(false, "/");
		}

		[StrictFactAttribute]
		public void testMustHaveTwoComponents()
		{
			AssertValid(false, "master");
			AssertValid(true, "heads/master");
		}

		[StrictFactAttribute]
		public void testValidHead()
		{
			AssertValid(true, "refs/heads/master");
			AssertValid(true, "refs/heads/pu");
			AssertValid(true, "refs/heads/z");
			AssertValid(true, "refs/heads/FoO");
		}

		[StrictFactAttribute]
		public void testValidTag()
		{
			AssertValid(true, "refs/tags/v1.0");
		}

		[StrictFactAttribute]
		public void testNoLockSuffix()
		{
			AssertValid(false, "refs/heads/master.lock");
		}

		[StrictFactAttribute]
		public void testNoDirectorySuffix()
		{
			AssertValid(false, "refs/heads/master/");
		}

		[StrictFactAttribute]
		public void testNoSpace()
		{
			AssertValid(false, "refs/heads/i haz space");
		}

		[StrictFactAttribute]
		public void testNoAsciiControlCharacters()
		{
			for (char c = '\0'; c < ' '; c++)
				AssertValid(false, "refs/heads/mast" + c + "er");
		}

		[StrictFactAttribute]
		public void testNoBareDot()
		{
			AssertValid(false, "refs/heads/.");
			AssertValid(false, "refs/heads/..");
			AssertValid(false, "refs/heads/./master");
			AssertValid(false, "refs/heads/../master");
		}

		[StrictFactAttribute]
		public void testNoLeadingOrTrailingDot()
		{
			AssertValid(false, ".");
			AssertValid(false, "refs/heads/.bar");
			AssertValid(false, "refs/heads/..bar");
			AssertValid(false, "refs/heads/bar.");
		}

		[StrictFactAttribute]
		public void testContainsDot()
		{
			AssertValid(true, "refs/heads/m.a.s.t.e.r");
			AssertValid(false, "refs/heads/master..pu");
		}

		[StrictFactAttribute]
		public void testNoMagicRefCharacters()
		{
			AssertValid(false, "refs/heads/master^");
			AssertValid(false, "refs/heads/^master");
			AssertValid(false, "^refs/heads/master");

			AssertValid(false, "refs/heads/master~");
			AssertValid(false, "refs/heads/~master");
			AssertValid(false, "~refs/heads/master");

			AssertValid(false, "refs/heads/master:");
			AssertValid(false, "refs/heads/:master");
			AssertValid(false, ":refs/heads/master");
		}

		[StrictFactAttribute]
		public void testShellGlob()
		{
			AssertValid(false, "refs/heads/master?");
			AssertValid(false, "refs/heads/?master");
			AssertValid(false, "?refs/heads/master");

			AssertValid(false, "refs/heads/master[");
			AssertValid(false, "refs/heads/[master");
			AssertValid(false, "[refs/heads/master");

			AssertValid(false, "refs/heads/master*");
			AssertValid(false, "refs/heads/*master");
			AssertValid(false, "*refs/heads/master");
		}

		[StrictFactAttribute]
		public void testValidSpecialCharacters()
		{
			AssertValid(true, "refs/heads/!");
			AssertValid(true, "refs/heads/\"");
			AssertValid(true, "refs/heads/#");
			AssertValid(true, "refs/heads/$");
			AssertValid(true, "refs/heads/%");
			AssertValid(true, "refs/heads/&");
			AssertValid(true, "refs/heads/'");
			AssertValid(true, "refs/heads/(");
			AssertValid(true, "refs/heads/)");
			AssertValid(true, "refs/heads/+");
			AssertValid(true, "refs/heads/,");
			AssertValid(true, "refs/heads/-");
			AssertValid(true, "refs/heads/;");
			AssertValid(true, "refs/heads/<");
			AssertValid(true, "refs/heads/=");
			AssertValid(true, "refs/heads/>");
			AssertValid(true, "refs/heads/@");
			AssertValid(true, "refs/heads/]");
			AssertValid(true, "refs/heads/_");
			AssertValid(true, "refs/heads/`");
			AssertValid(true, "refs/heads/{");
			AssertValid(true, "refs/heads/|");
			AssertValid(true, "refs/heads/}");

			// This is valid on UNIX, but not on Windows
			// hence we make in invalid due to non-portability
			//
			AssertValid(false, "refs/heads/\\");
		}

		[StrictFactAttribute]
		public void testUnicodeNames()
		{
			AssertValid(true, "refs/heads/\u00e5ngstr\u00f6m");
		}

		[StrictFactAttribute]
		public void testRefLogQueryIsValidRef()
		{
			AssertValid(false, "refs/heads/master@{1}");
			AssertValid(false, "refs/heads/master@{1.hour.ago}");
		}
	}
}
