using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Gitty.Lib.CSharp.Tests
{
    [TestFixture]
    public class ObjectIdMapTests
    {
        [Test]
        public void EnumeratorTest()
        {
            var map = new ObjectIdMap<string>
                          {
                              {ObjectId.FromString("003ae55c8f6f23aaee66acd2e1c35523fa6ddc33"),
                                  "003ae55c8f6f23aaee66acd2e1c35523fa6ddc33"},
                              {ObjectId.FromString("0129a76cb3bf83f137d3c2afdc019b48358990b4"),
                                  "0129a76cb3bf83f137d3c2afdc019b48358990b4"},
                              {ObjectId.FromString("015d23e47e60c0935b24759201cc7e0131393ee9"),
                                  "015d23e47e60c0935b24759201cc7e0131393ee9"},
                              {ObjectId.FromString("019e88399a970068ee9e8fefca4c57fc3a814035"),
                                  "019e88399a970068ee9e8fefca4c57fc3a814035"}
                          };

            var enumerator = map.GetEnumerator();
            Assert.AreEqual(true, enumerator.MoveNext(), "EnumeratorTest#010");
            Assert.AreEqual("003ae55c8f6f23aaee66acd2e1c35523fa6ddc33", enumerator.Current.Value, "EnumeratorTest#020");
            Assert.AreEqual(true, enumerator.MoveNext(), "EnumeratorTest#030");
            Assert.AreEqual("0129a76cb3bf83f137d3c2afdc019b48358990b4", enumerator.Current.Value, "EnumeratorTest#040");
            Assert.AreEqual(true, enumerator.MoveNext(), "EnumeratorTest#050");
            Assert.AreEqual("015d23e47e60c0935b24759201cc7e0131393ee9", enumerator.Current.Value, "EnumeratorTest#060");
            Assert.AreEqual(true, enumerator.MoveNext(), "EnumeratorTest#070");
            Assert.AreEqual("019e88399a970068ee9e8fefca4c57fc3a814035", enumerator.Current.Value, "EnumeratorTest#080");
            Assert.AreEqual(false, enumerator.MoveNext(), "EnumeratorTest#090");
        }
    }
}
