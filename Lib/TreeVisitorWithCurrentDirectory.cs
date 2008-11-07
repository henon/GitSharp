using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace Gitty.Lib
{
    [Complete]
    public abstract class TreeVisitorWithCurrentDirectory : TreeVisitor
    {
        private Stack<DirectoryInfo> stack;

        private DirectoryInfo currentDirectory;

        protected TreeVisitorWithCurrentDirectory(DirectoryInfo rootDirectory)
        {
            stack = new Stack<DirectoryInfo>(16);
            currentDirectory = rootDirectory;
        }

        protected DirectoryInfo GetCurrentDirectory()
        {
            return currentDirectory;
        }


        #region TreeVisitor Members

        public void StartVisitTree(Tree t)
        {
            stack.Push(currentDirectory);
            if (!t.IsRoot)
            {
                currentDirectory = new DirectoryInfo(Path.Combine(currentDirectory.FullName, t.Name));
            }
        }

        public virtual void EndVisitTree(Tree t)
        {
            currentDirectory = stack.Pop();
        }

        public abstract void VisitFile(FileTreeEntry f);

        public abstract void VisitSymlink(SymlinkTreeEntry s);

        #endregion
    }
}
