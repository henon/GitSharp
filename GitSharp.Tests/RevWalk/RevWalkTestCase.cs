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

using System;
using GitSharp.Core;
using GitSharp.Core.RevWalk;
using GitSharp.Core.DirectoryCache;
using GitSharp.Core.TreeWalk.Filter;
using NUnit.Framework;
using GitSharp.Core.Util;

namespace GitSharp.Tests.RevWalk
{
    
	public abstract class RevWalkTestCase : RepositoryTestCase
	{
		private ObjectWriter _ow;
		protected RevTree emptyTree;
		protected long nowTick; // [henon] this are seconds in git internal time representaiton
		protected GitSharp.Core.RevWalk.RevWalk rw;

		[SetUp]
		public override void setUp()
		{
			base.setUp();
			_ow = new ObjectWriter(db);
			rw = createRevWalk();
            emptyTree = rw.parseTree(_ow.WriteTree(new Core.Tree(db)));
			nowTick = 1236977987000L;
		}

		protected virtual GitSharp.Core.RevWalk.RevWalk createRevWalk()
		{
			return new GitSharp.Core.RevWalk.RevWalk(db);
		}

		protected void Tick(int secDelta)
		{
			nowTick += secDelta * 1000L;
		}

		protected RevBlob blob(string content)
		{
			return rw.lookupBlob(_ow.WriteBlob(Constants.encode(content)));
		}

		protected static DirCacheEntry File(string path, RevBlob blob)
		{
			var e = new DirCacheEntry(path);
			e.setFileMode(FileMode.RegularFile);
			e.setObjectId(blob);
			return e;
		}

		protected RevTree tree(params DirCacheEntry[] entries)
		{
			DirCache dc = DirCache.newInCore();
			DirCacheBuilder b = dc.builder();
			foreach (DirCacheEntry e in entries)
			{
				b.add(e);
			}
			b.finish();
			return rw.lookupTree(dc.writeTree(_ow));
		}

		protected RevObject get(RevTree tree, string path)
		{
			var tw = new GitSharp.Core.TreeWalk.TreeWalk(db);
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
				return rw.lookupAny(entid, (int)entmode.ObjectType);
			}

			Assert.Fail("Can't find " + path + " in tree " + tree.Name);
			return null; // never reached.
		}

		protected RevCommit Commit(params RevCommit[] parents)
		{
			return Commit(1, emptyTree, parents);
		}

		protected RevCommit Commit(RevTree tree, params RevCommit[] parents)
		{
			return Commit(1, tree, parents);
		}

		protected RevCommit Commit(int secDelta, params RevCommit[] parents)
		{
			return Commit(secDelta, emptyTree, parents);
		}

		private RevCommit Commit(int secDelta, ObjectId tree, params RevCommit[] parents)
		{
			Tick(secDelta);

            var c = new Core.Commit(db)
			        	{
			        		TreeId = tree,
			        		ParentIds = parents,
							Author = new PersonIdent(author, (nowTick).MillisToDateTime()), // [henon] offset?
			        		Committer = new PersonIdent(committer, (nowTick).MillisToDateTime()),
			        		Message = string.Empty
			        	};

			return rw.lookupCommit(_ow.WriteCommit(c));
		}

		protected RevTag Tag(string name, RevObject dst)
		{
            var t = new Core.Tag(db)
						{
							TagType = Constants.typeString(dst.Type),
							Id = dst.ToObjectId(),
							TagName = name,
                            Tagger = new PersonIdent(committer, (nowTick).MillisToDateTime()),
							Message = string.Empty
						};

			return (RevTag)rw.lookupAny(_ow.WriteTag(t), Constants.OBJ_TAG);
		}

		protected T Parse<T>(T t)
			where T : RevObject
		{
			rw.parseBody(t);
			return t;
		}

		protected void MarkStart(RevCommit commit)
		{
			rw.markStart(commit);
		}

		protected void MarkUninteresting(RevCommit commit)
		{
			rw.markUninteresting(commit);
		}

		protected static void AssertCommit(RevCommit exp, RevCommit act)
		{
			Assert.AreSame(exp, act);
		}
	}
}