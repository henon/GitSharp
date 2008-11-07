using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Exceptions
{
    [global::System.Serializable]
    public class SymlinksNotSupportedException : IOException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SymlinksNotSupportedException() { }
        public SymlinksNotSupportedException(string message) : base(message) { }
        public SymlinksNotSupportedException(string message, Exception inner) : base(message, inner) { }
        protected SymlinksNotSupportedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
