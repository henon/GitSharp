/*
 * Copyright (C) 2009, Google Inc.
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

using GitSharp.DirectoryCache;
using GitSharp.RevWalk;
using GitSharp.Tests.Util;
using GitSharp.TreeWalk.Filter;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests.RevWalk
{
	public abstract class RevWalkTestCase : RepositoryTestCase
	{
		private ObjectWriter _ow;
		protected RevTree EmptyTree;
		private long _nowTick; // [henon] this are seconds in git internal time representaiton
		protected GitSharp.RevWalk.RevWalk Rw;

		protected override void SetUp()
		{
			base.SetUp();
			_ow = new ObjectWriter(db);
			Rw = CreateRevWalk();
			EmptyTree = Rw.parseTree(_ow.WriteTree(new Tree(db)));
			_nowTick = 1236977987L;
		}

		protected virtual GitSharp.RevWalk.RevWalk CreateRevWalk()
		{
			return new GitSharp.RevWalk.RevWalk(db);
		}

		private void Tick(int secDelta)
		{
			_nowTick += secDelta * 1000L;
		}

		protected RevBlob Blob(string content)
		{
			return Rw.lookupBlob(_ow.WriteBlob(Constants.encode(content)));
		}

		protected static DirCacheEntry File(string path, RevBlob blob)
		{
			var e = new DirCacheEntry(path);
			e.setFileMode(FileMode.RegularFile);
			e.setObjectId(blob);
			return e;
		}

		protected RevTree Tree(params DirCacheEntry[] entries)
		{
			DirCache dc = DirCache.newInCore();
			DirCacheBuilder b = dc.builder();
			foreach (DirCacheEntry e in entries)
			{
				b.add(e);
			}
			b.finish();
			return Rw.lookupTree(dc.writeTree(_ow));
		}

		protected RevObject Get(RevTree tree, string path)
		{
			var tw = new GitSharp.TreeWalk.TreeWalk(db);
			tw.setFilter(PathFilterGroup.createFromStrings(new[] { path }));
			tw.reset(tree);

			while (tw.next())
			{
				if (tw.isSubtree() && !path.Equals(tw.getPathString()))
				{
					tw.enterSubtree();
					continue;
				}
				ObjectId entid = tw.getObjectId(0);
				FileMode entmode = tw.getFileMode(0);
				return Rw.lookupAny(entid, (int)entmode.ObjectType);
			}

			Assert.False(true, "Can't find " + path + " in tree " + tree.Name);
			return null; // never reached.
		}

		protected RevCommit Commit(params RevCommit[] parents)
		{
			return Commit(1, EmptyTree, parents);
		}

		protected RevCommit Commit(RevTree tree, params RevCommit[] parents)
		{
			return Commit(1, tree, parents);
		}

		protected RevCommit Commit(int secDelta, params RevCommit[] parents)
		{
			return Commit(secDelta, EmptyTree, parents);
		}

		private RevCommit Commit(int secDelta, ObjectId tree, params RevCommit[] parents)
		{
			Tick(secDelta);

			var c = new Commit(db)
						{
							TreeId = tree,
							ParentIds = parents,
							Author = new PersonIdent(jauthor, _nowTick.GitTimeToDateTimeOffset(0)), // [henon] offset?
							Committer = new PersonIdent(jcommitter, _nowTick.GitTimeToDateTimeOffset(0)),
							Message = string.Empty
						};

			return Rw.lookupCommit(_ow.WriteCommit(c));
		}

		protected RevTag Tag(string name, RevObject dst)
		{
			var t = new Tag(db)
						{
							TagType = Constants.typeString(dst.Type),
							Id = dst.ToObjectId(),
							TagName = name,
							Tagger = new PersonIdent(jcommitter, _nowTick.GitTimeToDateTimeOffset(0)),
							Message = string.Empty
						};

			return (RevTag)Rw.lookupAny(_ow.WriteTag(t), Constants.OBJ_TAG);
		}

		protected T Parse<T>(T t)
			where T : RevObject
		{
			Rw.parseBody(t);
			return t;
		}

		protected void MarkStart(RevCommit commit)
		{
			Rw.markStart(commit);
		}

		protected void MarkUninteresting(RevCommit commit)
		{
			Rw.markUninteresting(commit);
		}

		protected static void AssertCommit(RevCommit exp, RevCommit act)
		{
			Assert.Same(exp, act);
		}
	}
}