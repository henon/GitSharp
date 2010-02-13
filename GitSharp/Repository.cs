/*
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
 * - Neither the name of the TicGit project nor the
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
using System.Security;
using System.Text;
using System.IO;
using System.Diagnostics;
using GitSharp.Commands;
using GitSharp.Core;
using CoreRepository = GitSharp.Core.Repository;
using System.Text.RegularExpressions;

namespace GitSharp
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	public class Repository : IDisposable
	{
		#region Constructors

		internal CoreRepository _internal_repo;

		internal Repository(CoreRepository repo)
		{
			PreferredEncoding = Encoding.UTF8;
			_internal_repo = repo;
		}

		/// <summary>
		/// Initializes the Repository object.
		/// </summary>
		/// <param name="path">Path to the local git repository.</param>
		public Repository(string path)
			: this(GitSharp.Core.Repository.Open(path))
		{
		}


		#endregion

		#region Public properties


		/// <summary>
		/// Returns the path to the repository database (i.e. the .git directory).
		/// </summary>
		public string Directory
		{
			get
			{
				Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
				return _internal_repo.Directory.FullName;
			}
		}

		/// <summary>
		/// Gets or sets Head which is a symbolic reference to the active branch. Note that setting head 
		/// does not automatically check out that branch into the repositories working directory. 
		/// </summary>
		public Branch Head
		{
			get
			{
				Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
				return new Branch(this, "HEAD");
			}
			set
			{
				// Todo: what should we do with null?
				if (Head.Name != value.Name)
				{
					if (Branches.ContainsKey(value.Name))
					{
						var updateRef = _internal_repo.UpdateRef("HEAD");
						updateRef.NewObjectId = value.Target._id;
						updateRef.IsForceUpdate = true;
						updateRef.update();
						_internal_repo.WriteSymref(GitSharp.Core.Constants.HEAD, value.Name);
					}
					else
						throw new ArgumentException("Trying to set HEAD to non existent branch: " + value.Name);
				}

			}
		}

		public Index Index
		{
			get
			{
				return new Index(this); // <--- this is just a wrapper around the internal repo's GitIndex instance so need not cache it here
			}
		}

		/// <summary>
		/// Returns true if this repository is a bare repository. Bare repositories don't have a working directory and thus do not support some operations.
		/// </summary>
		public bool IsBare
		{
			get
			{
				Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
				return _internal_repo.Config.getBoolean("core", "bare", false);
			}
		}

		/// <summary>
		/// Returns the path to the working directory (i.e. the parent of the .git directory of a non-bare repo). Returns null if it is a bare repository.
		/// </summary>
		public string WorkingDirectory
		{
			get
			{
				Debug.Assert(_internal_repo != null, "Repository not initialized correctly.");
				if (IsBare)
					return null;
				return _internal_repo.WorkingDirectory.FullName;
			}
		}

		#endregion

		/// <summary>
		/// Check out the branch with the given name
		/// </summary>
		/// <param name="name"></param>
		public void CheckoutBranch(string name)
		{
			CheckoutBranch(new Branch(this, name));
		}

		/// <summary>
		/// Check out the given branch
		/// </summary>
		/// <param name="branch"></param>
		public void CheckoutBranch(Branch branch)
		{
			Branch.SwitchTo(branch);
		}

		/// <summary>
		/// Commit staged changes and update HEAD. The default git author is used.
		/// </summary>
		/// <param name="message">The commit message</param>
		/// <returns>Returns the newly created commit</returns>
		public Commit Commit(string message)
		{
			return Commit(message, new Author(Config["user.name"], Config["user.email"]));
		}

		/// <summary>
		/// Commit staged changes and update HEAD
		/// </summary>
		/// <param name="message">The commit message</param>
		/// <param name="author">The author of the content to be committed</param>
		/// <returns>Returns the newly created commit</returns>
		public Commit Commit(string message, Author author)
		{
			return Index.CommitChanges(message, author);
		}

		public IDictionary<string, Ref> Refs
		{
			get
			{
				var internal_refs = _internal_repo.getAllRefs();
				var dict = new Dictionary<string, Ref>(internal_refs.Count);
				foreach (var pair in internal_refs)
					dict[pair.Key] = new Ref(this, pair.Value);
				return dict;
			}
		}

		public IDictionary<string, Tag> Tags
		{
			get
			{
				var internal_tags = _internal_repo.getTags();
				var dict = new Dictionary<string, Tag>(internal_tags.Count);
				foreach (var pair in internal_tags)
					dict[pair.Key] = new Tag(this, pair.Value);
				return dict;
			}
		}

		public IDictionary<string, Branch> Branches
		{
			get
			{
				IDictionary<string, Core.Ref> internal_refs = _internal_repo._refDb.getRefs(Constants.R_HEADS);
				var dict = new Dictionary<string, Branch>(internal_refs.Count);
				foreach (var pair in internal_refs)
					dict[pair.Key.TrimStart('/')] = new Branch(this, pair.Value);
				return dict;
			}
		}

		public Branch CurrentBranch
		{
			get
			{
				return new Branch(this, _internal_repo.getBranch());
			}
		}

		public IDictionary<string, Branch> RemoteBranches
		{
			get
			{
				var internal_refs = _internal_repo._refDb.getRefs(Constants.R_REMOTES);
				var dict = new Dictionary<string, Branch>(internal_refs.Count);
				foreach (var pair in internal_refs)
				{
					var branch = new Branch(this, pair.Value);
					branch.IsRemote = true;
					dict[pair.Key] = branch;
				}
				return dict;
			}
		}

		/// <summary>
		/// Returns the git configuration containing repository-specific, user-specific and global 
		/// settings.
		/// </summary>
		public Config Config
		{
			get
			{
				return new Config(this);
			}
		}

		/// <summary>
		/// Get the differences between the working directory and the index.
		/// Returns a data structure containing the results (like "git status").
		/// </summary>
		public RepositoryStatus Status
		{
			get
			{
				return Index.Status;
			}
		}

		public static implicit operator CoreRepository(Repository repo)
		{
			return repo._internal_repo;
		}

		public override string ToString()
		{
			return "Repository[" + Directory + "]";
		}

		/// <summary>
		/// If data in a repository (i.e. filenames, commit messages, etc. ) is to be decoded with a special encoding
		/// set this property. Default setting is UTF8
		/// </summary>
		public Encoding PreferredEncoding
		{
			get;
			set;
		}

		public void Close()
		{
			_internal_repo.Dispose();
		}

		public void Dispose()
		{
			Close();
		}

		#region Repository initialization (git init)


		/// <summary>
		/// Initializes a non-bare repository. Use GitDirectory to specify location.
		/// </summary>
		/// <returns>The initialized repository</returns>
		public static Repository Init(string path)
		{
			return Init(path, false);
		}

		/// <summary>
		/// Initializes a repository. Use GitDirectory to specify the location. Default is the current directory.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="bare"></param>
		/// <returns></returns>
		public static Repository Init(string path, bool bare)
		{
			var cmd = new InitCommand() { GitDirectory = path, Bare = bare };
			return Init(cmd);
		}

		/// <summary>
		/// Initializes a repository in the current location using the provided git command's options.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public static Repository Init(InitCommand cmd)
		{
			cmd.Execute();
			return cmd.Repository;
		}

		#endregion

		/// <summary>
		/// Checks if the directory given by the path is a valid non-bare git repository. The given path may either point to 
		/// the working directory or the repository's .git directory.
		/// </summary>
		/// <param name="path"></param>
		/// <returns>Returns true if the given path is a valid git repository, false otherwise.</returns>
		public static bool IsValid(string path)
		{
			return IsValid(path, false);
		}

		/// <summary>
		/// Checks if the directory given by the path is a valid git repository.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="bare"></param>
		/// <returns>Returns true if the given path is a valid git repository, false otherwise.</returns>
		public static bool IsValid(string path, bool bare)
		{
			if (path == null)
				return false;
			if (!bare)
			{
				if (!Regex.IsMatch(path, "\\.git[/\\\\]?$"))
					path = Path.Combine(path, Constants.DOT_GIT);
			}

			if (!DirExists(path))
				return false;
			if (!FileExists(Path.Combine(path, "HEAD")))
				return false;
			if (!FileExists(Path.Combine(path, "config")))
				return false;
			//if (!DirExists(Path.Combine(path, "description")))
			//    return false;
			//if (!DirExists(Path.Combine(path, "hooks")))
			//   return false;
			//if (!DirExists(Path.Combine(path, "info")))
			//    return false;
			//if (!DirExists(Path.Combine(path, "info/exclude")))
			//    return false;
			if (!DirExists(Path.Combine(path, "objects")))
				return false;
			if (!DirExists(Path.Combine(path, "objects/info")))
				return false;
			if (!DirExists(Path.Combine(path, "objects/pack")))
				return false;
			if (!DirExists(Path.Combine(path, "refs")))
				return false;
			if (!DirExists(Path.Combine(path, "refs/heads")))
				return false;
			if (!DirExists(Path.Combine(path, "refs/tags")))
				return false;

			Repository repo = null;

			try
			{
				// let's see if it loads without throwing an exception
				repo = new Repository(path);
			}
			catch (Exception)
			{
				return false;
			}
			finally
			{
				if (repo != null)
				{
					repo.Dispose();
				}
			}
			return true;
		}

		/// <summary>
		/// Searches for a git repository starting at the given path.
		/// 
		/// Starting at the given path, searches up the file hierarchy for the next .git directory.
		/// </summary>
		/// <param name="starting_directory">The path where the search should start or null to start at the current directory</param>
		/// <returns>A path if a repository has been found or null otherwise</returns>
		public static string FindRepository(string starting_directory)
		{
			var directory = starting_directory;
			try
			{
				if (directory == null)
					directory = System.IO.Directory.GetCurrentDirectory();

				while (true)
				{
					var git = Path.Combine(directory, Constants.DOT_GIT);
					if (DirExists(git))
						return git;

					//Get parent directory
					var parent_directory = Path.Combine(Path.GetFullPath(directory), "..");
					parent_directory = Path.GetFullPath(parent_directory);
					if (parent_directory == directory) // <-- we have reached a toplevel directory which doesn't contain a .git dir.
						return null;
					directory = parent_directory;
				}
			}
			catch (ArgumentException) // --> invalid path form
			{
				return null;
			}
			catch (SecurityException) // --> access denied
			{
				return null;
			}
			catch (UnauthorizedAccessException) // --> access denied
			{
				return null;
			}
			catch (PathTooLongException)
			{
				return null;
			}
			catch (NotSupportedException) // --> hmm?
			{
				return null;
			}
		}

		private static bool DirExists(string path)
		{
			return new DirectoryInfo(path).Exists;
		}

		private static bool FileExists(string path)
		{
			return new FileInfo(path).Exists;
		}

	}

}