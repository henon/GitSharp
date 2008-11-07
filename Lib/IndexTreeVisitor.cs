using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Lib
{
    [Complete]
    public interface IndexTreeVisitor
    {
        /**
 * Visit a blob, and corresponding tree and index entries.
 *
 * @param treeEntry
 * @param indexEntry
 * @param file
 * @throws IOException
 */
        void VisitEntry(TreeEntry treeEntry, GitIndex.Entry indexEntry, FileInfo file);

        /**
         * Visit a blob, and corresponding tree nodes and associated index entry.
         *
         * @param treeEntry
         * @param auxEntry
         * @param indexEntry
         * @param file
         * @throws IOException
         */
        void VisitEntry(TreeEntry treeEntry, TreeEntry auxEntry, GitIndex.Entry indexEntry, FileInfo file);

        /**
         * Invoked after handling all child nodes of a tree, during a three way merge
         *
         * @param tree
         * @param auxTree
         * @param curDir
         * @throws IOException
         */
        void FinishVisitTree(Tree tree, Tree auxTree, String curDir);

        /**
         * Invoked after handling all child nodes of a tree, during two way merge.
         *
         * @param tree
         * @param i
         * @param curDir
         * @throws IOException
         */
        void FinishVisitTree(Tree tree, int i, String curDir);
    }
}
