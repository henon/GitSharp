/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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

using System.Collections.Generic;
using System.IO;
using GitSharp.Core;

namespace GitSharp
{
	public class RepositoryStatus
	{

		private GitIndex _index;
		private Core.Tree _tree;

		public RepositoryStatus(Repository repository)
		{
			Repository = repository;
			Update();
		}

		public Repository Repository
		{
			get;
			private set;
		}

		/// <summary>
		/// List of files added to the index, which are not in the current commit
		/// </summary>
		public HashSet<string> Added { get; private set; }

		/// <summary>
		/// List of files added to the index, which are already in the current commit with different content
		/// </summary>
		public HashSet<string> Staged { get; private set; }

		/// <summary>
		/// List of files removed from the index but are existent in the current commit
		/// </summary>
		public HashSet<string> Removed { get; private set; }

		/// <summary>
		/// List of files existent in the index but are missing in the working directory
		/// </summary>
		public HashSet<string> Missing { get; private set; }

		/// <summary>
		/// List of files with unstaged modifications. A file may be modified and staged at the same time if it has been modified after adding.
		/// </summary>
		public HashSet<string> Modified { get; private set; }

		/// <summary>
		/// List of files existing in the working directory but are neither tracked in the index nor in the current commit.
		/// </summary>
		public HashSet<string> Untracked { get; private set; }

		/// <summary>
		/// List of files with staged modifications that conflict.
		/// </summary>
		public HashSet<string> MergeConflict { get; private set; }

		///// <summary>
		///// Returns the number of files checked into the git repository
		///// </summary>
		//public int IndexSize { get { return _index.Members.Count; } }

		public bool AnyDifferences { get; private set; }

		/// <summary>
		/// Run the diff operation. Until this is called, all lists will be empty
		/// </summary>
		/// <returns>true if anything is different between index, tree, and workdir</returns>
		private bool Diff()
		{
			var commit = Repository.Head.CurrentCommit;
			_tree = (commit != null ? commit.Tree : new Core.Tree(Repository));
			_index = Repository.Index.GitIndex;
			DirectoryInfo root = _index.Repository.WorkingDirectory;
			var visitor = new AbstractIndexTreeVisitor { VisitEntryAux = OnVisitEntry };
			new IndexTreeWalker(_index, _tree, new DirectoryTree(Repository), root, visitor).Walk();
			return AnyDifferences;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="treeEntry"></param>
		/// <param name="wdirEntry">Note: wdirEntry is the gitignored working directory entry.</param>
		/// <param name="indexEntry"></param>
		/// <param name="file">Note: gitignore patterns do not influence this parameter</param>
		private void OnVisitEntry(TreeEntry treeEntry, TreeEntry wdirEntry, GitIndex.Entry indexEntry, FileInfo file)
		{
			//Console.WriteLine(" ----------- ");
			//if (treeEntry != null)
			//   Console.WriteLine("tree: " + treeEntry.Name);
			//if (wdirEntry != null)
			//   Console.WriteLine("w-dir: " + wdirEntry.Name);
			//if (indexEntry != null)
			//   Console.WriteLine("index: " + indexEntry.Name);
			//Console.WriteLine("file: " + file.Name);
			if (indexEntry != null)
			{
				if (treeEntry == null)
				{
					Added.Add(indexEntry.Name);
					AnyDifferences = true;
				}
				if (treeEntry != null && !treeEntry.Id.Equals(indexEntry.ObjectId))
				{
					Staged.Add(indexEntry.Name);
					AnyDifferences = true;
				}
				if (!file.Exists)
				{
					Missing.Add(indexEntry.Name);
					AnyDifferences = true;
				}
				if (file.Exists && indexEntry.IsModified(new DirectoryInfo(Repository.WorkingDirectory), true))
				{
					Modified.Add(indexEntry.Name);
					AnyDifferences = true;
				}
				if (indexEntry.Stage != 0)
				{
					MergeConflict.Add(indexEntry.Name);
					AnyDifferences = true;
				}
			}
			else // <-- index entry == null
			{
				if (treeEntry != null && !(treeEntry is Tree))
				{
					Removed.Add(treeEntry.FullName);
					AnyDifferences = true;
				}
				if (wdirEntry != null) // actually, we should enforce (treeEntry == null ) here too but original git does not, may be a bug. 
					Untracked.Add(wdirEntry.FullName);
			}
		}

		/// <summary>
		/// Recalculates the status
		/// </summary>
		public void Update()
		{
			AnyDifferences = false;
			Added = new HashSet<string>();
			Staged = new HashSet<string>();
			Removed = new HashSet<string>();
			Missing = new HashSet<string>();
			Modified = new HashSet<string>();
			Untracked = new HashSet<string>();
			MergeConflict = new HashSet<string>();
			Diff();
		}
	}
}
