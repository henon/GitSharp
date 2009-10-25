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

        private GitSharp.Core.GitIndex GitIndex
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
        /// Add an untracked file or directory to the index (like git add)
        /// </summary>
        /// <param name="path"></param>
        public void Add(string path)
        {
            if (new FileInfo(path).Exists)
                AddFile(path);
            else if (new DirectoryInfo(path).Exists)
                AddDirectory(path);
            else
                throw new ArgumentException("File or directory at <"+path+"> doesn't seem to exist.", "path");
        }

        /// <summary>
        /// Add an untracked file to the index (like git add)
        /// </summary>
        /// <param name="path"></param>
        public void AddFile(string path)
        {
            GitIndex.add(_repo._internal_repo.WorkingDirectory, new FileInfo(path));
        }

        /// <summary>
        /// Add an untracked directory to the index (like git add)
        /// </summary>
        /// <param name="path"></param>
        public void AddDirectory(string path)
        {
            throw new NotImplementedException("we need to recursively add files here, but be careful ... .gitignore must be respected");
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
    }
}
