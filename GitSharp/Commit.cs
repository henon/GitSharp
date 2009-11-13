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
using GitSharp.Core.Util;

using ObjectId = GitSharp.Core.ObjectId;
using CoreRef = GitSharp.Core.Ref;
using CoreCommit = GitSharp.Core.Commit;
using CoreTree = GitSharp.Core.Tree;
using System.IO;
using GitSharp.Core.TreeWalk;
using GitSharp.Core.TreeWalk.Filter;
using System.Diagnostics;

namespace GitSharp
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

        /// <summary>
        /// The encoding of the commit details.
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
                    return null;
                return InternalCommit.Encoding;
            }
        }

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
                return InternalCommit.Author.When.MillisToDateTimeOffset(InternalCommit.Author.TimeZoneOffset);
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
                return committer.When.MillisToDateTimeOffset(committer.TimeZoneOffset);
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

        /// <summary>
        ///  Returns all ancestor-commits of this commit. 
        ///  Be careful, in a big repository this can be quite a long list and you might go out of memory. Use for small repo's only.
        ///  
        /// Todo: reimplement this with an iterator instead of recursion.
        /// </summary>
        public IEnumerable<Commit> Ancestors
        {
            get
            {
                var ancestors = new Dictionary<ObjectId, Commit>();
                CollectAncestorIdsRecursive(this, ancestors);
                return ancestors.Values.ToArray();
            }
        }

        private static void CollectAncestorIdsRecursive(Commit commit, IDictionary<ObjectId, Commit> ancestors)
        {
            foreach (var parent in commit.InternalCommit.ParentIds.Where(id => !ancestors.ContainsKey(id)).Select(id => new Commit(commit._repo, id)))
            {
                var parentCommit = parent;
                ancestors[parentCommit._id] = parentCommit;
                CollectAncestorIdsRecursive(parentCommit, ancestors);
            }
        }

        /// <summary>
        /// Checkout this commit into the repositorie's working directory. Does not reset HEAD.
        /// </summary>
        public void Checkout()
        {
            Checkout(_repo.WorkingDirectory);
        }

        /// <summary>
        /// Checkout this commit into the given directory. Does not reset HEAD!
        /// </summary>
        /// <param name="working_directory">The directory to put the sources into</param>
        public void Checkout(string working_directory)
        {
            // Todo: what happens with a bare repo here ??
            if (InternalCommit == null)
                throw new InvalidOperationException("Unable to checkout this commit. It was not initialized properly (i.e. the hash is not pointing to a commit object).");
            if (working_directory == null)
                throw new ArgumentException("Path to checkout directory must not be null");
            if (new DirectoryInfo(working_directory).Exists == false)
                throw new IOException("Cannot checkout into non-existent directory: " + working_directory);
            var db = _repo._internal_repo;
            var index = _repo.Index.GitIndex;
            index.RereadIfNecessary();
            CoreTree tree = InternalCommit.TreeEntry;
            var co = new GitSharp.Core.WorkDirCheckout(db, new DirectoryInfo(working_directory), index, tree);
            co.checkout();
            if (working_directory == Repository.WorkingDirectory) // we wouldn't want to write index if the checkout was not done into the working directory or if the repo is bare, right?
                index.write();
        }

        /// <summary>
        /// Compare reference commit against compared commit. You may pass in a null commit (i.e. for getting the changes of the first commit)
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="compared"></param>
        /// <returns></returns>
        public static IEnumerable<Change> CompareCommits(Commit reference, Commit compared)
        {
            var changes = new List<Change>();
            if (reference == null && compared == null)
                return changes;
            var repo = (reference ?? compared).Repository;
            if (compared.Repository.Directory != repo.Directory)
                throw new InvalidOperationException("Can not compare commits from different repositories");
            var ref_tree = (reference != null ? reference.Tree._id : ObjectId.ZeroId);
            var compared_tree = (compared != null ? compared.Tree._id : ObjectId.ZeroId);
            var db = repo._internal_repo;
            var pathFilter = TreeFilter.ALL;
            var walk = new TreeWalk(db);
            //new GitSharp.Core.ObjectWriter(repo).WriteTree( new CoreTree(repo)); // <--- writing an empty tree object. very ugly hack that is necessary to get an empty tree into the treewalker.
            if (reference == null || compared == null)
                walk.reset((reference ?? compared).Tree._id);
            else
                walk.reset(new GitSharp.Core.AnyObjectId[] { ref_tree, compared_tree });
            //if (ref_tree == ObjectId.ZeroId)
            //    walk.addTree(new EmptyTreeIterator());
            //else
            //    walk.addTree(ref_tree);
            //if (compared_tree == ObjectId.ZeroId)
            //    walk.addTree(new EmptyTreeIterator());
            //else
            //    walk.addTree(compared_tree);
            walk.Recursive = true;
            walk.setFilter(AndTreeFilter.create(TreeFilter.ANY_DIFF, pathFilter));

            while (walk.next())
            {
                if (walk.getTreeCount() == 2)
                {
                    int m0 = walk.getRawMode(0);
                    int m1 = walk.getRawMode(1);
                    var change = new Change()
                    {
                        ReferenceCommit = reference,
                        ComparedCommit = compared,
                        ReferencePermissions = walk.getFileMode(0).Bits,
                        ComparedPermissions = walk.getFileMode(1).Bits,
                        Name = walk.getNameString(),
                        Path = walk.getPathString(),
                    };
                    changes.Add(change);
                    if (m0 == 0 && m1 != 0)
                    {
                        change.ChangeType = ChangeType.Added;
                        change.ComparedObject = AbstractObject.Wrap(repo, walk.getObjectId(1));
                    }
                    else if (m0 != 0 && m1 == 0)
                    {
                        change.ChangeType = ChangeType.Deleted;
                        change.ReferenceObject = AbstractObject.Wrap(repo, walk.getObjectId(0));
                    }
                    else if (m0 != m1 && walk.idEqual(0, 1))
                    {
                        change.ChangeType = ChangeType.TypeChanged;
                        change.ReferenceObject = AbstractObject.Wrap(repo, walk.getObjectId(0));
                        change.ComparedObject = AbstractObject.Wrap(repo, walk.getObjectId(1));
                    }
                    else
                    {
                        change.ChangeType = ChangeType.Modified;
                        change.ReferenceObject = AbstractObject.Wrap(repo, walk.getObjectId(0));
                        change.ComparedObject = AbstractObject.Wrap(repo, walk.getObjectId(1));
                    }
                }
                else
                {
                    var change = new Change()
                    {
                        ReferenceCommit = reference,
                        ComparedCommit = compared,
                        Name = walk.getNameString(),
                        Path = walk.getPathString(),
                    };
                    changes.Add(change);
                    if (reference != null)
                    {
                        change.ReferencePermissions = walk.getFileMode(0).Bits;
                        change.ChangeType = ChangeType.Deleted;
                        change.ReferenceObject = AbstractObject.Wrap(repo, walk.getObjectId(0));
                    }
                    else
                    {
                        change.ComparedPermissions = walk.getFileMode(0).Bits;
                        change.ChangeType = ChangeType.Added;
                        change.ComparedObject = AbstractObject.Wrap(repo, walk.getObjectId(0));
                    }
                }
            }
            return changes;
        }

        /// <summary>
        /// Compares this commit against another one and returns all changes between the two.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public IEnumerable<Change> CompareAgainst(Commit other)
        {
            return CompareCommits(this, other);
        }

        /// <summary>
        /// Returns the changes of this commit vs. it's parent commit(s). Works for the first commit too.
        /// </summary>
        public IEnumerable<Change> Changes
        {
            get
            {
                if (this.Parents.Count() == 0)
                    return CompareCommits(null, this);
                else
                    return Parents.SelectMany(parent => parent.CompareAgainst(this)).ToArray();
            }
        }

        public static Commit Create(string message, Commit parent, Tree tree)
        {
            if (tree==null)
                throw new ArgumentException("tree must not be null");
            var repo=tree.Repository;
            var author=new Author(repo.Config["user.name"], repo.Config["user.email"]);
            return Create(message, parent, tree, author, author, DateTimeOffset.Now);
        }

        public static Commit Create(string message, Commit parent, Tree tree, Author author)
        {
            return Create(message, parent, tree, author, author, DateTimeOffset.Now);
        }

        public static Commit Create(string message, Commit parent, Tree tree, Author author, Author committer, DateTimeOffset time)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("message must not be null or empty");
            if (tree == null)
                throw new ArgumentException("tree must not be null");
            var repo = tree.Repository;
            var corecommit = new CoreCommit(repo._internal_repo);
            if (parent != null)
                corecommit.ParentIds = new GitSharp.Core.ObjectId[] { parent._id };
            corecommit.Author = new GitSharp.Core.PersonIdent(author.Name, author.EmailAddress, time.ToMillisecondsSinceEpoch(), (int)time.Offset.TotalMinutes);
            corecommit.Committer = new GitSharp.Core.PersonIdent(committer.Name, committer.EmailAddress, time.ToMillisecondsSinceEpoch(), (int)time.Offset.TotalMinutes);
            corecommit.Message = message;
            corecommit.TreeEntry = tree.InternalTree;
            corecommit.Encoding = ExtractOverridenEncodingCommitFromConfig(repo);
            corecommit.Save();
            return new Commit(repo, corecommit);
        }

        private static Encoding ExtractOverridenEncodingCommitFromConfig(Repository repository)
        {
            string encodingAlias = repository.Config["i18n.commitencoding"];

            if (encodingAlias == null)
            {
                // No commitencoding has been specified in the config
                return null;
            }

            return Charset.forName(encodingAlias);
        }

        public override string ToString()
        {
            return "Commit[" + ShortHash + "]";
        }
    }
}
