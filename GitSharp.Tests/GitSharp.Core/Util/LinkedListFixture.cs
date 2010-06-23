using System;
using System.Collections.Generic;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Util
{
    [TestFixture]
    public class LinkedListFixture
    {
        [Test]
        public void EmptyList()
        {
            var list = new LinkedList<int>();
            var iter = new LinkedListIterator<int>(list);

            AssertEndOfListHasBeenReached(iter);
            AssertHelper.Throws<IndexOutOfRangeException>(iter.remove);
        }

        [Test]
        public void FilledList_NavigateForward()
        {
            var list = new LinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);

            var iter = new LinkedListIterator<int>(list);

            Assert.AreEqual(3, list.Count);

            Assert.IsTrue(iter.hasNext());
            Assert.AreEqual(1, iter.next());
            Assert.IsTrue(iter.hasNext());
            Assert.AreEqual(2, iter.next());
            Assert.IsTrue(iter.hasNext());
            Assert.AreEqual(3, iter.next());

            Assert.AreEqual(3, list.Count);

            AssertEndOfListHasBeenReached(iter);
        }

        [Test]
        public void FilledList_NavigateForwardAndRemoval()
        {
            var list = new LinkedList<int>();
            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);
            var iter = new LinkedListIterator<int>(list);

            Assert.AreEqual(3, list.Count);

            Assert.IsTrue(iter.hasNext());
            Assert.AreEqual(1, iter.next());
            iter.remove();
            Assert.IsTrue(iter.hasNext());
            Assert.AreEqual(2, iter.next());
            Assert.IsTrue(iter.hasNext());
            Assert.AreEqual(3, iter.next());

            Assert.AreEqual(2, list.Count);

            AssertEndOfListHasBeenReached(iter);
        }

        private static void AssertEndOfListHasBeenReached(LinkedListIterator<int> iter)
        {
            Assert.IsFalse(iter.hasNext());
            AssertHelper.Throws<IndexOutOfRangeException>(() => iter.next());
        }
    }
}
