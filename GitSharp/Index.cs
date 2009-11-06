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
            GitIndex.FilenameEncoding = repo.PreferredEncoding;
            if (_repo.PreferredEncoding != Encoding.UTF8 && _repo.PreferredEncoding != Encoding.Default)
                GitIndex.FilenameEncoding = Encoding.Default;
        }

        internal GitSharp.Core.GitIndex GitIndex
        {
            get
            {
                return _repo._internal_repo.Index;
            }
        }

        /// <summary>
        /// Add all untracked files to the index and stage all changes (like git add .)
        /// </summary>
        public void AddAll()
        {
            Add(_repo.WorkingDirectory);
        }

        /// <summary>
        /// Adds untracked files or directories to the index and writes the index to the disk (like "git add").
        /// For tracked files that were modified, it stages the modification. Is a no-op for tracked files that were
        /// not modified.
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

        /// <summary>
        /// Add content to the index directly without the need for a file in the working directory.
        /// </summary>
        /// <param name="encoded_relative_filepath">encoded file path (relative to working directory)</param>
        /// <param name="encoded_content">encoded content</param>
        public void Add(byte[] encoded_relative_filepath, byte[] encoded_content)
        {
            GitIndex.RereadIfNecessary();
            GitIndex.add(encoded_relative_filepath, encoded_content);
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
        /// Stages the given files. Untracked files are added.
        /// </summary>
        /// <param name="paths"></param>
        public void Stage(params string[] paths)
        {
            Add(paths);
        }

        public void Unstage(params string[] file)
        {
            // TODO: make sure it is tracked. then reset to the version in the current commit's tree.
            throw new NotImplementedException();
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

        /// <summary>
        /// Returns true if the index has been changed, which means there are changes to be committed. This
        /// is not to be confused with the status of the working directory. If changes in the working directory have not been
        /// staged then IsChanged is false.
        /// </summary>
        public bool IsChanged
        {
            get
            {
                return GitIndex.IsChanged;
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
            if ((parent == null && GitIndex.Members.Count == 0) || (parent != null && parent.Tree._id == tree_id))
                throw new InvalidOperationException("There are no changes to commit");
            var commit = Commit.Create(message, parent, new Tree(_repo, tree_id), author);
            Ref.Update("HEAD", commit);
            return commit;
        }

        public override string ToString()
        {
            return "Index[" + Path.Combine(_repo.Directory, "index") + "]";
        }
    }
}
