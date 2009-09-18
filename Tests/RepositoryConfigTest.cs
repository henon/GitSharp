/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GitSharp.Tests
{
	public class RepositoryConfigTest
	{
		[StrictFactAttribute]
		public void ReadBareKey()
		{
			Config c = Parse("[foo]\nbar\n");
			Assert.Equal(true, c.getBoolean("foo", null, "bar", false));
			Assert.Equal(string.Empty, c.getString("foo", null, "bar"));
		}

		[StrictFactAttribute]
		public void ReadWithSubsection()
		{
			Config c = Parse("[foo \"zip\"]\nbar\n[foo \"zap\"]\nbar=false\nn=3\n");
			Assert.Equal(true, c.getBoolean("foo", "zip", "bar", false));
			Assert.Equal(string.Empty, c.getString("foo", "zip", "bar"));
			Assert.Equal(false, c.getBoolean("foo", "zap", "bar", true));
			Assert.Equal("false", c.getString("foo", "zap", "bar"));
			Assert.Equal(3, c.getInt("foo", "zap", "n", 4));
			Assert.Equal(4, c.getInt("foo", "zap", "m", 4));
		}

		[StrictFactAttribute]
		public void PutRemote()
		{
			var c = new Config();
			c.setString("sec", "ext", "name", "value");
			c.setString("sec", "ext", "name2", "value2");
			const string expText = "[sec \"ext\"]\n\tname = value\n\tname2 = value2\n";
			Assert.Equal(expText, c.toText());
		}

		[StrictFactAttribute]
		public void PutGetSimple()
		{
			var c = new Config();
			c.setString("my", null, "somename", "false");
			Assert.Equal("false", c.getString("my", null, "somename"));
			Assert.Equal("[my]\n\tsomename = false\n", c.toText());
		}

		[StrictFactAttribute]
		public void PutGetStringList()
		{
			var c = new Config();
			var values = new List<string> { "value1", "value2" };
			c.SetStringList("my", null, "somename", values);

			object[] expArr = values.ToArray();
			string[] actArr = c.getStringList("my", null, "somename");
			Assert.True(expArr.SequenceEqual(actArr));

			const string expText = "[my]\n\tsomename = value1\n\tsomename = value2\n";
			Assert.Equal(expText, c.toText());
		}

		[StrictFactAttribute]
		public void ReadCaseInsensitive()
		{
			Config c = Parse("[Foo]\nBar\n");
			Assert.Equal(true, c.getBoolean("foo", null, "bar", false));
			Assert.Equal(string.Empty, c.getString("foo", null, "bar"));
		}

		[StrictFactAttribute]
		public void ReadBooleanTrueFalse1()
		{
			Config c = Parse("[s]\na = true\nb = false\n");
			Assert.Equal("true", c.getString("s", null, "a"));
			Assert.Equal("false", c.getString("s", null, "b"));

			Assert.True(c.getBoolean("s", "a", false));
			Assert.False(c.getBoolean("s", "b", true));
		}

		[StrictFactAttribute]
		public void ReadLong()
		{
			AssertReadLong(1L);
			AssertReadLong(-1L);
			AssertReadLong(long.MinValue);
			AssertReadLong(long.MaxValue);
			AssertReadLong(4L * 1024 * 1024 * 1024, "4g");
			AssertReadLong(3L * 1024 * 1024, "3 m");
			AssertReadLong(8L * 1024, "8 k");

			try
			{
				AssertReadLong(-1, "1.5g");
				Assert.False(true, "incorrectly accepted 1.5g");
			}
			catch (ArgumentException e)
			{
				Assert.Equal("Invalid long value: s.a=1.5g", e.Message);
			}
		}

		private static void AssertReadLong(long exp)
		{
			AssertReadLong(exp, exp.ToString());
		}

		private static void AssertReadLong(long exp, string act)
		{
			Config c = Parse("[s]\na = " + act + "\n");
			Assert.Equal(exp, c.getLong("s", null, "a", 0L));
		}

		private static Config Parse(string content)
		{
			var c = new Config(null);
			c.fromText(content);
			return c;
		}
	}
}