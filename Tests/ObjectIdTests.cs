/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using Xunit;

namespace GitSharp.Tests
{
	public class ObjectIdTests : XunitBaseFact
	{
		const string def4c620b = "def4c620bc3713bb1bb26b808ec9312548e73946";
		const string ff00eedd0 = "ff00eedd003713bb1bb26b808ec9312548e73946";

		[Fact]
		public void ObjectIdToStringTest()
		{
			var id = ObjectId.FromString("003ae55c8f6f23aaee66acd2e1c35523fa6ddc33");
			Assert.Equal("003ae55c8f6f23aaee66acd2e1c35523fa6ddc33", id.ToString());
			Assert.Equal(0, id.GetFirstByte());
		}

		[Fact]
		public void GetFirstByteTest()
		{
			for (var i = 0; i < 255; i++)
			{
				var iInHex = i.ToString("x").PadLeft(2, '0');
				foreach (var j in new[] { 0x0, 0x1, 0xffffff })
				{
					var firstFourBytes = iInHex + j.ToString("x").PadLeft(6, '0');
					var id = ObjectId.FromString(firstFourBytes + "00000000000000000000000000000000");
					Assert.Equal(i, id.GetFirstByte());
				}
			}
		}

		[Fact]
		public void test001_toString()
		{
			ObjectId oid = ObjectId.FromString(def4c620b);
			Assert.Equal(def4c620b, oid.ToString());
		}

		[Fact]
		public void test002_toString()
		{
			ObjectId oid = ObjectId.FromString(ff00eedd0);
			Assert.Equal(ff00eedd0, oid.ToString());
		}

		[Fact]
		public void test003_equals()
		{
			ObjectId a = ObjectId.FromString(def4c620b);
			ObjectId b = ObjectId.FromString(def4c620b);
			Assert.Equal(a.GetHashCode(), b.GetHashCode());
			Assert.True(a.Equals(b), "a and b are same");
		}

		[Fact]
		public void test004_isId()
		{
			Assert.True(ObjectId.IsId(def4c620b), "valid id");
		}

		[Fact]
		public void test005_notIsId()
		{
			Assert.False(ObjectId.IsId("bob"), "bob is not an id");
		}

		[Fact]
		public void test006_notIsId()
		{
			Assert.False(ObjectId.IsId("def4c620bc3713bb1bb26b808ec9312548e7394"), "39 digits is not an id");
		}

		[Fact]
		public void test007_notIsId()
		{
			Assert.False(ObjectId.IsId("Def4c620bc3713bb1bb26b808ec9312548e73946"), "uppercase is not accepted");
		}

		[Fact]
		public void test008_notIsId()
		{
			Assert.False(ObjectId.IsId("gef4c620bc3713bb1bb26b808ec9312548e73946"), "g is not a valid hex digit");
		}

		[Fact]
		public void test009_toString()
		{
			ObjectId oid = ObjectId.FromString(ff00eedd0);
			Assert.Equal(ff00eedd0, ObjectId.ToString(oid));
		}

		[Fact]
		public void test010_toString()
		{
			Assert.Equal(ObjectId.ZeroId.ToString(), ObjectId.ToString(null));
		}
	}
}
