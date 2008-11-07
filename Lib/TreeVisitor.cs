using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    [Complete]
    public interface TreeVisitor
    {
        /**
         * Visit to a tree node before child nodes are visited.
         *
         * @param t
         *            Tree
         * @throws IOException
         */
        void StartVisitTree(Tree t);

        /**
         * Visit to a tree node. after child nodes have been visited.
         *
         * @param t Tree
         * @throws IOException
         */
        void EndVisitTree(Tree t);

        /**
         * Visit to a blob.
         *
         * @param f Blob
         * @throws IOException
         */
        void VisitFile(FileTreeEntry f);

        /**
         * Visit to a symlink.
         *
         * @param s Symlink entry
         * @throws IOException
         */
        void VisitSymlink(SymlinkTreeEntry s);
    }
}
