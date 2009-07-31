/*
 * Copyright (C) 2008, Florian Köberle <florianskarten@web.de>
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

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests
{
    [TestFixture]
    public class FileNameMatcherTest
    {
#if false
	private void assertMatch(final String pattern, final String input,
			final boolean matchExpected, final boolean appendCanMatchExpected)
			throws InvalidPatternException {
		final FileNameMatcher matcher = new FileNameMatcher(pattern, null);
		matcher.append(input);
		assertEquals(matchExpected, matcher.isMatch());
		assertEquals(appendCanMatchExpected, matcher.canAppendMatch());
	}

	private void assertFileNameMatch(final String pattern, final String input,
			final char excludedCharacter, final boolean matchExpected,
			final boolean appendCanMatchExpected)
			throws InvalidPatternException {
		final FileNameMatcher matcher = new FileNameMatcher(pattern,
				new Character(excludedCharacter));
		matcher.append(input);
		assertEquals(matchExpected, matcher.isMatch());
		assertEquals(appendCanMatchExpected, matcher.canAppendMatch());
	}

	public void testVerySimplePatternCase0() throws Exception {
		assertMatch("", "", true, false);
	}

	public void testVerySimplePatternCase1() throws Exception {
		assertMatch("ab", "a", false, true);
	}

	public void testVerySimplePatternCase2() throws Exception {
		assertMatch("ab", "ab", true, false);
	}

	public void testVerySimplePatternCase3() throws Exception {
		assertMatch("ab", "ac", false, false);
	}

	public void testVerySimplePatternCase4() throws Exception {
		assertMatch("ab", "abc", false, false);
	}

	public void testVerySimpleWirdcardCase0() throws Exception {
		assertMatch("?", "a", true, false);
	}

	public void testVerySimpleWildCardCase1() throws Exception {
		assertMatch("??", "a", false, true);
	}

	public void testVerySimpleWildCardCase2() throws Exception {
		assertMatch("??", "ab", true, false);
	}

	public void testVerySimpleWildCardCase3() throws Exception {
		assertMatch("??", "abc", false, false);
	}

	public void testVerySimpleStarCase0() throws Exception {
		assertMatch("*", "", true, true);
	}

	public void testVerySimpleStarCase1() throws Exception {
		assertMatch("*", "a", true, true);
	}

	public void testVerySimpleStarCase2() throws Exception {
		assertMatch("*", "ab", true, true);
	}

	public void testSimpleStarCase0() throws Exception {
		assertMatch("a*b", "a", false, true);
	}

	public void testSimpleStarCase1() throws Exception {
		assertMatch("a*c", "ac", true, true);
	}

	public void testSimpleStarCase2() throws Exception {
		assertMatch("a*c", "ab", false, true);
	}

	public void testSimpleStarCase3() throws Exception {
		assertMatch("a*c", "abc", true, true);
	}

	public void testManySolutionsCase0() throws Exception {
		assertMatch("a*a*a", "aaa", true, true);
	}

	public void testManySolutionsCase1() throws Exception {
		assertMatch("a*a*a", "aaaa", true, true);
	}

	public void testManySolutionsCase2() throws Exception {
		assertMatch("a*a*a", "ababa", true, true);
	}

	public void testManySolutionsCase3() throws Exception {
		assertMatch("a*a*a", "aaaaaaaa", true, true);
	}

	public void testManySolutionsCase4() throws Exception {
		assertMatch("a*a*a", "aaaaaaab", false, true);
	}

	public void testVerySimpleGroupCase0() throws Exception {
		assertMatch("[ab]", "a", true, false);
	}

	public void testVerySimpleGroupCase1() throws Exception {
		assertMatch("[ab]", "b", true, false);
	}

	public void testVerySimpleGroupCase2() throws Exception {
		assertMatch("[ab]", "ab", false, false);
	}

	public void testVerySimpleGroupRangeCase0() throws Exception {
		assertMatch("[b-d]", "a", false, false);
	}

	public void testVerySimpleGroupRangeCase1() throws Exception {
		assertMatch("[b-d]", "b", true, false);
	}

	public void testVerySimpleGroupRangeCase2() throws Exception {
		assertMatch("[b-d]", "c", true, false);
	}

	public void testVerySimpleGroupRangeCase3() throws Exception {
		assertMatch("[b-d]", "d", true, false);
	}

	public void testVerySimpleGroupRangeCase4() throws Exception {
		assertMatch("[b-d]", "e", false, false);
	}

	public void testVerySimpleGroupRangeCase5() throws Exception {
		assertMatch("[b-d]", "-", false, false);
	}

	public void testTwoGroupsCase0() throws Exception {
		assertMatch("[b-d][ab]", "bb", true, false);
	}

	public void testTwoGroupsCase1() throws Exception {
		assertMatch("[b-d][ab]", "ca", true, false);
	}

	public void testTwoGroupsCase2() throws Exception {
		assertMatch("[b-d][ab]", "fa", false, false);
	}

	public void testTwoGroupsCase3() throws Exception {
		assertMatch("[b-d][ab]", "bc", false, false);
	}

	public void testTwoRangesInOneGroupCase0() throws Exception {
		assertMatch("[b-ce-e]", "a", false, false);
	}

	public void testTwoRangesInOneGroupCase1() throws Exception {
		assertMatch("[b-ce-e]", "b", true, false);
	}

	public void testTwoRangesInOneGroupCase2() throws Exception {
		assertMatch("[b-ce-e]", "c", true, false);
	}

	public void testTwoRangesInOneGroupCase3() throws Exception {
		assertMatch("[b-ce-e]", "d", false, false);
	}

	public void testTwoRangesInOneGroupCase4() throws Exception {
		assertMatch("[b-ce-e]", "e", true, false);
	}

	public void testTwoRangesInOneGroupCase5() throws Exception {
		assertMatch("[b-ce-e]", "f", false, false);
	}

	public void testIncompleteRangesInOneGroupCase0() throws Exception {
		assertMatch("a[b-]", "ab", true, false);
	}

	public void testIncompleteRangesInOneGroupCase1() throws Exception {
		assertMatch("a[b-]", "ac", false, false);
	}

	public void testIncompleteRangesInOneGroupCase2() throws Exception {
		assertMatch("a[b-]", "a-", true, false);
	}

	public void testCombinedRangesInOneGroupCase0() throws Exception {
		assertMatch("[a-c-e]", "b", true, false);
	}

	/**
	 * The c belongs to the range a-c. "-e" is no valid range so d should not
	 * match.
	 *
	 * @throws Exception
	 *             for some reasons
	 */
	public void testCombinedRangesInOneGroupCase1() throws Exception {
		assertMatch("[a-c-e]", "d", false, false);
	}

	public void testCombinedRangesInOneGroupCase2() throws Exception {
		assertMatch("[a-c-e]", "e", true, false);
	}

	public void testInversedGroupCase0() throws Exception {
		assertMatch("[!b-c]", "a", true, false);
	}

	public void testInversedGroupCase1() throws Exception {
		assertMatch("[!b-c]", "b", false, false);
	}

	public void testInversedGroupCase2() throws Exception {
		assertMatch("[!b-c]", "c", false, false);
	}

	public void testInversedGroupCase3() throws Exception {
		assertMatch("[!b-c]", "d", true, false);
	}

	public void testAlphaGroupCase0() throws Exception {
		assertMatch("[[:alpha:]]", "d", true, false);
	}

	public void testAlphaGroupCase1() throws Exception {
		assertMatch("[[:alpha:]]", ":", false, false);
	}

	public void testAlphaGroupCase2() throws Exception {
		// \u00f6 = 'o' with dots on it
		assertMatch("[[:alpha:]]", "\u00f6", true, false);
	}

	public void test2AlphaGroupsCase0() throws Exception {
		// \u00f6 = 'o' with dots on it
		assertMatch("[[:alpha:]][[:alpha:]]", "a\u00f6", true, false);
		assertMatch("[[:alpha:]][[:alpha:]]", "a1", false, false);
	}

	public void testAlnumGroupCase0() throws Exception {
		assertMatch("[[:alnum:]]", "a", true, false);
	}

	public void testAlnumGroupCase1() throws Exception {
		assertMatch("[[:alnum:]]", "1", true, false);
	}

	public void testAlnumGroupCase2() throws Exception {
		assertMatch("[[:alnum:]]", ":", false, false);
	}

	public void testBlankGroupCase0() throws Exception {
		assertMatch("[[:blank:]]", " ", true, false);
	}

	public void testBlankGroupCase1() throws Exception {
		assertMatch("[[:blank:]]", "\t", true, false);
	}

	public void testBlankGroupCase2() throws Exception {
		assertMatch("[[:blank:]]", "\r", false, false);
	}

	public void testBlankGroupCase3() throws Exception {
		assertMatch("[[:blank:]]", "\n", false, false);
	}

	public void testBlankGroupCase4() throws Exception {
		assertMatch("[[:blank:]]", "a", false, false);
	}

	public void testCntrlGroupCase0() throws Exception {
		assertMatch("[[:cntrl:]]", "a", false, false);
	}

	public void testCntrlGroupCase1() throws Exception {
		assertMatch("[[:cntrl:]]", String.valueOf((char) 7), true, false);
	}

	public void testDigitGroupCase0() throws Exception {
		assertMatch("[[:digit:]]", "0", true, false);
	}

	public void testDigitGroupCase1() throws Exception {
		assertMatch("[[:digit:]]", "5", true, false);
	}

	public void testDigitGroupCase2() throws Exception {
		assertMatch("[[:digit:]]", "9", true, false);
	}

	public void testDigitGroupCase3() throws Exception {
		// \u06f9 = EXTENDED ARABIC-INDIC DIGIT NINE
		assertMatch("[[:digit:]]", "\u06f9", true, false);
	}

	public void testDigitGroupCase4() throws Exception {
		assertMatch("[[:digit:]]", "a", false, false);
	}

	public void testDigitGroupCase5() throws Exception {
		assertMatch("[[:digit:]]", "]", false, false);
	}

	public void testGraphGroupCase0() throws Exception {
		assertMatch("[[:graph:]]", "]", true, false);
	}

	public void testGraphGroupCase1() throws Exception {
		assertMatch("[[:graph:]]", "a", true, false);
	}

	public void testGraphGroupCase2() throws Exception {
		assertMatch("[[:graph:]]", ".", true, false);
	}

	public void testGraphGroupCase3() throws Exception {
		assertMatch("[[:graph:]]", "0", true, false);
	}

	public void testGraphGroupCase4() throws Exception {
		assertMatch("[[:graph:]]", " ", false, false);
	}

	public void testGraphGroupCase5() throws Exception {
		// \u00f6 = 'o' with dots on it
		assertMatch("[[:graph:]]", "\u00f6", true, false);
	}

	public void testLowerGroupCase0() throws Exception {
		assertMatch("[[:lower:]]", "a", true, false);
	}

	public void testLowerGroupCase1() throws Exception {
		assertMatch("[[:lower:]]", "h", true, false);
	}

	public void testLowerGroupCase2() throws Exception {
		assertMatch("[[:lower:]]", "A", false, false);
	}

	public void testLowerGroupCase3() throws Exception {
		assertMatch("[[:lower:]]", "H", false, false);
	}

	public void testLowerGroupCase4() throws Exception {
		// \u00e4 = small 'a' with dots on it
		assertMatch("[[:lower:]]", "\u00e4", true, false);
	}

	public void testLowerGroupCase5() throws Exception {
		assertMatch("[[:lower:]]", ".", false, false);
	}

	public void testPrintGroupCase0() throws Exception {
		assertMatch("[[:print:]]", "]", true, false);
	}

	public void testPrintGroupCase1() throws Exception {
		assertMatch("[[:print:]]", "a", true, false);
	}

	public void testPrintGroupCase2() throws Exception {
		assertMatch("[[:print:]]", ".", true, false);
	}

	public void testPrintGroupCase3() throws Exception {
		assertMatch("[[:print:]]", "0", true, false);
	}

	public void testPrintGroupCase4() throws Exception {
		assertMatch("[[:print:]]", " ", true, false);
	}

	public void testPrintGroupCase5() throws Exception {
		// \u00f6 = 'o' with dots on it
		assertMatch("[[:print:]]", "\u00f6", true, false);
	}

	public void testPunctGroupCase0() throws Exception {
		assertMatch("[[:punct:]]", ".", true, false);
	}

	public void testPunctGroupCase1() throws Exception {
		assertMatch("[[:punct:]]", "@", true, false);
	}

	public void testPunctGroupCase2() throws Exception {
		assertMatch("[[:punct:]]", " ", false, false);
	}

	public void testPunctGroupCase3() throws Exception {
		assertMatch("[[:punct:]]", "a", false, false);
	}

	public void testSpaceGroupCase0() throws Exception {
		assertMatch("[[:space:]]", " ", true, false);
	}

	public void testSpaceGroupCase1() throws Exception {
		assertMatch("[[:space:]]", "\t", true, false);
	}

	public void testSpaceGroupCase2() throws Exception {
		assertMatch("[[:space:]]", "\r", true, false);
	}

	public void testSpaceGroupCase3() throws Exception {
		assertMatch("[[:space:]]", "\n", true, false);
	}

	public void testSpaceGroupCase4() throws Exception {
		assertMatch("[[:space:]]", "a", false, false);
	}

	public void testUpperGroupCase0() throws Exception {
		assertMatch("[[:upper:]]", "a", false, false);
	}

	public void testUpperGroupCase1() throws Exception {
		assertMatch("[[:upper:]]", "h", false, false);
	}

	public void testUpperGroupCase2() throws Exception {
		assertMatch("[[:upper:]]", "A", true, false);
	}

	public void testUpperGroupCase3() throws Exception {
		assertMatch("[[:upper:]]", "H", true, false);
	}

	public void testUpperGroupCase4() throws Exception {
		// \u00c4 = 'A' with dots on it
		assertMatch("[[:upper:]]", "\u00c4", true, false);
	}

	public void testUpperGroupCase5() throws Exception {
		assertMatch("[[:upper:]]", ".", false, false);
	}

	public void testXDigitGroupCase0() throws Exception {
		assertMatch("[[:xdigit:]]", "a", true, false);
	}

	public void testXDigitGroupCase1() throws Exception {
		assertMatch("[[:xdigit:]]", "d", true, false);
	}

	public void testXDigitGroupCase2() throws Exception {
		assertMatch("[[:xdigit:]]", "f", true, false);
	}

	public void testXDigitGroupCase3() throws Exception {
		assertMatch("[[:xdigit:]]", "0", true, false);
	}

	public void testXDigitGroupCase4() throws Exception {
		assertMatch("[[:xdigit:]]", "5", true, false);
	}

	public void testXDigitGroupCase5() throws Exception {
		assertMatch("[[:xdigit:]]", "9", true, false);
	}

	public void testXDigitGroupCase6() throws Exception {
		assertMatch("[[:xdigit:]]", "۹", false, false);
	}

	public void testXDigitGroupCase7() throws Exception {
		assertMatch("[[:xdigit:]]", ".", false, false);
	}

	public void testWordroupCase0() throws Exception {
		assertMatch("[[:word:]]", "g", true, false);
	}

	public void testWordroupCase1() throws Exception {
		// \u00f6 = 'o' with dots on it
		assertMatch("[[:word:]]", "\u00f6", true, false);
	}

	public void testWordroupCase2() throws Exception {
		assertMatch("[[:word:]]", "5", true, false);
	}

	public void testWordroupCase3() throws Exception {
		assertMatch("[[:word:]]", "_", true, false);
	}

	public void testWordroupCase4() throws Exception {
		assertMatch("[[:word:]]", " ", false, false);
	}

	public void testWordroupCase5() throws Exception {
		assertMatch("[[:word:]]", ".", false, false);
	}

	public void testMixedGroupCase0() throws Exception {
		assertMatch("[A[:lower:]C3-5]", "A", true, false);
	}

	public void testMixedGroupCase1() throws Exception {
		assertMatch("[A[:lower:]C3-5]", "C", true, false);
	}

	public void testMixedGroupCase2() throws Exception {
		assertMatch("[A[:lower:]C3-5]", "e", true, false);
	}

	public void testMixedGroupCase3() throws Exception {
		assertMatch("[A[:lower:]C3-5]", "3", true, false);
	}

	public void testMixedGroupCase4() throws Exception {
		assertMatch("[A[:lower:]C3-5]", "4", true, false);
	}

	public void testMixedGroupCase5() throws Exception {
		assertMatch("[A[:lower:]C3-5]", "5", true, false);
	}

	public void testMixedGroupCase6() throws Exception {
		assertMatch("[A[:lower:]C3-5]", "B", false, false);
	}

	public void testMixedGroupCase7() throws Exception {
		assertMatch("[A[:lower:]C3-5]", "2", false, false);
	}

	public void testMixedGroupCase8() throws Exception {
		assertMatch("[A[:lower:]C3-5]", "6", false, false);
	}

	public void testMixedGroupCase9() throws Exception {
		assertMatch("[A[:lower:]C3-5]", ".", false, false);
	}

	public void testSpecialGroupCase0() throws Exception {
		assertMatch("[[]", "[", true, false);
	}

	public void testSpecialGroupCase1() throws Exception {
		assertMatch("[]]", "]", true, false);
	}

	public void testSpecialGroupCase2() throws Exception {
		assertMatch("[]a]", "]", true, false);
	}

	public void testSpecialGroupCase3() throws Exception {
		assertMatch("[a[]", "[", true, false);
	}

	public void testSpecialGroupCase4() throws Exception {
		assertMatch("[a[]", "a", true, false);
	}

	public void testSpecialGroupCase5() throws Exception {
		assertMatch("[!]]", "]", false, false);
	}

	public void testSpecialGroupCase6() throws Exception {
		assertMatch("[!]]", "x", true, false);
	}

	public void testSpecialGroupCase7() throws Exception {
		assertMatch("[:]]", ":]", true, false);
	}

	public void testSpecialGroupCase8() throws Exception {
		assertMatch("[:]]", ":", false, true);
	}

	public void testSpecialGroupCase9() throws Exception {
		try {
			assertMatch("[[:]", ":", true, true);
			fail("InvalidPatternException expected");
		} catch (InvalidPatternException e) {
			// expected
		}
	}

	public void testUnsupportedGroupCase0() throws Exception {
		try {
			assertMatch("[[=a=]]", "b", false, false);
			fail("InvalidPatternException expected");
		} catch (InvalidPatternException e) {
			assertTrue(e.getMessage().contains("[=a=]"));
		}
	}

	public void testUnsupportedGroupCase1() throws Exception {
		try {
			assertMatch("[[.a.]]", "b", false, false);
			fail("InvalidPatternException expected");
		} catch (InvalidPatternException e) {
			assertTrue(e.getMessage().contains("[.a.]"));
		}
	}

	public void testFilePathSimpleCase() throws Exception {
		assertFileNameMatch("a/b", "a/b", '/', true, false);
	}

	public void testFilePathCase0() throws Exception {
		assertFileNameMatch("a*b", "a/b", '/', false, false);
	}

	public void testFilePathCase1() throws Exception {
		assertFileNameMatch("a?b", "a/b", '/', false, false);
	}

	public void testFilePathCase2() throws Exception {
		assertFileNameMatch("a*b", "a\\b", '\\', false, false);
	}

	public void testFilePathCase3() throws Exception {
		assertFileNameMatch("a?b", "a\\b", '\\', false, false);
	}

	public void testReset() throws Exception {
		final String pattern = "helloworld";
		final FileNameMatcher matcher = new FileNameMatcher(pattern, null);
		matcher.append("helloworld");
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		matcher.reset();
		matcher.append("hello");
		assertEquals(false, matcher.isMatch());
		assertEquals(true, matcher.canAppendMatch());
		matcher.append("world");
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		matcher.append("to much");
		assertEquals(false, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		matcher.reset();
		matcher.append("helloworld");
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
	}

	public void testCreateMatcherForSuffix() throws Exception {
		final String pattern = "helloworld";
		final FileNameMatcher matcher = new FileNameMatcher(pattern, null);
		matcher.append("hello");
		final FileNameMatcher childMatcher = matcher.createMatcherForSuffix();
		assertEquals(false, matcher.isMatch());
		assertEquals(true, matcher.canAppendMatch());
		assertEquals(false, childMatcher.isMatch());
		assertEquals(true, childMatcher.canAppendMatch());
		matcher.append("world");
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		assertEquals(false, childMatcher.isMatch());
		assertEquals(true, childMatcher.canAppendMatch());
		childMatcher.append("world");
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		assertEquals(true, childMatcher.isMatch());
		assertEquals(false, childMatcher.canAppendMatch());
		childMatcher.reset();
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		assertEquals(false, childMatcher.isMatch());
		assertEquals(true, childMatcher.canAppendMatch());
		childMatcher.append("world");
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		assertEquals(true, childMatcher.isMatch());
		assertEquals(false, childMatcher.canAppendMatch());
	}

	public void testCopyConstructor() throws Exception {
		final String pattern = "helloworld";
		final FileNameMatcher matcher = new FileNameMatcher(pattern, null);
		matcher.append("hello");
		final FileNameMatcher copy = new FileNameMatcher(matcher);
		assertEquals(false, matcher.isMatch());
		assertEquals(true, matcher.canAppendMatch());
		assertEquals(false, copy.isMatch());
		assertEquals(true, copy.canAppendMatch());
		matcher.append("world");
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		assertEquals(false, copy.isMatch());
		assertEquals(true, copy.canAppendMatch());
		copy.append("world");
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		assertEquals(true, copy.isMatch());
		assertEquals(false, copy.canAppendMatch());
		copy.reset();
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		assertEquals(false, copy.isMatch());
		assertEquals(true, copy.canAppendMatch());
		copy.append("helloworld");
		assertEquals(true, matcher.isMatch());
		assertEquals(false, matcher.canAppendMatch());
		assertEquals(true, copy.isMatch());
		assertEquals(false, copy.canAppendMatch());
	}
#endif
    }
}
