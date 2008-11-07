using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Lib
{
    public class PackIndexV1 : PackIndex
    {

        public PackIndexV1(Stream stream, byte[] hdr)
        {
            throw new NotImplementedException();
        }
        public override IEnumerator<PackIndex.MutableEntry> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override long ObjectCount
        {
            get
            {
                throw new NotImplementedException();
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        public override long Offset64Count
        {
            get
            {
                throw new NotImplementedException();
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        public override ObjectId GetObjectId(long nthPosition)
        {
            throw new NotImplementedException();
        }

        public override long FindOffset(AnyObjectId objId)
        {
            throw new NotImplementedException();
        }

        public override long FindCRC32(AnyObjectId objId)
        {
            throw new NotImplementedException();
        }

        public override bool HasCRC32Support()
        {
            throw new NotImplementedException();
        }
    }
}
