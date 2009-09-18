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

using System.Text;
using GitSharp.Exceptions;
using Xunit;

namespace GitSharp.Tests
{
	public class ObjectCheckerTests : XunitBaseFact
	{
		#region Setup/Teardown

		protected override void SetUp()
		{
			_checker = new ObjectChecker();
		}

		#endregion

		private ObjectChecker _checker;

		private static void Entry(StringBuilder b, string modeName)
		{
			b.Append(modeName);
			b.Append('\0');
			for (int i = 0; i < Constants.OBJECT_ID_LENGTH; i++)
			{
				b.Append((char) i);
			}
		}

		[StrictFactAttribute]
		public void testCheckBlob()
		{
			// Any blob should pass...
			_checker.checkBlob(new byte[0]);
			_checker.checkBlob(new byte[1]);

			_checker.check(ObjectType.Blob, new byte[0]);
			_checker.check(ObjectType.Blob, new byte[1]);
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidAuthor1()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <foo 1 +0000\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("invalid author", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidAuthor2()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor foo> 1 +0000\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("invalid author", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidAuthor3()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author 1 +0000\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("invalid author", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidAuthor4()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b> +0000\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("invalid author", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidAuthor5()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b>\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("invalid author", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidAuthor6()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b> z");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("invalid author", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidAuthor7()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b> 1 z");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("invalid author", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidCommitter()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author a <b> 1 +0000\n");
			b.Append("committer a <");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("invalid committer", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidParent1()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent ");
			b.Append("\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid parent", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidParent2()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent ");
			b.Append("zzzzfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid parent", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidParent3()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent  ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid parent", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidParent4()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent  ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("z\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid parent", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidParent5()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("parent\t");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("no author", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidTree1()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("zzzzfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid tree", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidTree2()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append("z\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid tree", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidTree3()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9b");
			b.Append("\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid tree", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitInvalidTree4()
		{
			var b = new StringBuilder();

			b.Append("tree  ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid tree", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitNoAuthor()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("no author", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitNoCommitter1()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> 1 +0000\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("no committer", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitNoCommitter2()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
			b.Append("\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				// Yes, really, we complain about author not being
				// found as the invalid parent line wasn't consumed.
				Assert.Equal("no committer", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitNoTree1()
		{
			var b = new StringBuilder();

			b.Append("parent ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tree header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitNoTree2()
		{
			var b = new StringBuilder();

			b.Append("trie ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tree header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitNoTree3()
		{
			var b = new StringBuilder();

			b.Append("tree");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tree header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidCommitNoTree4()
		{
			var b = new StringBuilder();

			b.Append("tree\t");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkCommit(data);
				Assert.False(true, "Did not catch corrupt object");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tree header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagInvalidTaggerHeader1()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag foo\n");
			b.Append("tagger \n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid tagger", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagInvalidTaggerHeader3()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag foo\n");
			b.Append("tagger a < 1 +000\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid tagger", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoObject1()
		{
			var b = new StringBuilder();

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no object header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoObject2()
		{
			var b = new StringBuilder();

			b.Append("object\t");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no object header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoObject3()
		{
			var b = new StringBuilder();

			b.Append("obejct ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no object header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoObject4()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("zz9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid object", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoObject5()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append(" \n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid object", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoObject6()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid object", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoTaggerHeader1()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag foo\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tagger header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoTagHeader1()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tag header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoTagHeader2()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag\tfoo\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tag header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoTagHeader3()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tga foo\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tag header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoTagHeader4()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag foo");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tagger header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoType1()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no type header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoType2()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type\tcommit\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no type header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoType3()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("tpye commit\n");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no type header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTagNoType4()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit");

			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTag(data);
				Assert.False(true, "incorrectly accepted invalid tag");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("no tag header", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeBadSorting1()
		{
			var b = new StringBuilder();
			Entry(b, "100644 foobar");
			Entry(b, "100644 fooaaa");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("incorrectly sorted", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeBadSorting2()
		{
			var b = new StringBuilder();
			Entry(b, "40000 a");
			Entry(b, "100644 a.c");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("incorrectly sorted", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeBadSorting3()
		{
			var b = new StringBuilder();
			Entry(b, "100644 a0c");
			Entry(b, "40000 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("incorrectly sorted", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeDuplicateNames1()
		{
			var b = new StringBuilder();
			Entry(b, "100644 a");
			Entry(b, "100644 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("duplicate entry names", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeDuplicateNames2()
		{
			var b = new StringBuilder();
			Entry(b, "100644 a");
			Entry(b, "100755 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("duplicate entry names", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeDuplicateNames3()
		{
			var b = new StringBuilder();
			Entry(b, "100644 a");
			Entry(b, "40000 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("duplicate entry names", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeDuplicateNames4()
		{
			var b = new StringBuilder();
			Entry(b, "100644 a");
			Entry(b, "100644 a.c");
			Entry(b, "100644 a.d");
			Entry(b, "100644 a.e");
			Entry(b, "40000 a");
			Entry(b, "100644 zoo");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("duplicate entry names", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeModeMissingName()
		{
			var b = new StringBuilder();
			b.Append("100644");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("truncated in mode", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeModeNotOctal1()
		{
			var b = new StringBuilder();
			Entry(b, "8 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid mode character", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeModeNotOctal2()
		{
			var b = new StringBuilder();
			Entry(b, "Z a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid mode character", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeModeNotSupportedMode1()
		{
			var b = new StringBuilder();
			Entry(b, "1 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid mode 1", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeModeNotSupportedMode2()
		{
			var b = new StringBuilder();
			Entry(b, "170000 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid mode " + 0170000, e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeModeStartsWithZero1()
		{
			var b = new StringBuilder();
			Entry(b, "0 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("mode starts with '0'", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeModeStartsWithZero2()
		{
			var b = new StringBuilder();
			Entry(b, "0100644 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("mode starts with '0'", e.Message);
			}
		}


		[StrictFactAttribute]
		public void testInvalidTreeModeStartsWithZero3()
		{
			var b = new StringBuilder();
			Entry(b, "040000 a");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("mode starts with '0'", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeNameContainsSlash()
		{
			var b = new StringBuilder();
			Entry(b, "100644 a/b");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("name contains '/'", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeNameIsDot()
		{
			var b = new StringBuilder();
			Entry(b, "100644 .");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid name '.'", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeNameIsDotDot()
		{
			var b = new StringBuilder();
			Entry(b, "100644 ..");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("invalid name '..'", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeNameIsEmpty()
		{
			var b = new StringBuilder();
			Entry(b, "100644 ");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("zero length name", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeTruncatedInName()
		{
			var b = new StringBuilder();
			b.Append("100644 b");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("truncated in name", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidTreeTruncatedInObjectId()
		{
			var b = new StringBuilder();
			b.Append("100644 b\012");
			char[] data = b.ToString().ToCharArray();
			try
			{
				_checker.checkTree(data);
				Assert.False(true, "incorrectly accepted an invalid tree");
			}
			catch (CorruptObjectException e)
			{
				Assert.Equal("truncated in object id", e.Message);
			}
		}

		[StrictFactAttribute]
		public void testInvalidType()
		{
			try
			{
				_checker.check(ObjectType.Bad, new char[0]);
				Assert.False(true, "Did not throw CorruptObjectException");
			}
			catch (CorruptObjectException e)
			{
				string m = e.Message;
				Assert.Equal("Invalid object type: " + ObjectType.Bad, m);
			}
		}

		[StrictFactAttribute]
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

			char[] data = b.ToString().ToCharArray();
			_checker.checkCommit(data);
			_checker.check(ObjectType.Commit, data);
		}

		[StrictFactAttribute]
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

			char[] data = b.ToString().ToCharArray();
			_checker.checkCommit(data);
			_checker.check(ObjectType.Commit, data);
		}

		[StrictFactAttribute]
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

			char[] data = b.ToString().ToCharArray();
			_checker.checkCommit(data);
			_checker.check(ObjectType.Commit, data);
		}

		[StrictFactAttribute]
		public void testValidCommitBlankAuthor()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author <> 0 +0000\n");
			b.Append("committer <> 0 +0000\n");

			char[] data = b.ToString().ToCharArray();
			_checker.checkCommit(data);
			_checker.check(ObjectType.Commit, data);
		}

		[StrictFactAttribute]
		public void testValidCommitNoParent()
		{
			var b = new StringBuilder();

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> 1 +0000\n");
			b.Append("committer A. U. Thor <author@localhost> 1 +0000\n");

			char[] data = b.ToString().ToCharArray();
			_checker.checkCommit(data);
			_checker.check(ObjectType.Commit, data);
		}

		[StrictFactAttribute]
		public void testValidCommitNormalTime()
		{
			var b = new StringBuilder();
			string when = "1222757360 -0730";

			b.Append("tree ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("author A. U. Thor <author@localhost> " + when + "\n");
			b.Append("committer A. U. Thor <author@localhost> " + when + "\n");

			char[] data = b.ToString().ToCharArray();
			_checker.checkCommit(data);
			_checker.check(ObjectType.Commit, data);
		}

		[StrictFactAttribute]
		public void testValidEmptyTree()
		{
			_checker.checkTree(new char[0]);
			_checker.check(ObjectType.Tree, new char[0]);
		}

		[StrictFactAttribute]
		public void testValidTag()
		{
			var b = new StringBuilder();

			b.Append("object ");
			b.Append("be9bfa841874ccc9f2ef7c48d0c76226f89b7189");
			b.Append('\n');

			b.Append("type commit\n");
			b.Append("tag test-tag\n");
			b.Append("tagger A. U. Thor <author@localhost> 1 +0000\n");

			char[] data = b.ToString().ToCharArray();
			_checker.checkTag(data);
			_checker.check(ObjectType.Tag, data);
		}

		[StrictFactAttribute]
		public void testValidTree1()
		{
			var b = new StringBuilder();
			Entry(b, "100644 regular-file");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTree2()
		{
			var b = new StringBuilder();
			Entry(b, "100755 executable");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTree3()
		{
			var b = new StringBuilder();
			Entry(b, "40000 tree");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTree4()
		{
			var b = new StringBuilder();
			Entry(b, "120000 symlink");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTree5()
		{
			var b = new StringBuilder();
			Entry(b, "160000 git link");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTree6()
		{
			var b = new StringBuilder();
			Entry(b, "100644 .a");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTreeSorting1()
		{
			var b = new StringBuilder();
			Entry(b, "100644 fooaaa");
			Entry(b, "100755 foobar");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTreeSorting2()
		{
			var b = new StringBuilder();
			Entry(b, "100755 fooaaa");
			Entry(b, "100644 foobar");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTreeSorting3()
		{
			var b = new StringBuilder();
			Entry(b, "40000 a");
			Entry(b, "100644 b");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTreeSorting4()
		{
			var b = new StringBuilder();
			Entry(b, "100644 a");
			Entry(b, "40000 b");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTreeSorting5()
		{
			var b = new StringBuilder();
			Entry(b, "100644 a.c");
			Entry(b, "40000 a");
			Entry(b, "100644 a0c");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTreeSorting6()
		{
			var b = new StringBuilder();
			Entry(b, "40000 a");
			Entry(b, "100644 apple");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTreeSorting7()
		{
			var b = new StringBuilder();
			Entry(b, "40000 an orang");
			Entry(b, "40000 an orange");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}

		[StrictFactAttribute]
		public void testValidTreeSorting8()
		{
			var b = new StringBuilder();
			Entry(b, "100644 a");
			Entry(b, "100644 a0c");
			Entry(b, "100644 b");
			char[] data = b.ToString().ToCharArray();
			_checker.checkTree(data);
		}
	}
}