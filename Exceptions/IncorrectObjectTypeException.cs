using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gitty.Lib;

namespace Gitty.Exceptions
{
    [global::System.Serializable]
    public class IncorrectObjectTypeException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public IncorrectObjectTypeException(ObjectId id, ObjectType type) 
            : base (string.Format("Object {0} is not a {1}.", id.ToString(),type))
        { 

        }
        public IncorrectObjectTypeException(ObjectId id, ObjectType type, Exception inner) : base(string.Format("Object {0} is not a {1}.", id.ToString(), type), inner) { }
        protected IncorrectObjectTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
