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
using GitSharp.Exceptions;
using GitSharp.FnMatch;
using Xunit;

namespace GitSharp.Tests.FileNameMatch
{
    public class FileNameMatcherTest
    {
        private void assertMatch(string pattern, string input, bool matchExpected, bool appendCanMatchExpected)
        {
            FileNameMatcher matcher = new FileNameMatcher(pattern, null);
            matcher.Append(input);
            Assert.Equal(matchExpected, matcher.IsMatch());
            Assert.Equal(appendCanMatchExpected, matcher.CanAppendMatch());
        }

        private void assertFileNameMatch(string pattern, string input, char excludedCharacter, bool matchExpected, bool appendCanMatchExpected)
        {
            FileNameMatcher matcher = new FileNameMatcher(pattern, excludedCharacter);
            matcher.Append(input);
            Assert.Equal(matchExpected, matcher.IsMatch());
            Assert.Equal(appendCanMatchExpected, matcher.CanAppendMatch());
        }

        [Fact]
        public virtual void testVerySimplePatternCase0()
        {
            assertMatch("", "", true, false);
        }

        [Fact]
        public virtual void testVerySimplePatternCase1()
        {
            assertMatch("ab", "a", false, true);
        }

        [Fact]
        public virtual void testVerySimplePatternCase2()
        {
            assertMatch("ab", "ab", true, false);
        }

        [Fact]
        public virtual void testVerySimplePatternCase3()
        {
            assertMatch("ab", "ac", false, false);
        }

        [Fact]
        public virtual void testVerySimplePatternCase4()
        {
            assertMatch("ab", "abc", false, false);
        }

        [Fact]
        public virtual void testVerySimpleWirdcardCase0()
        {
            assertMatch("?", "a", true, false);
        }

        [Fact]
        public virtual void testVerySimpleWildCardCase1()
        {
            assertMatch("??", "a", false, true);
        }

        [Fact]
        public virtual void testVerySimpleWildCardCase2()
        {
            assertMatch("??", "ab", true, false);
        }

        [Fact]
        public virtual void testVerySimpleWildCardCase3()
        {
            assertMatch("??", "abc", false, false);
        }

        [Fact]
        public virtual void testVerySimpleStarCase0()
        {
            assertMatch("*", "", true, true);
        }

        [Fact]
        public virtual void testVerySimpleStarCase1()
        {
            assertMatch("*", "a", true, true);
        }

        [Fact]
        public virtual void testVerySimpleStarCase2()
        {
            assertMatch("*", "ab", true, true);
        }

        [Fact]
        public virtual void testSimpleStarCase0()
        {
            assertMatch("a*b", "a", false, true);
        }

        [Fact]
        public virtual void testSimpleStarCase1()
        {
            assertMatch("a*c", "ac", true, true);
        }

        [Fact]
        public virtual void testSimpleStarCase2()
        {
            assertMatch("a*c", "ab", false, true);
        }

        [Fact]
        public virtual void testSimpleStarCase3()
        {
            assertMatch("a*c", "abc", true, true);
        }

        [Fact]
        public virtual void testManySolutionsCase0()
        {
            assertMatch("a*a*a", "aaa", true, true);
        }

        [Fact]
        public virtual void testManySolutionsCase1()
        {
            assertMatch("a*a*a", "aaaa", true, true);
        }

        [Fact]
        public virtual void testManySolutionsCase2()
        {
            assertMatch("a*a*a", "ababa", true, true);
        }

        [Fact]
        public virtual void testManySolutionsCase3()
        {
            assertMatch("a*a*a", "aaaaaaaa", true, true);
        }

        [Fact]
        public virtual void testManySolutionsCase4()
        {
            assertMatch("a*a*a", "aaaaaaab", false, true);
        }

        [Fact]
        public virtual void testVerySimpleGroupCase0()
        {
            assertMatch("[ab]", "a", true, false);
        }

        [Fact]
        public virtual void testVerySimpleGroupCase1()
        {
            assertMatch("[ab]", "b", true, false);
        }

        [Fact]
        public virtual void testVerySimpleGroupCase2()
        {
            assertMatch("[ab]", "ab", false, false);
        }

        [Fact]
        public virtual void testVerySimpleGroupRangeCase0()
        {
            assertMatch("[b-d]", "a", false, false);
        }

        [Fact]
        public virtual void testVerySimpleGroupRangeCase1()
        {
            assertMatch("[b-d]", "b", true, false);
        }

        [Fact]
        public virtual void testVerySimpleGroupRangeCase2()
        {
            assertMatch("[b-d]", "c", true, false);
        }

        [Fact]
        public virtual void testVerySimpleGroupRangeCase3()
        {
            assertMatch("[b-d]", "d", true, false);
        }

        [Fact]
        public virtual void testVerySimpleGroupRangeCase4()
        {
            assertMatch("[b-d]", "e", false, false);
        }

        [Fact]
        public virtual void testVerySimpleGroupRangeCase5()
        {
            assertMatch("[b-d]", "-", false, false);
        }

        [Fact]
        public virtual void testTwoGroupsCase0()
        {
            assertMatch("[b-d][ab]", "bb", true, false);
        }

        [Fact]
        public virtual void testTwoGroupsCase1()
        {
            assertMatch("[b-d][ab]", "ca", true, false);
        }

        [Fact]
        public virtual void testTwoGroupsCase2()
        {
            assertMatch("[b-d][ab]", "fa", false, false);
        }

        [Fact]
        public virtual void testTwoGroupsCase3()
        {
            assertMatch("[b-d][ab]", "bc", false, false);
        }

        [Fact]
        public virtual void testTwoRangesInOneGroupCase0()
        {
            assertMatch("[b-ce-e]", "a", false, false);
        }

        [Fact]
        public virtual void testTwoRangesInOneGroupCase1()
        {
            assertMatch("[b-ce-e]", "b", true, false);
        }

        [Fact]
        public virtual void testTwoRangesInOneGroupCase2()
        {
            assertMatch("[b-ce-e]", "c", true, false);
        }

        [Fact]
        public virtual void testTwoRangesInOneGroupCase3()
        {
            assertMatch("[b-ce-e]", "d", false, false);
        }

        [Fact]
        public virtual void testTwoRangesInOneGroupCase4()
        {
            assertMatch("[b-ce-e]", "e", true, false);
        }

        [Fact]
        public virtual void testTwoRangesInOneGroupCase5()
        {
            assertMatch("[b-ce-e]", "f", false, false);
        }

        [Fact]
        public virtual void testIncompleteRangesInOneGroupCase0()
        {
            assertMatch("a[b-]", "ab", true, false);
        }

        [Fact]
        public virtual void testIncompleteRangesInOneGroupCase1()
        {
            assertMatch("a[b-]", "ac", false, false);
        }

        [Fact]
        public virtual void testIncompleteRangesInOneGroupCase2()
        {
            assertMatch("a[b-]", "a-", true, false);
        }

        [Fact]
        public virtual void testCombinedRangesInOneGroupCase0()
        {
            assertMatch("[a-c-e]", "b", true, false);
        }

        ///	<summary>
        /// The c belongs to the range a-c. "-e" is no valid range so d should not 	match.
        ///	</summary>
        ///	<exception cref="Exception">for some reasons </exception>
        [Fact]
        public virtual void testCombinedRangesInOneGroupCase1()
        {
            assertMatch("[a-c-e]", "d", false, false);
        }

        [Fact]
        public virtual void testCombinedRangesInOneGroupCase2()
        {
            assertMatch("[a-c-e]", "e", true, false);
        }

        [Fact]
        public virtual void testInversedGroupCase0()
        {
            assertMatch("[!b-c]", "a", true, false);
        }

        [Fact]
        public virtual void testInversedGroupCase1()
        {
            assertMatch("[!b-c]", "b", false, false);
        }

        [Fact]
        public virtual void testInversedGroupCase2()
        {
            assertMatch("[!b-c]", "c", false, false);
        }

        [Fact]
        public virtual void testInversedGroupCase3()
        {
            assertMatch("[!b-c]", "d", true, false);
        }

        [Fact]
        public virtual void testAlphaGroupCase0()
        {
            assertMatch("[[:alpha:]]", "d", true, false);
        }

        [Fact]
        public virtual void testAlphaGroupCase1()
        {
            assertMatch("[[:alpha:]]", ":", false, false);
        }

        [Fact]
        public virtual void testAlphaGroupCase2()
        {
            // \u00f6 = 'o' with dots on it
            assertMatch("[[:alpha:]]", "\u00f6", true, false);
        }

        [Fact]
        public virtual void test2AlphaGroupsCase0()
        {
            // \u00f6 = 'o' with dots on it
            assertMatch("[[:alpha:]][[:alpha:]]", "a\u00f6", true, false);
            assertMatch("[[:alpha:]][[:alpha:]]", "a1", false, false);
        }

        [Fact]
        public virtual void testAlnumGroupCase0()
        {
            assertMatch("[[:alnum:]]", "a", true, false);
        }

        [Fact]
        public virtual void testAlnumGroupCase1()
        {
            assertMatch("[[:alnum:]]", "1", true, false);
        }

        [Fact]
        public virtual void testAlnumGroupCase2()
        {
            assertMatch("[[:alnum:]]", ":", false, false);
        }

        [Fact]
        public virtual void testBlankGroupCase0()
        {
            assertMatch("[[:blank:]]", " ", true, false);
        }

        [Fact]
        public virtual void testBlankGroupCase1()
        {
            assertMatch("[[:blank:]]", "\t", true, false);
        }

        [Fact]
        public virtual void testBlankGroupCase2()
        {
            assertMatch("[[:blank:]]", "\r", false, false);
        }

        [Fact]
        public virtual void testBlankGroupCase3()
        {
            assertMatch("[[:blank:]]", "\n", false, false);
        }

        [Fact]
        public virtual void testBlankGroupCase4()
        {
            assertMatch("[[:blank:]]", "a", false, false);
        }

        [Fact]
        public virtual void testCntrlGroupCase0()
        {
            assertMatch("[[:cntrl:]]", "a", false, false);
        }

        [Fact]
        public virtual void testCntrlGroupCase1()
        {
            assertMatch("[[:cntrl:]]", Convert.ToString((char)7), true, false);
        }

        [Fact]
        public virtual void testDigitGroupCase0()
        {
            assertMatch("[[:digit:]]", "0", true, false);
        }

        [Fact]
        public virtual void testDigitGroupCase1()
        {
            assertMatch("[[:digit:]]", "5", true, false);
        }

        [Fact]
        public virtual void testDigitGroupCase2()
        {
            assertMatch("[[:digit:]]", "9", true, false);
        }

        [Fact]
        public virtual void testDigitGroupCase3()
        {
            // \u06f9 = EXTENDED ARABIC-INDIC DIGIT NINE
            assertMatch("[[:digit:]]", "\u06f9", true, false);
        }

        [Fact]
        public virtual void testDigitGroupCase4()
        {
            assertMatch("[[:digit:]]", "a", false, false);
        }

        [Fact]
        public virtual void testDigitGroupCase5()
        {
            assertMatch("[[:digit:]]", "]", false, false);
        }

        [Fact]
        public virtual void testGraphGroupCase0()
        {
            assertMatch("[[:graph:]]", "]", true, false);
        }

        [Fact]
        public virtual void testGraphGroupCase1()
        {
            assertMatch("[[:graph:]]", "a", true, false);
        }

        [Fact]
        public virtual void testGraphGroupCase2()
        {
            assertMatch("[[:graph:]]", ".", true, false);
        }

        [Fact]
        public virtual void testGraphGroupCase3()
        {
            assertMatch("[[:graph:]]", "0", true, false);
        }

        [Fact]
        public virtual void testGraphGroupCase4()
        {
            assertMatch("[[:graph:]]", " ", false, false);
        }

        [Fact]
        public virtual void testGraphGroupCase5()
        {
            // \u00f6 = 'o' with dots on it
            assertMatch("[[:graph:]]", "\u00f6", true, false);
        }

        [Fact]
        public virtual void testLowerGroupCase0()
        {
            assertMatch("[[:lower:]]", "a", true, false);
        }

        [Fact]
        public virtual void testLowerGroupCase1()
        {
            assertMatch("[[:lower:]]", "h", true, false);
        }

        [Fact]
        public virtual void testLowerGroupCase2()
        {
            assertMatch("[[:lower:]]", "A", false, false);
        }

        [Fact]
        public virtual void testLowerGroupCase3()
        {
            assertMatch("[[:lower:]]", "H", false, false);
        }

        [Fact]
        public virtual void testLowerGroupCase4()
        {
            // \u00e4 = small 'a' with dots on it
            assertMatch("[[:lower:]]", "\u00e4", true, false);
        }

        [Fact]
        public virtual void testLowerGroupCase5()
        {
            assertMatch("[[:lower:]]", ".", false, false);
        }

        [Fact]
        public virtual void testPrintGroupCase0()
        {
            assertMatch("[[:print:]]", "]", true, false);
        }

        [Fact]
        public virtual void testPrintGroupCase1()
        {
            assertMatch("[[:print:]]", "a", true, false);
        }

        [Fact]
        public virtual void testPrintGroupCase2()
        {
            assertMatch("[[:print:]]", ".", true, false);
        }

        [Fact]
        public virtual void testPrintGroupCase3()
        {
            assertMatch("[[:print:]]", "0", true, false);
        }

        [Fact]
        public virtual void testPrintGroupCase4()
        {
            assertMatch("[[:print:]]", " ", true, false);
        }

        [Fact]
        public virtual void testPrintGroupCase5()
        {
            // \u00f6 = 'o' with dots on it
            assertMatch("[[:print:]]", "\u00f6", true, false);
        }

        [Fact]
        public virtual void testPunctGroupCase0()
        {
            assertMatch("[[:punct:]]", ".", true, false);
        }

        [Fact]
        public virtual void testPunctGroupCase1()
        {
            assertMatch("[[:punct:]]", "@", true, false);
        }

        [Fact]
        public virtual void testPunctGroupCase2()
        {
            assertMatch("[[:punct:]]", " ", false, false);
        }

        [Fact]
        public virtual void testPunctGroupCase3()
        {
            assertMatch("[[:punct:]]", "a", false, false);
        }

        [Fact]
        public virtual void testSpaceGroupCase0()
        {
            assertMatch("[[:space:]]", " ", true, false);
        }

        [Fact]
        public virtual void testSpaceGroupCase1()
        {
            assertMatch("[[:space:]]", "\t", true, false);
        }

        [Fact]
        public virtual void testSpaceGroupCase2()
        {
            assertMatch("[[:space:]]", "\r", true, false);
        }

        [Fact]
        public virtual void testSpaceGroupCase3()
        {
            assertMatch("[[:space:]]", "\n", true, false);
        }

        [Fact]
        public virtual void testSpaceGroupCase4()
        {
            assertMatch("[[:space:]]", "a", false, false);
        }

        [Fact]
        public virtual void testUpperGroupCase0()
        {
            assertMatch("[[:upper:]]", "a", false, false);
        }

        [Fact]
        public virtual void testUpperGroupCase1()
        {
            assertMatch("[[:upper:]]", "h", false, false);
        }

        [Fact]
        public virtual void testUpperGroupCase2()
        {
            assertMatch("[[:upper:]]", "A", true, false);
        }

        [Fact]
        public virtual void testUpperGroupCase3()
        {
            assertMatch("[[:upper:]]", "H", true, false);
        }

        [Fact]
        public virtual void testUpperGroupCase4()
        {
            // \u00c4 = 'A' with dots on it
            assertMatch("[[:upper:]]", "\u00c4", true, false);
        }

        [Fact]
        public virtual void testUpperGroupCase5()
        {
            assertMatch("[[:upper:]]", ".", false, false);
        }

        [Fact]
        public virtual void testXDigitGroupCase0()
        {
            assertMatch("[[:xdigit:]]", "a", true, false);
        }

        [Fact]
        public virtual void testXDigitGroupCase1()
        {
            assertMatch("[[:xdigit:]]", "d", true, false);
        }

        [Fact]
        public virtual void testXDigitGroupCase2()
        {
            assertMatch("[[:xdigit:]]", "f", true, false);
        }

        [Fact]
        public virtual void testXDigitGroupCase3()
        {
            assertMatch("[[:xdigit:]]", "0", true, false);
        }

        [Fact]
        public virtual void testXDigitGroupCase4()
        {
            assertMatch("[[:xdigit:]]", "5", true, false);
        }

        [Fact]
        public virtual void testXDigitGroupCase5()
        {
            assertMatch("[[:xdigit:]]", "9", true, false);
        }

        [Fact]
        public virtual void testXDigitGroupCase6()
        {
            assertMatch("[[:xdigit:]]", "۹", false, false);
        }

        [Fact]
        public virtual void testXDigitGroupCase7()
        {
            assertMatch("[[:xdigit:]]", ".", false, false);
        }

        [Fact]
        public virtual void testWordroupCase0()
        {
            assertMatch("[[:word:]]", "g", true, false);
        }

        [Fact]
        public virtual void testWordroupCase1()
        {
            // \u00f6 = 'o' with dots on it
            assertMatch("[[:word:]]", "\u00f6", true, false);
        }

        [Fact]
        public virtual void testWordroupCase2()
        {
            assertMatch("[[:word:]]", "5", true, false);
        }

        [Fact]
        public virtual void testWordroupCase3()
        {
            assertMatch("[[:word:]]", "_", true, false);
        }

        [Fact]
        public virtual void testWordroupCase4()
        {
            assertMatch("[[:word:]]", " ", false, false);
        }

        [Fact]
        public virtual void testWordroupCase5()
        {
            assertMatch("[[:word:]]", ".", false, false);
        }

        [Fact]
        public virtual void testMixedGroupCase0()
        {
            assertMatch("[A[:lower:]C3-5]", "A", true, false);
        }

        [Fact]
        public virtual void testMixedGroupCase1()
        {
            assertMatch("[A[:lower:]C3-5]", "C", true, false);
        }

        [Fact]
        public virtual void testMixedGroupCase2()
        {
            assertMatch("[A[:lower:]C3-5]", "e", true, false);
        }

        [Fact]
        public virtual void testMixedGroupCase3()
        {
            assertMatch("[A[:lower:]C3-5]", "3", true, false);
        }

        [Fact]
        public virtual void testMixedGroupCase4()
        {
            assertMatch("[A[:lower:]C3-5]", "4", true, false);
        }

        [Fact]
        public virtual void testMixedGroupCase5()
        {
            assertMatch("[A[:lower:]C3-5]", "5", true, false);
        }

        [Fact]
        public virtual void testMixedGroupCase6()
        {
            assertMatch("[A[:lower:]C3-5]", "B", false, false);
        }

        [Fact]
        public virtual void testMixedGroupCase7()
        {
            assertMatch("[A[:lower:]C3-5]", "2", false, false);
        }

        [Fact]
        public virtual void testMixedGroupCase8()
        {
            assertMatch("[A[:lower:]C3-5]", "6", false, false);
        }

        [Fact]
        public virtual void testMixedGroupCase9()
        {
            assertMatch("[A[:lower:]C3-5]", ".", false, false);
        }

        [Fact]
        public virtual void testSpecialGroupCase0()
        {
            assertMatch("[[]", "[", true, false);
        }

        [Fact]
        public virtual void testSpecialGroupCase1()
        {
            assertMatch("[]]", "]", true, false);
        }

        [Fact]
        public virtual void testSpecialGroupCase2()
        {
            assertMatch("[]a]", "]", true, false);
        }

        [Fact]
        public virtual void testSpecialGroupCase3()
        {
            assertMatch("[a[]", "[", true, false);
        }

        [Fact]
        public virtual void testSpecialGroupCase4()
        {
            assertMatch("[a[]", "a", true, false);
        }

        [Fact]
        public virtual void testSpecialGroupCase5()
        {
            assertMatch("[!]]", "]", false, false);
        }

        [Fact]
        public virtual void testSpecialGroupCase6()
        {
            assertMatch("[!]]", "x", true, false);
        }

        [Fact]
        public virtual void testSpecialGroupCase7()
        {
            assertMatch("[:]]", ":]", true, false);
        }

        [Fact]
        public virtual void testSpecialGroupCase8()
        {
            assertMatch("[:]]", ":", false, true);
        }

        [Fact]
        public virtual void testSpecialGroupCase9()
        {
        	Assert.Throws<NoClosingBracketException>(() => assertMatch("[[:]", ":", true, true));
        }

        [Fact]
        public virtual void testUnsupportedGroupCase0()
        {
            try
            {
                assertMatch("[[=a=]]", "b", false, false);
            }
            catch (InvalidPatternException e)
            {
                Assert.True(e.Message.Contains("[=a=]"));
            }
        }

        [Fact]
        public virtual void testUnsupportedGroupCase1()
        {
            try
            {
                assertMatch("[[.a.]]", "b", false, false);
            }
            catch (InvalidPatternException e)
            {
                Assert.True(e.Message.Contains("[.a.]"));
            }
        }

        [Fact]
        public virtual void testFilePathSimpleCase()
        {
            assertFileNameMatch("a/b", "a/b", '/', true, false);
        }

        [Fact]
        public virtual void testFilePathCase0()
        {
            assertFileNameMatch("a*b", "a/b", '/', false, false);
        }

        [Fact]
        public virtual void testFilePathCase1()
        {
            assertFileNameMatch("a?b", "a/b", '/', false, false);
        }

        [Fact]
        public virtual void testFilePathCase2()
        {
            assertFileNameMatch("a*b", "a\\b", '\\', false, false);
        }

        [Fact]
        public virtual void testFilePathCase3()
        {
            assertFileNameMatch("a?b", "a\\b", '\\', false, false);
        }

        [Fact]
        public virtual void testReset()
        {
            const string pattern = "helloworld";

            FileNameMatcher matcher = new FileNameMatcher(pattern, null);
            matcher.Append("helloworld");
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            matcher.Reset();
            matcher.Append("hello");
            Assert.Equal(false, matcher.IsMatch());
            Assert.Equal(true, matcher.CanAppendMatch());
            matcher.Append("world");
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            matcher.Append("to much");
            Assert.Equal(false, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            matcher.Reset();
            matcher.Append("helloworld");
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
        }

        [Fact]
        public virtual void testCreateMatcherForSuffix()
        {
            const string pattern = "helloworld";

            FileNameMatcher matcher = new FileNameMatcher(pattern, null);
            matcher.Append("hello");

            FileNameMatcher childMatcher = matcher.CreateMatcherForSuffix();
            Assert.Equal(false, matcher.IsMatch());
            Assert.Equal(true, matcher.CanAppendMatch());
            Assert.Equal(false, childMatcher.IsMatch());
            Assert.Equal(true, childMatcher.CanAppendMatch());
            matcher.Append("world");
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            Assert.Equal(false, childMatcher.IsMatch());
            Assert.Equal(true, childMatcher.CanAppendMatch());
            childMatcher.Append("world");
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            Assert.Equal(true, childMatcher.IsMatch());
            Assert.Equal(false, childMatcher.CanAppendMatch());
            childMatcher.Reset();
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            Assert.Equal(false, childMatcher.IsMatch());
            Assert.Equal(true, childMatcher.CanAppendMatch());
            childMatcher.Append("world");
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            Assert.Equal(true, childMatcher.IsMatch());
            Assert.Equal(false, childMatcher.CanAppendMatch());
        }

        [Fact]
        public virtual void testCopyConstructor()
        {
            const string pattern = "helloworld";

            FileNameMatcher matcher = new FileNameMatcher(pattern, null);
            matcher.Append("hello");

            FileNameMatcher copy = new FileNameMatcher(matcher);
            Assert.Equal(false, matcher.IsMatch());
            Assert.Equal(true, matcher.CanAppendMatch());
            Assert.Equal(false, copy.IsMatch());
            Assert.Equal(true, copy.CanAppendMatch());
            matcher.Append("world");
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            Assert.Equal(false, copy.IsMatch());
            Assert.Equal(true, copy.CanAppendMatch());
            copy.Append("world");
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            Assert.Equal(true, copy.IsMatch());
            Assert.Equal(false, copy.CanAppendMatch());
            copy.Reset();
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            Assert.Equal(false, copy.IsMatch());
            Assert.Equal(true, copy.CanAppendMatch());
            copy.Append("helloworld");
            Assert.Equal(true, matcher.IsMatch());
            Assert.Equal(false, matcher.CanAppendMatch());
            Assert.Equal(true, copy.IsMatch());
            Assert.Equal(false, copy.CanAppendMatch());
        }
    }
}