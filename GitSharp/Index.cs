using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Git
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
            GitIndex.Read();
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

        private void AddDirectory(DirectoryInfo path)
        {
            AddRecursively(path);
        }

        private void AddRecursively(DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles())
                AddFile(file);
            foreach (var subdir in dir.GetDirectories())
                AddDirectory(subdir);
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

        public RepositoryStatus CompareAgainstWorkingDirectory()
        {
            return CompareAgainstWorkingDirectory(true);
        }

        public RepositoryStatus CompareAgainstWorkingDirectory(bool honor_ignore_rules)
        {
            if (honor_ignore_rules)
                throw new NotImplementedException("Ignore rules are not implemented");
            var tree = new GitSharp.Core.Tree(_repo._internal_repo);
            var diff = new GitSharp.Core.IndexDiff(tree, GitIndex);
            return new RepositoryStatus(diff);
        }

        public override string ToString()
        {
            return "Index[" + Path.Combine(_repo.Directory, "index") + "]";
        }
    }
}
