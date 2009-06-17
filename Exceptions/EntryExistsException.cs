using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Exceptions
{
    public class EntryExistsException : Exception
    {
        public EntryExistsException(string name)
            : base(string.Format("Tree entry \"{0}\" already exists.", name))
        {
        }

    }
}
