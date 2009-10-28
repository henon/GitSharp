using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileTreeEntry = GitSharp.Core.FileTreeEntry;

namespace GitSharp
{

    /// <summary>
    /// Leaf represents a file entry in a Tree.
    /// </summary>
    public class Leaf : Blob, ITreeNode
    {
        internal Leaf(Repository repo, FileTreeEntry entry) : base(repo, entry.Id) {
            _internal_file_tree_entry = entry;
        }

        private FileTreeEntry _internal_file_tree_entry;

        /// <summary>
        /// True if the file is executable (unix).
        /// </summary>
        public bool IsExecutable
        {
            get
            {
                return _internal_file_tree_entry.IsExecutable;
            }
        }

        /// <summary>
        /// The file name
        /// </summary>
        public string Name
        {
            get
            {
                return _internal_file_tree_entry.Name;
            }
        }

        /// <summary>
        /// The full path relative to repostiory root
        /// </summary>
        public string Path
        {
            get
            {
                return _internal_file_tree_entry.FullName;
            }
        }

        /// <summary>
        /// The unix file permissions.
        /// 
        /// Todo: model this with a permission object
        /// </summary>
        public int Permissions
        {
            get
            {
                return _internal_file_tree_entry.Mode.Bits;
            }
        }

        /// <summary>
        /// The parent Tree.
        /// </summary>
        public Tree Parent
        {
            get
            {
                return new Tree(_repo, _internal_file_tree_entry.Parent);
            }
        }

    }
}
