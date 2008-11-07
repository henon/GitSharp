using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gitty.Lib;
using System.IO;

namespace Gitty.Exceptions
{
    [global::System.Serializable]
    public class CorruptObjectException : IOException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public CorruptObjectException(ObjectId id, string message) : base(string.Format("Object {0} is corrupt: {1}",id,message)) { }
        public CorruptObjectException(string message) : base(message) { }
        public CorruptObjectException(ObjectId id, string message, Exception inner) : base(string.Format("Object {0} is corrupt: {1}", id, message), inner) { }
        protected CorruptObjectException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
