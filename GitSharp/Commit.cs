using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Core.Util;

using ObjectId = GitSharp.Core.ObjectId;
using CoreRef = GitSharp.Core.Ref;
using CoreCommit = GitSharp.Core.Commit;
using CoreTree = GitSharp.Core.Tree;

namespace Git
{
    /// <summary>
    /// Represents a revision of the content tracked in the repository.
    /// </summary>
    public class Commit : AbstractObject
    {

        public Commit(Repository repo, string name)
            : base(repo, name)
        {
        }

        internal Commit(Repository repo, CoreRef @ref)
            : base(repo, @ref.ObjectId)
        {
        }

        internal Commit(Repository repo, CoreCommit internal_commit)
            : base(repo, internal_commit.CommitId)
        {
            _internal_commit = internal_commit;
        }

        internal Commit(Repository repo, ObjectId id)
            : base(repo, id)
        {
        }

        private CoreCommit _internal_commit;

        private CoreCommit InternalCommit
        {
            get
            {
                if (_internal_commit == null)
                    try
                    {
                        _internal_commit = _repo._internal_repo.MapCommit(_id);
                    }
                    catch (Exception)
                    {
                        // the commit object is invalid. however, we can not allow exceptions here because they would not be expected.
                    }
                return _internal_commit;
            }
        }

        public bool IsValid
        {
            get
            {
                return InternalCommit is CoreCommit;
            }
        }

        /// <summary>
        /// The commit message.
        /// </summary>
        public string Message
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return null;
                return InternalCommit.Message;
            }
        }

        ///// <summary>
        ///// The encoding of the commit message.
        ///// </summary>
        //public Encoding Encoding
        //{
        //    get
        //    {
        //        if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
        //            return null;
        //        return InternalCommit.Encoding;
        //    }
        //}

        /// <summary>
        /// The author of the change set represented by this commit. 
        /// </summary>
        public Author Author
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return null;
                return new Author() { Name = InternalCommit.Author.Name, EmailAddress = InternalCommit.Author.EmailAddress };
            }
        }

        /// <summary>
        /// The person who committed the change set by reusing authorship information from another commit. If the commit was created by the author himself, Committer is equal to the Author.
        /// </summary>
        public Author Committer
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return null;
                var committer = InternalCommit.Committer;
                if (committer == null) // this is null if the author committed himself
                    return Author;
                return new Author() { Name = committer.Name, EmailAddress = committer.EmailAddress };
            }
        }

        /// <summary>
        /// Original timestamp of the commit created by Author. 
        /// </summary>
        public DateTimeOffset AuthorDate
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return DateTimeOffset.MinValue;
                return InternalCommit.Author.When.UnixTimeToDateTimeOffset(InternalCommit.Author.TimeZoneOffset); // leave optimizations to the compiler.
            }
        }

        /// <summary>
        /// Final timestamp of the commit, after Committer has re-committed Author's commit.
        /// </summary>
        public DateTimeOffset CommitDate
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return DateTimeOffset.MinValue;
                var committer = InternalCommit.Committer;
                if (committer == null) // this is null if the author committed himself
                     committer = InternalCommit.Author;
                return committer.When.UnixTimeToDateTimeOffset(committer.TimeZoneOffset);
            }
        }

        /// <summary>
        /// Returns true if the commit was created by the author of the change set himself.
        /// </summary>
        public bool IsCommittedByAuthor
        {
            get
            {
                return Author == Committer;
            }
        }

        /// <summary>
        /// Returns all parent commits.
        /// </summary>
        public IEnumerable<Commit> Parents
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return new Commit[0];
                return InternalCommit.ParentIds.Select(parent_id => new Commit(_repo, parent_id)).ToArray();
            }
        }

        /// <summary>
        /// True if the commit has at least one parent.
        /// </summary>
        public bool HasParents
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return false;
                return InternalCommit.ParentIds.Length > 0;
            }
        }

        /// <summary>
        /// The first parent commit if the commit has at least one parent, null otherwise.
        /// </summary>
        public Commit Parent
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return null;
                if (HasParents)
                    return new Commit(_repo, InternalCommit.ParentIds[0]);
                return null;
            }
        }

        /// <summary>
        /// The commit's reference to the root of the directory structure of the revision.
        /// </summary>
        public Tree Tree
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return null;
                try
                {
                    return new Tree(_repo, InternalCommit.TreeEntry);
                }
                catch (GitSharp.Core.Exceptions.MissingObjectException)
                {
                    return null; // relieve the client of having to catch the exception! If tree is null it is obvious that the tree could not be found.
                }
            }
        }

        public override string ToString()
        {
            return "Commit[" + ShortHash + "]";
        }
    }
}
