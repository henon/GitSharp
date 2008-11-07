using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    public abstract class PackIndexWriter
    {
        internal static byte[] TOC = { 255, (byte)'t', (byte)'O', (byte)'c' };
    }
}
