using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    [Complete]
    public class ForceModified : TreeVisitor
    {
        public void StartVisitTree(Tree t)
        {
            t.SetModified();
        }

        public void EndVisitTree(Tree t)
        {
            // Nothing to do.
        }

        public void VisitFile(FileTreeEntry f)
        {
            f.SetModified();
        }

        public void VisitSymlink(SymlinkTreeEntry s)
        {
            // TODO: handle symlinks. Only problem is that JGit is independent of
            // Eclipse
            // and Pure Java does not know what to do about symbolic links.
        }
    }
}
