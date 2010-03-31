using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Core.Merge;
using GitSharp.Core.RevWalk;

namespace GitSharp.Commands
{
	public static class MergeCommand
	{
		public static MergeResult Execute(MergeOptions options)
		{
			options.Validate();

			var result = new MergeResult();
			if (CanFastForward(options))
			{
				result.Success=true;
				result.Commit = options.Commits[1];
				result.Tree = result.Commit.Tree;
				UpdateBranch(options, result);
			}
			else
			{
				var merger = SelectMerger(options);
				result.Success = merger.Merge(options.Commits.Select(c => ((Core.Commit)c).CommitId).ToArray());
				result.Tree = new Tree(options.Repository, merger.GetResultTreeId());
				if (options.NoCommit)
				{
					// this is empty on purpose
				}
				else
				{
					if (string.IsNullOrEmpty(options.Message))
					{
						options.Message = FormatMergeMessage(options);
					}
					var author = Author.GetDefaultAuthor(options.Repository);
					result.Commit = Commit.Create(options.Message, options.Commits, result.Tree, author, author, DateTimeOffset.Now);
					UpdateBranch(options, result);
				}
			}
			return result;
		}

		private static void UpdateBranch(MergeOptions options, MergeResult result)
		{
			if (options.Branches.Length >= 1 && options.Branches[0] is Branch)
				Ref.Update("refs/heads/" + options.Branches[0].Name, result.Commit);
		}

		private static bool CanFastForward(MergeOptions options)
		{
			if (options.MergeStrategy != MergeStrategy.Recursive || options.NoFastForward || options.Commits.Length != 2)
				return false;
			var rw = new RevWalk(options.Repository);
			return rw.isMergedInto(new RevCommit(options.Commits[0]._id), new RevCommit(options.Commits[1]._id));
		}

		private static string FormatMergeMessage(MergeOptions options)
		{
			if (options.Branches.Length > 0 && options.Branches[0] is Branch)
				return string.Format("Merge branch '{0}' into {1}", options.Branches[1].Name, options.Branches[0].Name);
			else
				return "Merge commits: " + string.Join(", ", options.Commits.Select(c => c.Hash).ToArray()); // todo: replace this fallback message with something sensible.
		}

		private static Merger SelectMerger(MergeOptions options)
		{
			switch (options.MergeStrategy)
			{
				case MergeStrategy.Ours:
					return Core.Merge.MergeStrategy.Ours.NewMerger(options.Repository);
				case MergeStrategy.Theirs:
					return Core.Merge.MergeStrategy.Theirs.NewMerger(options.Repository);
				case MergeStrategy.Recursive:
					return Core.Merge.MergeStrategy.SimpleTwoWayInCore.NewMerger(options.Repository);
			}
			throw new ArgumentException("Invalid merge option: " + options.MergeStrategy);
		}

	}


	public enum MergeStrategy { Ours, Theirs, Recursive }

	public class MergeOptions
	{
		public MergeOptions()
		{
			NoCommit = false;
			NoFastForward = false;
		}

		internal Repository Repository { get; set; }

		/// <summary>
		/// Commit message of the merge. If left empty or null a good default message will be provided by the merge command.
		/// </summary>
		public string Message { get; set; }

		public MergeStrategy MergeStrategy { get; set; }

		private Ref[] _branches;

		/// <summary>
		/// The branches to merge. This automatically sets the Commits property.
		/// </summary>
		public Ref[] Branches
		{
			get { return _branches; }
			set
			{
				_branches = value;
				if (value != null)
					Commits = value.Select(b => b.Target as Commit).ToArray();
				if (_branches != null && _branches.Length > 0 && _branches[0] != null)
					Repository = _branches[0]._repo;
			}
		}

		/// <summary>
		/// The commits to merge, set this only if you can not specify the branches.
		/// </summary>
		public Commit[] Commits { get; set; }

		/// <summary>
		/// With NoCommit=true MergeCommand performs the merge but pretends the merge failed and does not autocommit, to give the user a chance to inspect and further tweak the merge result before committing.
		/// By default MergeCommand performs the merge and committs the result (the default value is false).
		/// </summary>
		public bool NoCommit { get; set; }

		/// <summary>
		/// When true Generate a merge commit even if the merge resolved as a fast-forward. 
		/// MergeCommand by default does not generate a merge commit if the merge resolved as a fast-forward, only updates the branch pointer (the default value is false).
		/// </summary>
		public bool NoFastForward { get; set; }


		public bool Log { get; set; }

		public void Validate()
		{
			if (Repository == null)
				throw new ArgumentException("Repository must not be null");
			if (Commits.Count() < 2)
				throw new ArgumentException("Need at least two commits to merge");
		}
	}

	public class MergeResult
	{
		/// <summary>
		/// True if the merge was sucessful. In case of conflicts or the strategy not being able to conduct the merge this is false.
		/// </summary>
		public bool Success { get; set; }

		/// <summary>
		/// Result object of the merge command. If MergeOptions.NoCommit == true this is null.
		/// </summary>
		public Commit Commit { get; set; }

		/// <summary>
		/// Resulting tree. This property is especially useful when merging with option NoCommit == true.
		/// </summary>
		public Tree Tree { get; set; }

	}
}
