using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GitSharp.Util
{
    public class DigestOutputStream
    {
        FileStream m_stream;
        MessageDigest m_digest;

        public DigestOutputStream(MessageDigest digest, FileStream stream)
        {
            m_digest = digest;
            m_stream = stream;
        }

        public void Write(byte[] bytes, int offset, int count)
        {
            m_digest.Update(bytes, offset, count);
            m_stream.Write(bytes, offset, count);
        }
    }
}
