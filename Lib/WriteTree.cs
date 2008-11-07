using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gitty.Util;
using Gitty.Exceptions;

namespace Gitty.Lib
{
    [Complete]
    public class WriteTree : TreeVisitorWithCurrentDirectory
    {
        private ObjectWriter ow;
        public WriteTree(DirectoryInfo sourceDirectory, Repository db)
            : base(sourceDirectory)
        {
            ow = new ObjectWriter(db);
        }

        public override void VisitFile(FileTreeEntry f)
        {
            f.Id = ow.WriteBlob(PathUtil.CombineFilePath(GetCurrentDirectory(), f.Name));
        }

        public override void VisitSymlink(SymlinkTreeEntry s)
        {
            if (s.IsModified)
            {
                throw new SymlinksNotSupportedException("Symlink \""
                        + s.FullName
                        + "\" cannot be written as the link target"
                        + " cannot be read from within Java.");
            }
        }

        public override void EndVisitTree(Tree t)
        {
            base.EndVisitTree(t);            
            t.Id = ow.WriteTree(t);
        }

    }
}
