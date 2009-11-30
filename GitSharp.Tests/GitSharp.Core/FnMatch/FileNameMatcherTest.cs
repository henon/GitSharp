/*
 * Copyright (C) 2008, Florian Köberle <florianskarten@web.de>
 * Copyright (C) 2009, Adriano Machado <adriano.m.machado@hotmail.com>
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
using GitSharp.Core.Exceptions;
using GitSharp.Core.FnMatch;
using GitSharp.Core.Tests.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests.FnMatch
{
	[TestFixture]
	public class FileNameMatcherTest
	{
		private static void AssertMatch(string pattern, string input, bool matchExpected, bool appendCanMatchExpected)
		{
			var matcher = new FileNameMatcher(pattern, null);
			matcher.Append(input);
			Assert.AreEqual(matchExpected, matcher.IsMatch());
			Assert.AreEqual(appendCanMatchExpected, matcher.CanAppendMatch());
		}

		private static void AssertFileNameMatch(string pattern, string input, char excludedCharacter, bool matchExpected, bool appendCanMatchExpected)
		{
			var matcher = new FileNameMatcher(pattern, excludedCharacter);
			matcher.Append(input);
			Assert.AreEqual(matchExpected, matcher.IsMatch());
			Assert.AreEqual(appendCanMatchExpected, matcher.CanAppendMatch());
		}

		[Test]
		public virtual void testVerySimplePatternCase0()
		{
			AssertMatch(string.Empty, string.Empty, true, false);
		}

		[Test]
		public virtual void testVerySimplePatternCase1()
		{
			AssertMatch("ab", "a", false, true);
		}

		[Test]
		public virtual void testVerySimplePatternCase2()
		{
			AssertMatch("ab", "ab", true, false);
		}

		[Test]
		public virtual void testVerySimplePatternCase3()
		{
			AssertMatch("ab", "ac", false, false);
		}

		[Test]
		public virtual void testVerySimplePatternCase4()
		{
			AssertMatch("ab", "abc", false, false);
		}

		[Test]
		public virtual void testVerySimpleWirdcardCase0()
		{
			AssertMatch("?", "a", true, false);
		}

		[Test]
		public virtual void testVerySimpleWildCardCase1()
		{
			AssertMatch("??", "a", false, true);
		}

		[Test]
		public virtual void testVerySimpleWildCardCase2()
		{
			AssertMatch("??", "ab", true, false);
		}

		[Test]
		public virtual void testVerySimpleWildCardCase3()
		{
			AssertMatch("??", "abc", false, false);
		}

		[Test]
		public virtual void testVerySimpleStarCase0()
		{
			AssertMatch("*", string.Empty, true, true);
		}

		[Test]
		public virtual void testVerySimpleStarCase1()
		{
			AssertMatch("*", "a", true, true);
		}

		[Test]
		public virtual void testVerySimpleStarCase2()
		{
			AssertMatch("*", "ab", true, true);
		}

		[Test]
		public virtual void testSimpleStarCase0()
		{
			AssertMatch("a*b", "a", false, true);
		}

		[Test]
		public virtual void testSimpleStarCase1()
		{
			AssertMatch("a*c", "ac", true, true);
		}

		[Test]
		public virtual void testSimpleStarCase2()
		{
			AssertMatch("a*c", "ab", false, true);
		}

		[Test]
		public virtual void testSimpleStarCase3()
		{
			AssertMatch("a*c", "abc", true, true);
		}

		[Test]
		public virtual void testManySolutionsCase0()
		{
			AssertMatch("a*a*a", "aaa", true, true);
		}

		[Test]
		public virtual void testManySolutionsCase1()
		{
			AssertMatch("a*a*a", "aaaa", true, true);
		}

		[Test]
		public virtual void testManySolutionsCase2()
		{
			AssertMatch("a*a*a", "ababa", true, true);
		}

		[Test]
		public virtual void testManySolutionsCase3()
		{
			AssertMatch("a*a*a", "aaaaaaaa", true, true);
		}

		[Test]
		public virtual void testManySolutionsCase4()
		{
			AssertMatch("a*a*a", "aaaaaaab", false, true);
		}

		[Test]
		public virtual void testVerySimpleGroupCase0()
		{
			AssertMatch("[ab]", "a", true, false);
		}

		[Test]
		public virtual void testVerySimpleGroupCase1()
		{
			AssertMatch("[ab]", "b", true, false);
		}

		[Test]
		public virtual void testVerySimpleGroupCase2()
		{
			AssertMatch("[ab]", "ab", false, false);
		}

		[Test]
		public virtual void testVerySimpleGroupRangeCase0()
		{
			AssertMatch("[b-d]", "a", false, false);
		}

		[Test]
		public virtual void testVerySimpleGroupRangeCase1()
		{
			AssertMatch("[b-d]", "b", true, false);
		}

		[Test]
		public virtual void testVerySimpleGroupRangeCase2()
		{
			AssertMatch("[b-d]", "c", true, false);
		}

		[Test]
		public virtual void testVerySimpleGroupRangeCase3()
		{
			AssertMatch("[b-d]", "d", true, false);
		}

		[Test]
		public virtual void testVerySimpleGroupRangeCase4()
		{
			AssertMatch("[b-d]", "e", false, false);
		}

		[Test]
		public virtual void testVerySimpleGroupRangeCase5()
		{
			AssertMatch("[b-d]", "-", false, false);
		}

		[Test]
		public virtual void testTwoGroupsCase0()
		{
			AssertMatch("[b-d][ab]", "bb", true, false);
		}

		[Test]
		public virtual void testTwoGroupsCase1()
		{
			AssertMatch("[b-d][ab]", "ca", true, false);
		}

		[Test]
		public virtual void testTwoGroupsCase2()
		{
			AssertMatch("[b-d][ab]", "fa", false, false);
		}

		[Test]
		public virtual void testTwoGroupsCase3()
		{
			AssertMatch("[b-d][ab]", "bc", false, false);
		}

		[Test]
		public virtual void testTwoRangesInOneGroupCase0()
		{
			AssertMatch("[b-ce-e]", "a", false, false);
		}

		[Test]
		public virtual void testTwoRangesInOneGroupCase1()
		{
			AssertMatch("[b-ce-e]", "b", true, false);
		}

		[Test]
		public virtual void testTwoRangesInOneGroupCase2()
		{
			AssertMatch("[b-ce-e]", "c", true, false);
		}

		[Test]
		public virtual void testTwoRangesInOneGroupCase3()
		{
			AssertMatch("[b-ce-e]", "d", false, false);
		}

		[Test]
		public virtual void testTwoRangesInOneGroupCase4()
		{
			AssertMatch("[b-ce-e]", "e", true, false);
		}

		[Test]
		public virtual void testTwoRangesInOneGroupCase5()
		{
			AssertMatch("[b-ce-e]", "f", false, false);
		}

		[Test]
		public virtual void testIncompleteRangesInOneGroupCase0()
		{
			AssertMatch("a[b-]", "ab", true, false);
		}

		[Test]
		public virtual void testIncompleteRangesInOneGroupCase1()
		{
			AssertMatch("a[b-]", "ac", false, false);
		}

		[Test]
		public virtual void testIncompleteRangesInOneGroupCase2()
		{
			AssertMatch("a[b-]", "a-", true, false);
		}

		[Test]
		public virtual void testCombinedRangesInOneGroupCase0()
		{
			AssertMatch("[a-c-e]", "b", true, false);
		}

		///	<summary>
		/// The c belongs to the range a-c. "-e" is no valid range so d should not 	match.
		///	</summary>
		///	<exception cref="Exception">for some reasons </exception>
		[Test]
		public virtual void testCombinedRangesInOneGroupCase1()
		{
			AssertMatch("[a-c-e]", "d", false, false);
		}

		[Test]
		public virtual void testCombinedRangesInOneGroupCase2()
		{
			AssertMatch("[a-c-e]", "e", true, false);
		}

		[Test]
		public virtual void testInversedGroupCase0()
		{
			AssertMatch("[!b-c]", "a", true, false);
		}

		[Test]
		public virtual void testInversedGroupCase1()
		{
			AssertMatch("[!b-c]", "b", false, false);
		}

		[Test]
		public virtual void testInversedGroupCase2()
		{
			AssertMatch("[!b-c]", "c", false, false);
		}

		[Test]
		public virtual void testInversedGroupCase3()
		{
			AssertMatch("[!b-c]", "d", true, false);
		}

		[Test]
		public virtual void testAlphaGroupCase0()
		{
			AssertMatch("[[:alpha:]]", "d", true, false);
		}

		[Test]
		public virtual void testAlphaGroupCase1()
		{
			AssertMatch("[[:alpha:]]", ":", false, false);
		}

		[Test]
		public virtual void testAlphaGroupCase2()
		{
			// \u00f6 = 'o' with dots on it
			AssertMatch("[[:alpha:]]", "\u00f6", true, false);
		}

		[Test]
		public virtual void test2AlphaGroupsCase0()
		{
			// \u00f6 = 'o' with dots on it
			AssertMatch("[[:alpha:]][[:alpha:]]", "a\u00f6", true, false);
			AssertMatch("[[:alpha:]][[:alpha:]]", "a1", false, false);
		}

		[Test]
		public virtual void testAlnumGroupCase0()
		{
			AssertMatch("[[:alnum:]]", "a", true, false);
		}

		[Test]
		public virtual void testAlnumGroupCase1()
		{
			AssertMatch("[[:alnum:]]", "1", true, false);
		}

		[Test]
		public virtual void testAlnumGroupCase2()
		{
			AssertMatch("[[:alnum:]]", ":", false, false);
		}

		[Test]
		public virtual void testBlankGroupCase0()
		{
			AssertMatch("[[:blank:]]", " ", true, false);
		}

		[Test]
		public virtual void testBlankGroupCase1()
		{
			AssertMatch("[[:blank:]]", "\t", true, false);
		}

		[Test]
		public virtual void testBlankGroupCase2()
		{
			AssertMatch("[[:blank:]]", "\r", false, false);
		}

		[Test]
		public virtual void testBlankGroupCase3()
		{
			AssertMatch("[[:blank:]]", "\n", false, false);
		}

		[Test]
		public virtual void testBlankGroupCase4()
		{
			AssertMatch("[[:blank:]]", "a", false, false);
		}

		[Test]
		public virtual void testCntrlGroupCase0()
		{
			AssertMatch("[[:cntrl:]]", "a", false, false);
		}

		[Test]
		public virtual void testCntrlGroupCase1()
		{
			AssertMatch("[[:cntrl:]]", Convert.ToString((char)7), true, false);
		}

		[Test]
		public virtual void testDigitGroupCase0()
		{
			AssertMatch("[[:digit:]]", "0", true, false);
		}

		[Test]
		public virtual void testDigitGroupCase1()
		{
			AssertMatch("[[:digit:]]", "5", true, false);
		}

		[Test]
		public virtual void testDigitGroupCase2()
		{
			AssertMatch("[[:digit:]]", "9", true, false);
		}

		[Test]
		public virtual void testDigitGroupCase3()
		{
			// \u06f9 = EXTENDED ARABIC-INDIC DIGIT NINE
			AssertMatch("[[:digit:]]", "\u06f9", true, false);
		}

		[Test]
		public virtual void testDigitGroupCase4()
		{
			AssertMatch("[[:digit:]]", "a", false, false);
		}

		[Test]
		public virtual void testDigitGroupCase5()
		{
			AssertMatch("[[:digit:]]", "]", false, false);
		}

		[Test]
		public virtual void testGraphGroupCase0()
		{
			AssertMatch("[[:graph:]]", "]", true, false);
		}

		[Test]
		public virtual void testGraphGroupCase1()
		{
			AssertMatch("[[:graph:]]", "a", true, false);
		}

		[Test]
		public virtual void testGraphGroupCase2()
		{
			AssertMatch("[[:graph:]]", ".", true, false);
		}

		[Test]
		public virtual void testGraphGroupCase3()
		{
			AssertMatch("[[:graph:]]", "0", true, false);
		}

		[Test]
		public virtual void testGraphGroupCase4()
		{
			AssertMatch("[[:graph:]]", " ", false, false);
		}

		[Test]
		public virtual void testGraphGroupCase5()
		{
			// \u00f6 = 'o' with dots on it
			AssertMatch("[[:graph:]]", "\u00f6", true, false);
		}

		[Test]
		public virtual void testLowerGroupCase0()
		{
			AssertMatch("[[:lower:]]", "a", true, false);
		}

		[Test]
		public virtual void testLowerGroupCase1()
		{
			AssertMatch("[[:lower:]]", "h", true, false);
		}

		[Test]
		public virtual void testLowerGroupCase2()
		{
			AssertMatch("[[:lower:]]", "A", false, false);
		}

		[Test]
		public virtual void testLowerGroupCase3()
		{
			AssertMatch("[[:lower:]]", "H", false, false);
		}

		[Test]
		public virtual void testLowerGroupCase4()
		{
			// \u00e4 = small 'a' with dots on it
			AssertMatch("[[:lower:]]", "\u00e4", true, false);
		}

		[Test]
		public virtual void testLowerGroupCase5()
		{
			AssertMatch("[[:lower:]]", ".", false, false);
		}

		[Test]
		public virtual void testPrintGroupCase0()
		{
			AssertMatch("[[:print:]]", "]", true, false);
		}

		[Test]
		public virtual void testPrintGroupCase1()
		{
			AssertMatch("[[:print:]]", "a", true, false);
		}

		[Test]
		public virtual void testPrintGroupCase2()
		{
			AssertMatch("[[:print:]]", ".", true, false);
		}

		[Test]
		public virtual void testPrintGroupCase3()
		{
			AssertMatch("[[:print:]]", "0", true, false);
		}

		[Test]
		public virtual void testPrintGroupCase4()
		{
			AssertMatch("[[:print:]]", " ", true, false);
		}

		[Test]
		public virtual void testPrintGroupCase5()
		{
			// \u00f6 = 'o' with dots on it
			AssertMatch("[[:print:]]", "\u00f6", true, false);
		}

		[Test]
		public virtual void testPunctGroupCase0()
		{
			AssertMatch("[[:punct:]]", ".", true, false);
		}

		[Test]
		public virtual void testPunctGroupCase1()
		{
			AssertMatch("[[:punct:]]", "@", true, false);
		}

		[Test]
		public virtual void testPunctGroupCase2()
		{
			AssertMatch("[[:punct:]]", " ", false, false);
		}

		[Test]
		public virtual void testPunctGroupCase3()
		{
			AssertMatch("[[:punct:]]", "a", false, false);
		}

		[Test]
		public virtual void testSpaceGroupCase0()
		{
			AssertMatch("[[:space:]]", " ", true, false);
		}

		[Test]
		public virtual void testSpaceGroupCase1()
		{
			AssertMatch("[[:space:]]", "\t", true, false);
		}

		[Test]
		public virtual void testSpaceGroupCase2()
		{
			AssertMatch("[[:space:]]", "\r", true, false);
		}

		[Test]
		public virtual void testSpaceGroupCase3()
		{
			AssertMatch("[[:space:]]", "\n", true, false);
		}

		[Test]
		public virtual void testSpaceGroupCase4()
		{
			AssertMatch("[[:space:]]", "a", false, false);
		}

		[Test]
		public virtual void testUpperGroupCase0()
		{
			AssertMatch("[[:upper:]]", "a", false, false);
		}

		[Test]
		public virtual void testUpperGroupCase1()
		{
			AssertMatch("[[:upper:]]", "h", false, false);
		}

		[Test]
		public virtual void testUpperGroupCase2()
		{
			AssertMatch("[[:upper:]]", "A", true, false);
		}

		[Test]
		public virtual void testUpperGroupCase3()
		{
			AssertMatch("[[:upper:]]", "H", true, false);
		}

		[Test]
		public virtual void testUpperGroupCase4()
		{
			// \u00c4 = 'A' with dots on it
			AssertMatch("[[:upper:]]", "\u00c4", true, false);
		}

		[Test]
		public virtual void testUpperGroupCase5()
		{
			AssertMatch("[[:upper:]]", ".", false, false);
		}

		[Test]
		public virtual void testXDigitGroupCase0()
		{
			AssertMatch("[[:xdigit:]]", "a", true, false);
		}

		[Test]
		public virtual void testXDigitGroupCase1()
		{
			AssertMatch("[[:xdigit:]]", "d", true, false);
		}

		[Test]
		public virtual void testXDigitGroupCase2()
		{
			AssertMatch("[[:xdigit:]]", "f", true, false);
		}

		[Test]
		public virtual void testXDigitGroupCase3()
		{
			AssertMatch("[[:xdigit:]]", "0", true, false);
		}

		[Test]
		public virtual void testXDigitGroupCase4()
		{
			AssertMatch("[[:xdigit:]]", "5", true, false);
		}

		[Test]
		public virtual void testXDigitGroupCase5()
		{
			AssertMatch("[[:xdigit:]]", "9", true, false);
		}

		[Test]
		public virtual void testXDigitGroupCase6()
		{
			AssertMatch("[[:xdigit:]]", "۹", false, false);
		}

		[Test]
		public virtual void testXDigitGroupCase7()
		{
			AssertMatch("[[:xdigit:]]", ".", false, false);
		}

		[Test]
		public virtual void testWordroupCase0()
		{
			AssertMatch("[[:word:]]", "g", true, false);
		}

		[Test]
		public virtual void testWordroupCase1()
		{
			// \u00f6 = 'o' with dots on it
			AssertMatch("[[:word:]]", "\u00f6", true, false);
		}

		[Test]
		public virtual void testWordroupCase2()
		{
			AssertMatch("[[:word:]]", "5", true, false);
		}

		[Test]
		public virtual void testWordroupCase3()
		{
			AssertMatch("[[:word:]]", "_", true, false);
		}

		[Test]
		public virtual void testWordroupCase4()
		{
			AssertMatch("[[:word:]]", " ", false, false);
		}

		[Test]
		public virtual void testWordroupCase5()
		{
			AssertMatch("[[:word:]]", ".", false, false);
		}

		[Test]
		public virtual void testMixedGroupCase0()
		{
			AssertMatch("[A[:lower:]C3-5]", "A", true, false);
		}

		[Test]
		public virtual void testMixedGroupCase1()
		{
			AssertMatch("[A[:lower:]C3-5]", "C", true, false);
		}

		[Test]
		public virtual void testMixedGroupCase2()
		{
			AssertMatch("[A[:lower:]C3-5]", "e", true, false);
		}

		[Test]
		public virtual void testMixedGroupCase3()
		{
			AssertMatch("[A[:lower:]C3-5]", "3", true, false);
		}

		[Test]
		public virtual void testMixedGroupCase4()
		{
			AssertMatch("[A[:lower:]C3-5]", "4", true, false);
		}

		[Test]
		public virtual void testMixedGroupCase5()
		{
			AssertMatch("[A[:lower:]C3-5]", "5", true, false);
		}

		[Test]
		public virtual void testMixedGroupCase6()
		{
			AssertMatch("[A[:lower:]C3-5]", "B", false, false);
		}

		[Test]
		public virtual void testMixedGroupCase7()
		{
			AssertMatch("[A[:lower:]C3-5]", "2", false, false);
		}

		[Test]
		public virtual void testMixedGroupCase8()
		{
			AssertMatch("[A[:lower:]C3-5]", "6", false, false);
		}

		[Test]
		public virtual void testMixedGroupCase9()
		{
			AssertMatch("[A[:lower:]C3-5]", ".", false, false);
		}

		[Test]
		public virtual void testSpecialGroupCase0()
		{
			AssertMatch("[[]", "[", true, false);
		}

		[Test]
		public virtual void testSpecialGroupCase1()
		{
			AssertMatch("[]]", "]", true, false);
		}

		[Test]
		public virtual void testSpecialGroupCase2()
		{
			AssertMatch("[]a]", "]", true, false);
		}

		[Test]
		public virtual void testSpecialGroupCase3()
		{
			AssertMatch("[a[]", "[", true, false);
		}

		[Test]
		public virtual void testSpecialGroupCase4()
		{
			AssertMatch("[a[]", "a", true, false);
		}

		[Test]
		public virtual void testSpecialGroupCase5()
		{
			AssertMatch("[!]]", "]", false, false);
		}

		[Test]
		public virtual void testSpecialGroupCase6()
		{
			AssertMatch("[!]]", "x", true, false);
		}

		[Test]
		public virtual void testSpecialGroupCase7()
		{
			AssertMatch("[:]]", ":]", true, false);
		}

		[Test]
		public virtual void testSpecialGroupCase8()
		{
			AssertMatch("[:]]", ":", false, true);
		}

		[Test]
		public virtual void testSpecialGroupCase9()
		{
            AssertHelper.Throws<InvalidPatternException>(() => AssertMatch("[[:]", ":", true, true));
		}

		[Test]
		public virtual void testUnsupportedGroupCase0()
		{
			try
			{
				AssertMatch("[[=a=]]", "b", false, false);
			}
			catch (InvalidPatternException e)
			{
				Assert.IsTrue(e.Message.Contains("[=a=]"));
			}
		}

		[Test]
		public virtual void testUnsupportedGroupCase1()
		{
			try
			{
				AssertMatch("[[.a.]]", "b", false, false);
			}
			catch (InvalidPatternException e)
			{
				Assert.IsTrue(e.Message.Contains("[.a.]"));
			}
		}

		[Test]
		public virtual void testFilePathSimpleCase()
		{
			AssertFileNameMatch("a/b", "a/b", '/', true, false);
		}

		[Test]
		public virtual void testFilePathCase0()
		{
			AssertFileNameMatch("a*b", "a/b", '/', false, false);
		}

		[Test]
		public virtual void testFilePathCase1()
		{
			AssertFileNameMatch("a?b", "a/b", '/', false, false);
		}

		[Test]
		public virtual void testFilePathCase2()
		{
			AssertFileNameMatch("a*b", "a\\b", '\\', false, false);
		}

		[Test]
		public virtual void testFilePathCase3()
		{
			AssertFileNameMatch("a?b", "a\\b", '\\', false, false);
		}

		[Test]
		public virtual void testReset()
		{
			const string pattern = "helloworld";

			var matcher = new FileNameMatcher(pattern, null);
			matcher.Append("helloworld");
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			matcher.Reset();
			matcher.Append("hello");
			Assert.AreEqual(false, matcher.IsMatch());
			Assert.AreEqual(true, matcher.CanAppendMatch());
			matcher.Append("world");
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			matcher.Append("to much");
			Assert.AreEqual(false, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			matcher.Reset();
			matcher.Append("helloworld");
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
		}

		[Test]
		public virtual void testCreateMatcherForSuffix()
		{
			const string pattern = "helloworld";

			var matcher = new FileNameMatcher(pattern, null);
			matcher.Append("hello");

			FileNameMatcher childMatcher = matcher.CreateMatcherForSuffix();
			Assert.AreEqual(false, matcher.IsMatch());
			Assert.AreEqual(true, matcher.CanAppendMatch());
			Assert.AreEqual(false, childMatcher.IsMatch());
			Assert.AreEqual(true, childMatcher.CanAppendMatch());
			matcher.Append("world");
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			Assert.AreEqual(false, childMatcher.IsMatch());
			Assert.AreEqual(true, childMatcher.CanAppendMatch());
			childMatcher.Append("world");
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			Assert.AreEqual(true, childMatcher.IsMatch());
			Assert.AreEqual(false, childMatcher.CanAppendMatch());
			childMatcher.Reset();
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			Assert.AreEqual(false, childMatcher.IsMatch());
			Assert.AreEqual(true, childMatcher.CanAppendMatch());
			childMatcher.Append("world");
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			Assert.AreEqual(true, childMatcher.IsMatch());
			Assert.AreEqual(false, childMatcher.CanAppendMatch());
		}

		[Test]
		public virtual void testCopyConstructor()
		{
			const string pattern = "helloworld";

			var matcher = new FileNameMatcher(pattern, null);
			matcher.Append("hello");

			var copy = new FileNameMatcher(matcher);
			Assert.AreEqual(false, matcher.IsMatch());
			Assert.AreEqual(true, matcher.CanAppendMatch());
			Assert.AreEqual(false, copy.IsMatch());
			Assert.AreEqual(true, copy.CanAppendMatch());
			matcher.Append("world");
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			Assert.AreEqual(false, copy.IsMatch());
			Assert.AreEqual(true, copy.CanAppendMatch());
			copy.Append("world");
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			Assert.AreEqual(true, copy.IsMatch());
			Assert.AreEqual(false, copy.CanAppendMatch());
			copy.Reset();
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			Assert.AreEqual(false, copy.IsMatch());
			Assert.AreEqual(true, copy.CanAppendMatch());
			copy.Append("helloworld");
			Assert.AreEqual(true, matcher.IsMatch());
			Assert.AreEqual(false, matcher.CanAppendMatch());
			Assert.AreEqual(true, copy.IsMatch());
			Assert.AreEqual(false, copy.CanAppendMatch());
		}
	}
}