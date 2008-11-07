using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace Gitty.Lib
{
    public class WindowedFile
    {
        public WindowedFile(FileInfo packFile)
        {

        }
        public ThreadStart OnOpen { get; set; }

        internal void Close()
        {
            throw new NotImplementedException();
        }

        internal void ReadCompressed(long position, byte[] dstbuf, WindowCursor curs)
        {
            throw new NotImplementedException();
        }

        internal void CopyToStream(long dataOffset, byte[] buf, int cnt, Stream stream, WindowCursor curs)
        {
            throw new NotImplementedException();
        }

        internal void ReadFully(long position, byte[] intbuf, WindowCursor curs)
        {
            throw new NotImplementedException();
        }

        internal int Read(long objectOffset, byte[] buf, int p, int toRead, WindowCursor curs)
        {
            throw new NotImplementedException();
        }

        internal int Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

		internal int Read(long position, byte[] sig, WindowCursor curs)
		{
			throw new NotImplementedException();
		}

		internal string Name
		{
			get { throw new NotImplementedException(); }
		}
	}
}
