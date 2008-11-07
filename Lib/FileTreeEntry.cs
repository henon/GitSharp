using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    public class FileTreeEntry : TreeEntry 
    {
        public FileTreeEntry(Tree parent, ObjectId id, byte[] nameUTF8, bool execute)
            : base(parent,id, nameUTF8)
        {
            this.SetExecutable(execute);
        }

        private FileMode _mode;
        public override FileMode Mode
        {
            get { return _mode ; }
        }

        public override void Accept(TreeVisitor tv, int flags)
        {
            if ((MODIFIED_ONLY & flags) == MODIFIED_ONLY && !IsModified)
                return;

            tv.VisitFile(this);
        }

        public bool IsExecutable
        {
            get
            {
                return this.Mode == FileMode.ExecutableFile;
            }
        }

        public void SetExecutable(bool execute)
        {
            _mode = execute ? FileMode.ExecutableFile : FileMode.RegularFile;
        }

        public ObjectLoader OpenReader()
        {
            return this.Repository.OpenBlob(this.Id);
        }

        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            r.Append(ObjectId.ToString(this.Id));
            r.Append(' ');
            r.Append(this.IsExecutable ? 'X' : 'F');
            r.Append(' ');
            r.Append(this.FullName);
            return r.ToString();
        }
    }
}
