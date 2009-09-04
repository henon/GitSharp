using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Transport;
using System.IO;
using GitSharp.Util;

namespace GitSharp
{
    public class PackIndexWriterV1 : PackIndexWriter
    {
        public static bool CanStore(PackedObjectInfo objectInfo)
        {
            // We are limited to 4 GB per pack as offset is 32 bit unsigned int.
            //
            return objectInfo.Offset.UnsignedRightShift(1) < int.MaxValue;
        }


        public PackIndexWriterV1(Stream output)
            : base(output)
        {
        }


        internal override void WriteInternal()
        {
            WriteFanOutTable();

            foreach (PackedObjectInfo oe in entries)
            {
                if (!CanStore(oe))
                    throw new IOException("Pack too large for index version 1");
                _stream.Write((int)oe.Offset);
				_stream.Write(oe);
            }

            WriteChecksumFooter();
        }
    }
}
