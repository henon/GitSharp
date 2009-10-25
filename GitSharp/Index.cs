using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GitSharp.Core.DirectoryCache;
using System.Diagnostics;

namespace Git
{

    /// <summary>
    /// Represents the index of a git repository which keeps track of changes that are about to be committed.
    /// </summary>
    public class Index : IDisposable
    {
        private Repository _repo;
        private DirCache _index;

        public Index(Repository repo)
        {
            if (repo.IsBare)
                throw new ArgumentException("Bare repositories have not got an index.");
            _repo = repo;
            _index = DirCache.read(_repo._internal_repo);
        }

        /// <summary>
        /// Add all untracked files to the index and writes the index to the disk (like "git add .")
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
            try
            {
                var builder = _index.builder();
                foreach (var path in paths)
                {
                    if (new FileInfo(path).Exists)
                        AddFile(new FileInfo(path), builder);
                    else if (new DirectoryInfo(path).Exists)
                        AddDirectory(new DirectoryInfo(path), builder);
                    else
                        throw new ArgumentException("File or directory at <" + path + "> doesn't seem to exist.", "path");
                }
                builder.finish();
                _index.write();
            }
            finally
            {
                _index.unlock();
            }
        }

        private void AddFile(FileInfo path, DirCacheBuilder builder)
        {
            builder.add(new DirCacheEntry(GitSharp.Core.Constants.encode(path.FullName)));
            //GitIndex.add(_repo._internal_repo.WorkingDirectory, new FileInfo(path));
        }

        private void AddDirectory(DirectoryInfo path, DirCacheBuilder builder)
        {
             AddRecursively(path, builder);
        }

        private void AddRecursively(DirectoryInfo dir, DirCacheBuilder builder)
        {
            foreach (var file in dir.GetFiles())
                AddFile(file, builder);
            foreach (var subdir in dir.GetDirectories())
                AddDirectory(subdir, builder);
        }

        /// <summary>
        /// Writes the index to the disk.
        /// </summary>
        public void Write()
        {
            _index.write();
        }

        /// <summary>
        /// Reads the index from the disk
        /// </summary>
        public void Read()
        {
            _index.read();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_index == null)
                return;
            _index.unlock();
            _index = null;
            _repo = null;
        }

        #endregion
    }
}
