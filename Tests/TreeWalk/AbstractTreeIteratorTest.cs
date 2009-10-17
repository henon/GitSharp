/*
 * Copyright (C) 2009, Tor Arne Vestbø <torarnv@gmail.com>
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
using GitSharp.Core;
using GitSharp.Core.TreeWalk;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
	[TestFixture]
	public class AbstractTreeIteratorTest
	{
	    private static string prefix(string path)
	    {
	        int s = path.LastIndexOf('/');
	        return s > 0 ? path.Slice(0, s) : "";
	    }
	
	    public class FakeTreeIterator : WorkingTreeIterator
	    {
	        public FakeTreeIterator(string pathName, FileMode fileMode) : base(prefix(pathName))
	        {
	            Mode = fileMode.Bits;
	
	            int s = pathName.LastIndexOf('/');
	            byte[] name = Constants.encode(pathName.Substring(s + 1));
	            ensurePathCapacity(PathOffset + name.Length, PathOffset);
	            Array.Copy(name, 0, Path, PathOffset, name.Length);
	            PathLen = PathOffset + name.Length;
	        }
	
	        public new void ensurePathCapacity(int capacity, int length)
	        {
	            base.ensurePathCapacity(capacity, length);
	        }
	
	        public override AbstractTreeIterator createSubtreeIterator(Repository repo)
	        {
	            return null;
	        }
	    }
	
	    [Test]
	    public void testPathCompare()
	    {
			Assert.IsTrue(new FakeTreeIterator("a", FileMode.RegularFile).pathCompare(
					new FakeTreeIterator("a", FileMode.Tree)) < 0);
	
			Assert.IsTrue(new FakeTreeIterator("a", FileMode.Tree).pathCompare(
					new FakeTreeIterator("a", FileMode.RegularFile)) > 0);
	
			Assert.IsTrue(new FakeTreeIterator("a", FileMode.RegularFile).pathCompare(
					new FakeTreeIterator("a", FileMode.RegularFile)) == 0);
	
			Assert.IsTrue(new FakeTreeIterator("a", FileMode.Tree).pathCompare(
					new FakeTreeIterator("a", FileMode.Tree)) == 0);
		}
	
	    [Test]
	    public void testGrowPath()
	    {
			FakeTreeIterator i = new FakeTreeIterator("ab", FileMode.Tree);
			byte[] origpath = i.Path;
			Assert.AreEqual(i.Path[0], 'a');
			Assert.AreEqual(i.Path[1], 'b');
	
			i.growPath(2);
	
	        Assert.AreNotSame(origpath, i.Path);
	        Assert.AreEqual(origpath.Length * 2, i.Path.Length);
	        Assert.AreEqual(i.Path[0], 'a');
	        Assert.AreEqual(i.Path[1], 'b');
		}
	
	    [Test]
	    public void testEnsurePathCapacityFastCase()
	    {
			FakeTreeIterator i = new FakeTreeIterator("ab", FileMode.Tree);
			int want = 50;
	        byte[] origpath = i.Path;
	        Assert.AreEqual(i.Path[0], 'a');
	        Assert.AreEqual(i.Path[1], 'b');
	        Assert.IsTrue(want < i.Path.Length);
	
			i.ensurePathCapacity(want, 2);
	
	        Assert.AreSame(origpath, i.Path);
	        Assert.AreEqual(i.Path[0], 'a');
	        Assert.AreEqual(i.Path[1], 'b');
		}
	
	    [Test]
	    public void testEnsurePathCapacityGrows()
	    {
			FakeTreeIterator i = new FakeTreeIterator("ab", FileMode.Tree);
			int want = 384;
	        byte[] origpath = i.Path;
	        Assert.AreEqual(i.Path[0], 'a');
	        Assert.AreEqual(i.Path[1], 'b');
	        Assert.IsTrue(i.Path.Length < want);
	
			i.ensurePathCapacity(want, 2);
	
	        Assert.AreNotSame(origpath, i.Path);
	        Assert.AreEqual(512, i.Path.Length);
	        Assert.AreEqual(i.Path[0], 'a');
	        Assert.AreEqual(i.Path[1], 'b');
		}
	
	    [Test]
	    public void testEntryFileMode()
	    {
			foreach (FileMode m in new [] { FileMode.Tree,
					FileMode.RegularFile, FileMode.ExecutableFile,
					FileMode.GitLink, FileMode.Symlink }) {
				FakeTreeIterator i = new FakeTreeIterator("a", m);
				Assert.AreEqual(m.Bits, i.EntryRawMode);
				Assert.AreSame(m, i.EntryFileMode);
			}
		}
	
	    [Test]
	    public void testEntryPath()
	    {
			FakeTreeIterator i = new FakeTreeIterator("a/b/cd", FileMode.Tree);
			Assert.AreEqual("a/b/cd", i.EntryPathString);
			Assert.AreEqual(2, i.NameLength);
			byte[] b = new byte[3];
			b[0] = 0x0a;
			i.getName(b, 1);
			Assert.AreEqual(0x0a, b[0]);
			Assert.AreEqual('c', b[1]);
			Assert.AreEqual('d', b[2]);
		}
	
	    [Test]
		public void testCreateEmptyTreeIterator() {
			FakeTreeIterator i = new FakeTreeIterator("a/b/cd", FileMode.Tree);
			EmptyTreeIterator e = i.createEmptyTreeIterator();
			Assert.IsNotNull(e);
			Assert.AreEqual(i.EntryPathString + "/", e.EntryPathString);
		}
	}
}
