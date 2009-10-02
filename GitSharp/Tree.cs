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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ObjectId = GitSharp.Core.ObjectId;
using CoreRef = GitSharp.Core.Ref;
using CoreCommit = GitSharp.Core.Commit;
using CoreTree = GitSharp.Core.Tree;
using FileTreeEntry = GitSharp.Core.FileTreeEntry;

namespace Git
{

    /// <summary>
    /// Represents a directory in the git repository.
    /// </summary>
    public class Tree : AbstractObject, ITreeNode
    {
        internal Tree(Repository repo, CoreTree tree)
            : base(repo, tree.Id)
        {
            _internal_tree = tree;
        }

        private CoreTree _internal_tree;

        private CoreTree InternalTree
        {
            get
            {
                if (_internal_tree == null)
                    try
                    {
                        _internal_tree = _repo._internal_repo.MapTree(_id);
                    }
                    catch (Exception)
                    {
                        // the commit object is invalid. however, we can not allow exceptions here because they would not be expected.
                    }
                return _internal_tree;
            }
        }

        public string Name
        {
            get
            {
                if (InternalTree == null)
                    return null;
                return InternalTree.Name;
            }
        }

        /// <summary>
        /// True if the tree has no parent.
        /// </summary>
        public bool IsRoot
        {
            get
            {
                if (InternalTree == null)
                    return true;
                return InternalTree.IsRoot;
            }
        }

        public Tree Parent
        {
            get
            {
                if (InternalTree == null)
                    return null;
                return new Tree(_repo, InternalTree.Parent);
            }
        }

        public IEnumerable<AbstractObject> Children
        {
            get
            {
                if (InternalTree == null)
                    return new Leaf[0];
                return InternalTree.Members.Select(tree_entry =>
                {
                    if (tree_entry is FileTreeEntry)
                        return new Leaf(_repo, tree_entry as FileTreeEntry) as AbstractObject;
                    else
                        return new Tree(_repo, tree_entry as CoreTree) as AbstractObject; // <--- is this always correct? we'll see :P
                }).ToArray();
            }
        }

        public string Path
        {
            get
            {
                if (InternalTree == null)
                    return null;
                return InternalTree.FullName;
            }
        }

        public int Permissions
        {

            get
            {
                if (InternalTree == null)
                    return 0;
                return InternalTree.Mode.Bits;
            }
        }

        public override string ToString()
        {
            return "Tree[" + ShortHash + "]";
        }
    }
}
