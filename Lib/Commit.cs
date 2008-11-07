using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    public class Commit : Treeish
    {
        public Commit(Repository db, ObjectId id, byte[] raw) 
        {
            throw new NotImplementedException();
        }

        #region Treeish Members

        public ObjectId GetTreeId()
        {
            throw new NotImplementedException();
        }

        public Tree GetTree()
        {
            throw new NotImplementedException();
        }
        
        #endregion

        public ObjectId[] ParentIds { get; set; }
    }
}
