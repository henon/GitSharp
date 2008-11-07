using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    public class UnpackedObjectLoader : ObjectLoader 
    {
        public UnpackedObjectLoader(Repository repo, ObjectId objectId)
        {

        }

        public override ObjectType ObjectType
        {
            get { throw new NotImplementedException(); }
        }

        public override long Size
        {
            get { throw new NotImplementedException(); }
        }

        public override byte[] Bytes
        {
            get { throw new NotImplementedException(); }
        }

        public override byte[] CachedBytes
        {
            get { throw new NotImplementedException(); }
        }

        public override ObjectType RawType
        {
            get { throw new NotImplementedException(); }
        }

        public override long RawSize
        {
            get { throw new NotImplementedException(); }
        }
    }
}
