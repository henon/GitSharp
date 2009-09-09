/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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

using System.IO;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class TreeIteratorPreOrderTest : RepositoryTestCase
    {
		/// <summary>
		/// Empty tree
		/// </summary>
		[Test]
		public void testEmpty()
		{
			Tree tree = new Tree(db);
			TreeIterator i = MakeIterator(tree);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual(string.Empty, i.Current.FullName);
			Assert.IsFalse(i.MoveNext());
		}

		///	<summary>
		/// one file
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testSimpleF1()
		{
			Tree tree = new Tree(db);
			tree.AddFile("x");
			TreeIterator i = MakeIterator(tree);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual(string.Empty, i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("x", i.Current.Name);
			Assert.IsFalse(i.MoveNext());
		}

		///	<summary>
		/// two files
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testSimpleF2()
		{
			Tree tree = new Tree(db);
			tree.AddFile("a");
			tree.AddFile("x");
			TreeIterator i = MakeIterator(tree);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual(string.Empty, i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a", i.Current.Name);
			Assert.AreEqual("x", i.Current.Name);
			Assert.IsFalse(i.MoveNext());
		}

		///	<summary>
		/// Empty tree
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testSimpleT()
		{
			Tree tree = new Tree(db);
			tree.AddTree("a");
			TreeIterator i = MakeIterator(tree);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual(string.Empty, i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a", i.Current.FullName);
			Assert.IsFalse(i.MoveNext());
		}

		[Test]
		public void testTricky()
		{
			Tree tree = new Tree(db);
			tree.AddFile("a.b");
			tree.AddFile("a.c");
			tree.AddFile("a/b.b/b");
			tree.AddFile("a/b");
			tree.AddFile("a/c");
			tree.AddFile("a=c");
			tree.AddFile("a=d");

			TreeIterator i = MakeIterator(tree);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual(string.Empty, i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a.b", i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a.c", i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a", i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a/b", i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a/b.b", i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a/b.b/b", i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a/c", i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a=c", i.Current.FullName);
			Assert.IsTrue(i.MoveNext());
			Assert.AreEqual("a=d", i.Current.FullName);
			Assert.IsFalse(i.MoveNext());
		}

		private static TreeIterator MakeIterator(Tree tree)
		{
			return new TreeIterator(tree, TreeIterator.Order.PREORDER);
		}
    }
}
