/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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

using GitSharp.Diff;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class EditTest
    {
        [Test]
	    public void testCreate()
        {
		    Edit e = new Edit(1, 2, 3, 4);
		    Assert.AreEqual(1, e.getBeginA());
		    Assert.AreEqual(2, e.getEndA());
		    Assert.AreEqual(3, e.getBeginB());
		    Assert.AreEqual(4, e.getEndB());
	    }

        [Test]
	    public void testCreateEmpty()
        {
		    Edit e = new Edit(1, 3);
		    Assert.AreEqual(1, e.getBeginA());
		    Assert.AreEqual(1, e.getEndA());
		    Assert.AreEqual(3, e.getBeginB());
		    Assert.AreEqual(3, e.getEndB());
	    }

        [Test]
	    public void testSwap()
        {
		    Edit e = new Edit(1, 2, 3, 4);
		    e.swap();
		    Assert.AreEqual(3, e.getBeginA());
		    Assert.AreEqual(4, e.getEndA());
		    Assert.AreEqual(1, e.getBeginB());
		    Assert.AreEqual(2, e.getEndB());
	    }

        [Test]
	    public void testType_Insert()
        {
		    Edit e = new Edit(1, 1, 1, 2);
		    Assert.AreEqual(Edit.Type.INSERT, e.getType());
	    }

        [Test]
	    public void testType_Delete()
        {
		    Edit e = new Edit(1, 2, 1, 1);
            Assert.AreEqual(Edit.Type.DELETE, e.getType());
	    }

        [Test]
	    public void testType_Replace()
        {
		    Edit e = new Edit(1, 2, 1, 4);
            Assert.AreEqual(Edit.Type.REPLACE, e.getType());
	    }

        [Test]
	    public void testType_Empty() 
        {
            Assert.AreEqual(Edit.Type.EMPTY, new Edit(1, 1, 2, 2).getType());
		    Assert.AreEqual(Edit.Type.EMPTY, new Edit(1, 2).getType());
	    }

        [Test]
	    public void testToString()
        {
		    Edit e = new Edit(1, 2, 1, 4);
		    Assert.AreEqual("REPLACE(1-2,1-4)", e.toString());
	    }

        [Test]
	    public void testEquals1()
        {
		    Edit e1 = new Edit(1, 2, 3, 4);
		    Edit e2 = new Edit(1, 2, 3, 4);

		    Assert.IsTrue(e1.equals(e1));
		    Assert.IsTrue(e1.equals(e2));
		    Assert.IsTrue(e2.equals(e1));
		    Assert.AreEqual(e1.hashCode(), e2.hashCode());
		    Assert.IsFalse(e1.equals(""));
	    }

        [Test]
	    public void testNotEquals1()
        {
		    Assert.IsFalse(new Edit(1, 2, 3, 4).equals(new Edit(0, 2, 3, 4)));
	    }

        [Test]
	    public void testNotEquals2()
        {
		    Assert.IsFalse(new Edit(1, 2, 3, 4).equals(new Edit(1, 0, 3, 4)));
	    }

        [Test]
	    public void testNotEquals3()
        {
		    Assert.IsFalse(new Edit(1, 2, 3, 4).equals(new Edit(1, 2, 0, 4)));
	    }

        [Test]
	    public void testNotEquals4()
        {
		    Assert.IsFalse(new Edit(1, 2, 3, 4).equals(new Edit(1, 2, 3, 0)));
	    }

        [Test]
	    public void testExtendA()
        {
		    Edit e = new Edit(1, 2, 1, 1);

		    e.extendA();
		    Assert.AreEqual(new Edit(1, 3, 1, 1), e);

		    e.extendA();
		    Assert.AreEqual(new Edit(1, 4, 1, 1), e);
	    }

        [Test]
	    public void testExtendB()
        {
		    Edit e = new Edit(1, 2, 1, 1);

		    e.extendB();
		    Assert.AreEqual(new Edit(1, 2, 1, 2), e);

		    e.extendB();
		    Assert.AreEqual(new Edit(1, 2, 1, 3), e);
	    }
    }
}