/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Google Inc.
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

using System;
using System.Text;
using GitSharp.Core;
using GitSharp.Core.Exceptions;
using NUnit.Framework;

namespace GitSharp.Core.Tests
{
	[TestFixture]
	public class ObjectCheckerTests
	{
		#region Setup/Teardown

		[SetUp]
		public void setUp()
		{
			_checker = new ObjectChecker();
		}

		#endregion

		private ObjectChecker _checker;

		private static void entry(StringBuilder b, string modeName)
		{
			b.Append(modeName);
			b.Append('\0');
			for (int i = 0; i < Constants.OBJECT_ID_LENGTH; i++)
			{
				b.Append((char) i);
			}
		}

		[Test]
		public void testCheckBlob()
		{
			// Any blob should pass...
			_checker.checkBlob(new byte[0]);
			_checker.checkBlob(new byte[1]);

			_checker.check(Constants.OBJ_BLOB, new byte[0]);
			_checker.check(Constants.OBJ_BLOB, new byte[1]);
		}

		[Test]
		public void testInvalidCommitInvalidAuthor1()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <foo 1 +0000\n");

            byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("invalid author", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidAuthor2()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor foo> 1 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("invalid author", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidAuthor3()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author 1 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("invalid author", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidAuthor4()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b> +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("invalid author", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidAuthor5()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b>\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("invalid author", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidAuthor6()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b> z");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("invalid author", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidAuthor7()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b> 1 z");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("invalid author", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidCommitter()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b> 1 +0000\n");
			b.Append("committer a <");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("invalid committer", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidParent1()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent ");
			b.Append("\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid parent", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidParent2()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent ");
			b.Append("zzzzfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid parent", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidParent3()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent  ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid parent", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidParent4()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent  ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("z\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid parent", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidParent5()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent\t");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("no author", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidTree1()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("zzzzfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid tree", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidTree2()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("z\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid tree", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidTree3()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9b");
			b.Append("\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid tree", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitInvalidTree4()
		{
			var b = new StringBuilder();

			b.Append("tree  ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid tree", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitNoAuthor()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("no author", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitNoCommitter1()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> 1 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("no committer", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitNoCommitter2()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
			b.Append("\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.AreEqual("no committer", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitNoTree1()
		{
			var b = new StringBuilder();

			b.Append("parent ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no tree header", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitNoTree2()
		{
			var b = new StringBuilder();

			b.Append("trie ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no tree header", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitNoTree3()
		{
			var b = new StringBuilder();

			b.Append("tree");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no tree header", e.Message);
			}
		}

		[Test]
		public void testInvalidCommitNoTree4()
		{
			var b = new StringBuilder();

			b.Append("tree\t");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkCommit(data);
				Assert.Fail("Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no tree header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagInvalidTaggerHeader1()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag foo\n");
			b.Append("tagger \n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid tagger", e.Message);
			}
		}

		[Test]
		public void testInvalidTagInvalidTaggerHeader3()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag foo\n");
			b.Append("tagger a < 1 +000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid tagger", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoObject1()
		{
			var b = new StringBuilder();

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no object header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoObject2()
		{
			var b = new StringBuilder();

			b.Append("object\t");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no object header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoObject3()
		{
			var b = new StringBuilder();

			b.Append("obejct ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no object header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoObject4()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("zz9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid object", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoObject5()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append(" \n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid object", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoObject6()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid object", e.Message);
			}
		}

		[Test]
		public void testValidTagHasNoTaggerHeader()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag foo\n");

            _checker.checkTag(Constants.encodeASCII(b.ToString()));
		}

		[Test]
		public void testInvalidTagNoTagHeader1()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no tag header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoTagHeader2()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag\tfoo\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no tag header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoTagHeader3()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tga foo\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no tag header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoType1()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no type header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoType2()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type\tcommit\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no type header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoType3()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("tpye commit\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no type header", e.Message);
			}
		}

		[Test]
		public void testInvalidTagNoType4()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit");

			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTag(data);
				Assert.Fail("incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("no tag header", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeBadSorting1()
		{
			var b = new StringBuilder();
			entry(b, "100644 foobar");
			entry(b, "100644 fooaaa");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("incorrectly sorted", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeBadSorting2()
		{
			var b = new StringBuilder();
			entry(b, "40000 a");
			entry(b, "100644 a.c");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("incorrectly sorted", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeBadSorting3()
		{
			var b = new StringBuilder();
			entry(b, "100644 a0c");
			entry(b, "40000 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("incorrectly sorted", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeDuplicateNames1()
		{
			var b = new StringBuilder();
			entry(b, "100644 a");
			entry(b, "100644 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("duplicate entry names", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeDuplicateNames2()
		{
			var b = new StringBuilder();
			entry(b, "100644 a");
			entry(b, "100755 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("duplicate entry names", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeDuplicateNames3()
		{
			var b = new StringBuilder();
			entry(b, "100644 a");
			entry(b, "40000 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("duplicate entry names", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeDuplicateNames4()
		{
			var b = new StringBuilder();
			entry(b, "100644 a");
			entry(b, "100644 a.c");
			entry(b, "100644 a.d");
			entry(b, "100644 a.e");
			entry(b, "40000 a");
			entry(b, "100644 zoo");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("duplicate entry names", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeModeMissingName()
		{
			var b = new StringBuilder();
			b.Append("100644");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("truncated in mode", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeModeNotOctal1()
		{
			var b = new StringBuilder();
			entry(b, "8 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid mode character", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeModeNotOctal2()
		{
			var b = new StringBuilder();
			entry(b, "Z a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid mode character", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeModeNotSupportedMode1()
		{
			var b = new StringBuilder();
			entry(b, "1 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid mode 1", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeModeNotSupportedMode2()
		{
			var b = new StringBuilder();
			entry(b, "170000 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid mode " + 0170000, e.Message);
			}
		}

		[Test]
		public void testInvalidTreeModeStartsWithZero1()
		{
			var b = new StringBuilder();
			entry(b, "0 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("mode starts with '0'", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeModeStartsWithZero2()
		{
			var b = new StringBuilder();
			entry(b, "0100644 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("mode starts with '0'", e.Message);
			}
		}


		[Test]
		public void testInvalidTreeModeStartsWithZero3()
		{
			var b = new StringBuilder();
			entry(b, "040000 a");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("mode starts with '0'", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeNameContainsSlash()
		{
			var b = new StringBuilder();
			entry(b, "100644 a/b");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("name contains '/'", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeNameIsDot()
		{
			var b = new StringBuilder();
			entry(b, "100644 .");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid name '.'", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeNameIsDotDot()
		{
			var b = new StringBuilder();
			entry(b, "100644 ..");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("invalid name '..'", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeNameIsEmpty()
		{
			var b = new StringBuilder();
			entry(b, "100644 ");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("zero length name", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeTruncatedInName()
		{
			var b = new StringBuilder();
			b.Append("100644 b");
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("truncated in name", e.Message);
			}
		}

		[Test]
		public void testInvalidTreeTruncatedInObjectId()
		{
			var b = new StringBuilder();
			b.Append("100644 b" + '\0' + (char)1 + (char)2);
			byte[] data = Constants.encodeASCII(b.ToString());
			try
			{
				_checker.checkTree(data);
				Assert.Fail("incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.AreEqual("truncated in object id", e.Message);
			}
		}

		[Test]
		public void testInvalidType()
		{
			try
			{
				_checker.check(Constants.OBJ_BAD, new byte[0]);
				Assert.Fail("Did not throw CorruptObjectException");
			}
			catch (CorruptObjectException e)
			{
				string m = e.Message;
				Assert.AreEqual("Invalid object type: " + Constants.OBJ_BAD, m);
			}
		}

		[Test]
		public void testValidCommit128Parent()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			for (int i = 0; i < 128; i++)
			{
				b.Append("parent ");
				b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
				b.Append('\n');
			}

			b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
			b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkCommit(data);
			_checker.check(Constants.OBJ_COMMIT, data);
		}

		[Test]
		public void testValidCommit1Parent()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
			b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkCommit(data);
			_checker.check(Constants.OBJ_COMMIT, data);
		}

		[Test]
		public void testValidCommit2Parent()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
			b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkCommit(data);
			_checker.check(Constants.OBJ_COMMIT, data);
		}

		[Test]
		public void testValidCommitBlankAuthor()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author <> 0 +0000\n");
			b.Append("committer <> 0 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkCommit(data);
			_checker.check(Constants.OBJ_COMMIT, data);
		}

		[Test]
		public void testValidCommitNoParent()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
			b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkCommit(data);
			_checker.check(Constants.OBJ_COMMIT, data);
		}

		[Test]
		public void testValidCommitNormalTime()
		{
			var b = new StringBuilder();
			string when = "1222757360 -0730";

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> " + when + "\n");
			b.Append("committer A. U. Thor <author@localhost> " + when + "\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkCommit(data);
			_checker.check(Constants.OBJ_COMMIT, data);
		}

		[Test]
		public void testValidEmptyTree()
		{
			_checker.checkTree(new byte[0]);
			_checker.check(Constants.OBJ_TREE, new byte[0]);
		}

		[Test]
		public void testValidTag()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag test-tag\n");
			b.Append("tagger A. U. Thor <author@localhost> 1 +0000\n");

			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTag(data);
			_checker.check(Constants.OBJ_TAG, data);
		}

		[Test]
		public void testValidTree1()
		{
			var b = new StringBuilder();
			entry(b, "100644 regular-file");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTree2()
		{
			var b = new StringBuilder();
			entry(b, "100755 executable");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTree3()
		{
			var b = new StringBuilder();
			entry(b, "40000 tree");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTree4()
		{
			var b = new StringBuilder();
			entry(b, "120000 symlink");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTree5()
		{
			var b = new StringBuilder();
			entry(b, "160000 git link");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTree6()
		{
			var b = new StringBuilder();
			entry(b, "100644 .a");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTreeSorting1()
		{
			var b = new StringBuilder();
			entry(b, "100644 fooaaa");
			entry(b, "100755 foobar");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTreeSorting2()
		{
			var b = new StringBuilder();
			entry(b, "100755 fooaaa");
			entry(b, "100644 foobar");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTreeSorting3()
		{
			var b = new StringBuilder();
			entry(b, "40000 a");
			entry(b, "100644 b");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTreeSorting4()
		{
			var b = new StringBuilder();
			entry(b, "100644 a");
			entry(b, "40000 b");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTreeSorting5()
		{
			var b = new StringBuilder();
			entry(b, "100644 a.c");
			entry(b, "40000 a");
			entry(b, "100644 a0c");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTreeSorting6()
		{
			var b = new StringBuilder();
			entry(b, "40000 a");
			entry(b, "100644 apple");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTreeSorting7()
		{
			var b = new StringBuilder();
			entry(b, "40000 an orang");
			entry(b, "40000 an orange");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}

		[Test]
		public void testValidTreeSorting8()
		{
			var b = new StringBuilder();
			entry(b, "100644 a");
			entry(b, "100644 a0c");
			entry(b, "100644 b");
			byte[] data = Constants.encodeASCII(b.ToString());
			_checker.checkTree(data);
		}
	}
}