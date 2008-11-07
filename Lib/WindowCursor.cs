using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    public class WindowCursor
    {
		internal byte[] tempId = new byte[ObjectId.Constants.ObjectIdLength];

        internal void Release()
        {
            throw new NotImplementedException();
        }
    }
}
