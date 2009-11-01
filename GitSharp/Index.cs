using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CoreCommit = GitSharp.Core.Commit;

namespace GitSharp
{

    /// <summary>
    /// Represents the index of a git repository which keeps track of changes that are about to be committed.
    /// </summary>
    public class Index
    {
        private Repository _repo;

        public Index(Repository repo)
        {
            _repo = repo;
        }

        internal GitSharp.Core.GitIndex GitIndex
        {
            get
            {
                return _repo._internal_repo.Index;
            }
        }

        /// <summary>
        /// Add all untracked files to the index (like git add .)
        /// </summary>
        public void AddAll()
        {
            Add(_repo.WorkingDirectory);
        }

        /// <summary>
        /// Adds untracked files or directories to the index and writes the index to the disk (like "git add")
        /// 
        /// Note: Add as many files as possible by one call of this method for best performance.
        /// </summary>
        /// <param name="paths">Paths to add to the index</param>
        public void Add(params string[] paths)
        {
            GitIndex.RereadIfNecessary();
            foreach (var path in paths)
            {
                if (new FileInfo(path).Exists)
                    AddFile(new FileInfo(path));
                else if (new DirectoryInfo(path).Exists)
                    AddDirectory(new DirectoryInfo(path));
                else
                    throw new ArgumentException("File or directory at <" + path + "> doesn't seem to exist.", "path");
            }
            GitIndex.write();
        }

        private void AddFile(FileInfo path)
        {
            GitIndex.add(_repo._internal_repo.WorkingDirectory, path);
        }

        private void AddDirectory(DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles())
                AddFile(file);
            foreach (var subdir in dir.GetDirectories())
                AddDirectory(subdir);
        }

        /// <summary>
        /// Removes files or directories from the index which are no longer to be tracked. 
        /// Does not delete files from the working directory. Use Delete to remove and delete files.
        /// 
        /// Note: Remove requires the files and directories to be removed to be present in the working
        /// directory in order to find out. TODO: make this not dependent of the working directory by
        /// looking into the tree of the current commit.
        /// </summary>
        /// <param name="paths"></param>
        public void Remove(params string[] paths)
        {
            GitIndex.RereadIfNecessary();
            foreach (var path in paths)
            {
                if (new FileInfo(path).Exists)
                    RemoveFile(new FileInfo(path), false);
                else if (new DirectoryInfo(path).Exists)
                    RemoveDirectory(new DirectoryInfo(path), false);
                else
                    throw new ArgumentException("File or directory at <" + path + "> doesn't seem to exist.", "path");
            }
            GitIndex.write();
        }

        /// <summary>
        /// Removes files or directories from the index and delete them from the working directory.
        /// 
        /// </summary>
        /// <param name="paths"></param>
        public void Delete(params string[] paths)
        {
            GitIndex.RereadIfNecessary();
            foreach (var path in paths)
            {
                if (new FileInfo(path).Exists)
                    RemoveFile(new FileInfo(path), true);
                else if (new DirectoryInfo(path).Exists)
                    RemoveDirectory(new DirectoryInfo(path), true);
                else
                    throw new ArgumentException("File or directory at <" + path + "> doesn't seem to exist.", "path");
            }
            GitIndex.write();
        }

        private void RemoveFile(FileInfo path, bool delete_file)
        {
            GitIndex.remove((FileSystemInfo)_repo._internal_repo.WorkingDirectory, (FileSystemInfo)path); // Todo: change GitIndex.Remove to remove(DirectoryInfo , FileInfo) ??
            if (delete_file)
                path.Delete();
        }

        private void RemoveDirectory(DirectoryInfo dir, bool delete_dir)
        {
            foreach (var file in dir.GetFiles())
                RemoveFile(file, delete_dir);
            foreach (var subdir in dir.GetDirectories())
                RemoveDirectory(subdir, delete_dir);
            if (delete_dir)
                dir.Delete(true);
        }

        /// <summary>
        /// Writes the index to the disk.
        /// </summary>
        public void Write()
        {
            GitIndex.write();
        }

        /// <summary>
        /// Reads the index from the disk
        /// </summary>
        public void Read()
        {
            GitIndex.Read();
        }

         //public RepositoryStatus CompareAgainstWorkingDirectory(bool honor_ignore_rules)

        public RepositoryStatus Status
        {
            get
            {
                //if (honor_ignore_rules)
                //    throw new NotImplementedException("Ignore rules are not implemented");
                var commit = _repo.Head.CurrentCommit;
                var tree = commit != null ? commit.Tree.InternalTree : new GitSharp.Core.Tree(_repo._internal_repo);
                var diff = new GitSharp.Core.IndexDiff(tree, GitIndex);
                return new RepositoryStatus(diff);
            }
        }

        public Commit CommitChanges(string message, Author author)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Commit message must not be null or empty!", "message");
            if (string.IsNullOrEmpty(author.Name))
                throw new ArgumentException("Author name must not be null or empty!", "author");
            GitIndex.RereadIfNecessary();
            var tree_id = GitIndex.writeTree();
            // check if tree is different from current commit's tree
            var parent = _repo.CurrentBranch.CurrentCommit;
            if (GitIndex.Members.Count == 0 || (parent != null && parent.Tree._id == tree_id))
                throw new InvalidOperationException("There are no changes to commit");
            var corecommit = new CoreCommit(_repo._internal_repo);
            if (parent != null)
                corecommit.ParentIds = new GitSharp.Core.ObjectId[] { parent._id };
            corecommit.Author = new GitSharp.Core.PersonIdent(author.Name, author.EmailAddress, DateTime.Now, TimeZoneInfo.Local);
            corecommit.Committer = corecommit.Author;
            corecommit.Message = message;
            corecommit.TreeEntry = _repo._internal_repo.MapTree(tree_id);
            corecommit.Save();
            var newRef =  _repo._internal_repo.UpdateRef("HEAD");
            newRef.NewObjectId =corecommit.CommitId;
            newRef.IsForceUpdate = true;
            newRef.Update();
            return new Commit(_repo, corecommit);
        }

        public override string ToString()
        {
            return "Index[" + Path.Combine(_repo.Directory, "index") + "]";
        }
    }
}
