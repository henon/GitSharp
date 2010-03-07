using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Util
{
	[TestFixture]
	public class PathUtilsTest
	{
		[Test]
		public void RelativePath()
		{
			Assert.AreEqual(Join("foo","bar","baz"), PathUtil.RelativePath(@"\foo\bar\baz", @"\foo\bar\baz\foo\bar\baz"));
			Assert.AreEqual(Join("foo", "bar", "baz"), PathUtil.RelativePath(@"/foo/bar/baz", @"/foo/bar/baz/foo/bar/\//\baz"));
			Assert.AreEqual(Join("..",".."), PathUtil.RelativePath(@"\foo\bar\baz", @"\foo"));
			Assert.AreEqual("path", PathUtil.RelativePath(@"foo/bar/baz", @"path"));
			Assert.AreEqual(Join("..", ".."), PathUtil.RelativePath(@"\foo\bar\baz", @"../.."));
			Assert.AreEqual("hmm.txt", PathUtil.RelativePath(@"\foo\bar\baz", @"hmm.txt"));
			Assert.AreEqual("", PathUtil.RelativePath(@"\foo\bar\baz", @"/foo/bar\baz"));
			Assert.AreEqual(Join("","foo","bar","baz"), PathUtil.RelativePath(@"foo\bar\baz", @"/foo/bar\baz"));
		}

		private string Join(params string[] parts)
		{
			return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
		}
	}
}
