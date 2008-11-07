using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Lib
{
    [Complete]
    public class IndexDiff
    {
        private GitIndex _index;
        private Tree _tree;

        public IndexDiff(Repository repository)
        {
            this._tree = repository.MapTree("HEAD");
            this._index = repository.Index;
        }

        public IndexDiff(Tree tree, GitIndex index)
        {
            this._tree = tree;
            this._index = index;
        }

        private bool anyChanges = false;
        public bool Diff()
        {
            DirectoryInfo root = _index.Repository.WorkingDirectory;
            AbstractIndexTreeVisitor visitor = new AbstractIndexTreeVisitor();
            visitor.VisitEntry = delegate(TreeEntry treeEntry, GitIndex.Entry indexEntry, FileInfo file)
            {
                if (treeEntry == null)
                {
                    this.Added.Add(indexEntry.Name);
                    anyChanges = true;
                }
                else if (indexEntry == null)
                {
                    if (!(treeEntry is Tree))
                        Removed.Add(treeEntry.FullName);
                    anyChanges = true;
                }
                else
                {
                    if (!treeEntry.Id.Equals(indexEntry.ObjectId))
                    {
                        Changed.Add(indexEntry.Name);
                        anyChanges = true;
                    }
                }

                if (indexEntry != null)
                {
                    if (!file.Exists)
                    {
                        Missing.Add(indexEntry.Name);
                        anyChanges = true;
                    }
                    else
                    {
                        if (indexEntry.IsModified(root, true))
                        {
                            Modified.Add(indexEntry.Name);
                            anyChanges = true;
                        }
                    }
                }
            };
            new IndexTreeWalker(_index, _tree, root, visitor).Walk();

            return anyChanges;
        }

        public HashSet<string> Added { get; private set; }
        public HashSet<string> Changed { get; private set; }
        public HashSet<string> Removed { get; private set; }
        public HashSet<string> Missing { get; private set; }
        public HashSet<string> Modified { get; private set; }
    }
}
