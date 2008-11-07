using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gitty.Exceptions;

namespace Gitty.Lib
{
    [Complete]
    public class WholePackedObjectLoader : PackedObjectLoader
    {
        public WholePackedObjectLoader(WindowCursor curs, PackFile pr,
                long dataOffset, long objectOffset, ObjectType type, int size)
            : base(curs, pr, dataOffset, objectOffset)
        {

            this._objectType = type;
            this._objectSize = size;
        }

        public override ObjectId GetDeltaBase()
        {
            return null;
        }

        public override byte[] CachedBytes
        {
            get
            {
                if (this.ObjectType != ObjectType.Commit)
                {
                    UnpackedObjectCache.Entry cache = pack.ReadCache(this.DataOffset);
                    if (cache != null)
                    {
                        curs.Release();
                        return cache.Data;
                    }
                }

                try
                {
                    // might not should be down converting this.Size
                    byte[] data = pack.Decompress(this.DataOffset, (int)this.Size, curs);
                    curs.Release();
                    if (this.ObjectType != ObjectType.Commit)
                        pack.SaveCache(this.DataOffset, data, this.ObjectType);
                    return data;
                }
                catch (FormatException fe)
                {
                    throw new CorruptObjectException(this.Id, "bad stream", fe);
                }

            }
        }

        public override ObjectType RawType
        {
            get
            {
                return this.ObjectType;
            }
        }

        public override long RawSize
        {
            get
            {
                return this.Size;
            }
        }
    }
}
