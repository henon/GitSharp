using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Exceptions
{
    [global::System.Serializable]
    public class RevisionSyntaxException : IOException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //
        public string Revision { get; private set; }
        public RevisionSyntaxException(string revstr)
        {
            this.Revision = revstr;
        }

        public RevisionSyntaxException(string revstr, string message)
            : base(message)
        {
            this.Revision = revstr;
        }


        public RevisionSyntaxException(string message, Exception inner) : base(message, inner) { }
        protected RevisionSyntaxException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public override string ToString()
        {
            return base.ToString() + ":" + this.Revision;
        }
    }
}
