using System;
using System.IO;

namespace Gitty.Lib
{
    [Complete]
    public abstract class PackedObjectLoader : ObjectLoader
    {
        protected PackFile pack;

        internal WindowCursor curs;

        internal long objectOffset;


        public PackedObjectLoader(WindowCursor c, PackFile pr, long dataOffset, long objectOffset)
        {
            this.curs = c;
            this.pack = pr;
            this.DataOffset = dataOffset;
            this.objectOffset = objectOffset;
        }

        /**
         * @return offset of object data within pack file
         */
        public long DataOffset { get; protected set; }

        public override byte[] Bytes
        {
            get
            {
                byte[] data = this.CachedBytes;
                byte[] copy = new byte[data.Length];
                Array.Copy(data, 0, copy, 0, data.Length);
                return data;
            }
        }
        /**
         * Copy raw object representation from storage to provided output stream.
         * <p>
         * Copied data doesn't include object header. User must provide temporary
         * buffer used during copying by underlying I/O layer.
         * </p>
         *
         * @param out
         *            output stream when data is copied. No buffering is guaranteed.
         * @param buf
         *            temporary buffer used during copying. Recommended size is at
         *            least few kB.
         * @throws IOException
         *             when the object cannot be read.
         */
        public void CopyRawData(Stream o, byte[] buf)
        {
            pack.CopyRawData(this, o, buf);
        }

        /**
         * @return true if this loader is capable of fast raw-data copying basing on
         *         compressed data checksum; false if raw-data copying needs
         *         uncompressing and compressing data
         */
        public bool SupportsFastCopyRawData()
        {
            return pack.SupportsFastCopyRawData();
        }

        /**
         * @return id of delta base object for this object representation. null if
         *         object is not stored as delta.
         * @throws IOException
         *             when delta base cannot read.
         */
        public abstract ObjectId GetDeltaBase();

        protected ObjectType _objectType;
        public override ObjectType ObjectType
        {
            get
            {
                return _objectType;
            }
        }

        protected long _objectSize;
        public override long Size
        {
            get
            {
                return _objectSize;
            }
        }

    }
}
