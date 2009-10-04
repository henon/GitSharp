using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ObjectId = GitSharp.Core.ObjectId;
using CoreRef = GitSharp.Core.Ref;
using CoreCommit = GitSharp.Core.Commit;
using CoreTree = GitSharp.Core.Tree;
using CoreRepository = GitSharp.Core.Repository;

namespace Git
{
    public class Branch : Ref
    {
        public Branch(Ref @ref) : base(@ref._repo, @ref.Name)
        {
        }

        public Branch(Repository repo, string name) : base(repo, name)
        {
        }

        internal Branch(Repository repo, CoreRef @ref)
            : this(repo, @ref.Name)
        {
        }

        /// <summary>
        /// Returns the latest commit on this branch.
        /// </summary>
        public Commit CurrentCommit
        {
            get
            {
                return Target as Commit;
            }
        }

        /// <summary>
        /// True if this ref points to a remote branch.
        /// </summary>
        public bool IsRemote
        {
            get;
            internal set;
        }

        public void Merge(Branch other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete this branch
        /// </summary>
        public void Delete()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Switch to this branch and check it out into the working directory.
        /// </summary>
        public void Checkout()
        {
            throw new NotImplementedException();
        }

        public void ResetSoft(string hash)
        {
            throw new NotImplementedException();
        }

        public void ResetHard(string hash)
        {
            throw new NotImplementedException();
        }

    }
}
