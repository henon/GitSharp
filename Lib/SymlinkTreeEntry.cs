using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    [Complete]
    public class SymlinkTreeEntry : TreeEntry
    {
        private static long serialVersionUID = 1L;

        public SymlinkTreeEntry(Tree parent, ObjectId id, byte[] nameUTF8)
            : base(parent, id, nameUTF8)
        {
        }

        public override FileMode Mode
        {
            get
            {
                return FileMode.Symlink;
            }
        }

        public override void Accept(TreeVisitor tv, int flags)
        {
            if ((MODIFIED_ONLY & flags) == MODIFIED_ONLY && !this.IsModified)
            {
                return;
            }

            tv.VisitSymlink(this);
        }

        public override String ToString()
        {
            StringBuilder r = new StringBuilder();
            r.Append(ObjectId.ToString(this.Id));
            r.Append(" S ");
            r.Append(this.FullName);
            return r.ToString();
        }
    }
}
