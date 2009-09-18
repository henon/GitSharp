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

using System.Collections;
using GitSharp.Diff;
using Xunit;

namespace GitSharp.Tests.Diff
{
	public class EditListTest
	{
		[StrictFactAttribute]
		public void testEmpty()
		{
			var l = new EditList();
			Assert.Equal(0, l.size());
			Assert.True(l.isEmpty());
			Assert.Equal("EditList[]", l.ToString());

			Assert.True(l.Equals(l));
			Assert.True(l.Equals(new EditList()));
			Assert.False(l.Equals(string.Empty));
			Assert.Equal(l.GetHashCode(), new EditList().GetHashCode());
		}

		[StrictFactAttribute]
		public void testAddOne()
		{
			var e = new Edit(1, 2, 1, 1);
			var l = new EditList { e };
			Assert.Equal(1, l.size());
			Assert.False(l.isEmpty());
			Assert.Same(e, l.get(0));
			IEnumerator i = l.GetEnumerator();
			i.Reset();
			i.MoveNext();
			Assert.Same(e, i.Current);

			Assert.True(l.Equals(l));
			Assert.False(l.Equals(new EditList()));

			var l2 = new EditList { e };
			Assert.True(l.Equals(l2));
			Assert.True(l2.Equals(l));
			Assert.Equal(l.GetHashCode(), l2.GetHashCode());
		}

		[StrictFactAttribute]
		public void testAddTwo()
		{
			var e1 = new Edit(1, 2, 1, 1);
			var e2 = new Edit(8, 8, 8, 12);
			var l = new EditList { e1, e2 };
			Assert.Equal(2, l.size());
			Assert.Same(e1, l.get(0));
			Assert.Same(e2, l.get(1));

			IEnumerator i = l.GetEnumerator();
			i.Reset();
			i.MoveNext();
			Assert.Same(e1, i.Current);
			i.MoveNext();
			Assert.Same(e2, i.Current);

			Assert.True(l.Equals(l));
			Assert.False(l.Equals(new EditList()));

			var l2 = new EditList { e1, e2 };
			Assert.True(l.Equals(l2));
			Assert.True(l2.Equals(l));
			Assert.Equal(l.GetHashCode(), l2.GetHashCode());
		}

		[StrictFactAttribute]
		public void testSet()
		{
			var e1 = new Edit(1, 2, 1, 1);
			var e2 = new Edit(3, 4, 3, 3);
			var l = new EditList { e1 };
			Assert.Same(e1, l.get(0));
			Assert.Same(e1, l.set(0, e2));
			Assert.Same(e2, l.get(0));
		}

		[StrictFactAttribute]
		public void testRemove()
		{
			var e1 = new Edit(1, 2, 1, 1);
			var e2 = new Edit(8, 8, 8, 12);
			var l = new EditList { e1, e2 };
			l.Remove(e1);
			Assert.Equal(1, l.size());
			Assert.Same(e2, l.get(0));
		}
	}
}