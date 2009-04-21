using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gitty.Core.Transport;

namespace Gitty.Core
{
    public class PackIndexWriterV2 : PackIndexWriter
    {
        public PackIndexWriterV2(Stream output)
            : base(output)
        {
        }

        protected override void WriteInternal()
        {
            WriteTOC(2);
            WriteFanOutTable();
            WriteObjectNames();
            WriteCRCs();
            WriteOffset32();
            WriteOffset64();
            WriteChecksumFooter();
        }

        private void WriteObjectNames()
        {
            foreach (PackedObjectInfo oe in entries)
                _stream.Write(oe);
        }

        private void WriteCRCs()
        {
            foreach (PackedObjectInfo oe in entries)
                _stream.Write(oe.CRC);
        }

        private void WriteOffset32()
        {
            int o64 = 0;
            foreach (PackedObjectInfo oe in entries)
            {
                long o = oe.Offset;
                if (o < int.MaxValue)
                    _stream.Write((int)o);
                else
                    _stream.Write((int)((1 << 31) | o64++));
            }
        }

        private void WriteOffset64()
        {
            foreach (PackedObjectInfo oe in entries)
            {
                long o = oe.Offset;
                if (o > int.MaxValue)
                    _stream.Write(o);
            }
        }
    }
}
