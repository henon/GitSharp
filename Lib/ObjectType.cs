using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    public enum ObjectType
    {
        Bad = -1, 
        Extension = 0,
        Commit = 1,
        Tree = 2,
        Blob = 3,
        Tag = 4,
        ObjectType5 = 5,
		OFSDelta = 6,
		RefDelta = 7
    }
}
