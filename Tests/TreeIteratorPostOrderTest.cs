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

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests
{
    [TestFixture]
    public class TreeIteratorPostOrderTest : RepositoryTestCase
    {
#if false

	/** Empty tree */
	public void testEmpty() {
		Tree tree = new Tree(db);
		TreeIterator i = makeIterator(tree);
		assertTrue(i.hasNext());
		assertEquals("", i.next().getFullName());
		assertFalse(i.hasNext());
	}

	/**
	 * one file
	 * 
	 * @throws IOException
	 */
	public void testSimpleF1() throws IOException {
		Tree tree = new Tree(db);
		tree.addFile("x");
		TreeIterator i = makeIterator(tree);
		assertTrue(i.hasNext());
		assertEquals("x", i.next().getName());
		assertTrue(i.hasNext());
		assertEquals("", i.next().getFullName());
		assertFalse(i.hasNext());
	}

	/**
	 * two files
	 * 
	 * @throws IOException
	 */
	public void testSimpleF2() throws IOException {
		Tree tree = new Tree(db);
		tree.addFile("a");
		tree.addFile("x");
		TreeIterator i = makeIterator(tree);
		assertTrue(i.hasNext());
		assertEquals("a", i.next().getName());
		assertEquals("x", i.next().getName());
		assertTrue(i.hasNext());
		assertEquals("", i.next().getFullName());
		assertFalse(i.hasNext());
	}

	/**
	 * Empty tree
	 * 
	 * @throws IOException
	 */
	public void testSimpleT() throws IOException {
		Tree tree = new Tree(db);
		tree.addTree("a");
		TreeIterator i = makeIterator(tree);
		assertTrue(i.hasNext());
		assertEquals("a", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("", i.next().getFullName());
		assertFalse(i.hasNext());
	}
	
	public void testTricky() throws IOException {
		Tree tree = new Tree(db);
		tree.addFile("a.b");
		tree.addFile("a.c");
		tree.addFile("a/b.b/b");
		tree.addFile("a/b");
		tree.addFile("a/c");
		tree.addFile("a=c");
		tree.addFile("a=d");

		TreeIterator i = makeIterator(tree);
		assertTrue(i.hasNext());
		assertEquals("a.b", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("a.c", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("a/b", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("a/b.b/b", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("a/b.b", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("a/c", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("a", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("a=c", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("a=d", i.next().getFullName());
		assertTrue(i.hasNext());
		assertEquals("", i.next().getFullName());
		assertFalse(i.hasNext());
	}

	private TreeIterator makeIterator(Tree tree) {
		return new TreeIterator(tree, TreeIterator.Order.POSTORDER);
	}
#endif
    }
}
