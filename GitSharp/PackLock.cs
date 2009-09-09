/*
 * Copyright (C) 2009, Google Inc.
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.IO;
using GitSharp.Util;

namespace GitSharp
{
    /** Keeps track of a {@link PackFile}'s associated <code>.keep</code> file. */
    public class PackLock
    {
        private readonly FileInfo keepFile;

        /**
         * Create a new lock for a pack file.
         *
         * @param packFile
         *            location of the <code>pack-*.pack</code> file.
         */
        public PackLock(FileInfo packFile)
        {
            string n = packFile.Name;
            string p = packFile.DirectoryName + Path.DirectorySeparatorChar + n.Slice(0, n.Length - 5) + ".keep";
            keepFile = new FileInfo(p);
        }

        /**
         * Create the <code>pack-*.keep</code> file, with the given message.
         *
         * @param msg
         *            message to store in the file.
         * @return true if the keep file was successfully written; false otherwise.
         * @throws IOException
         *             the keep file could not be written.
         */
        public bool Lock(string msg)
        {
            if (msg == null) return false;
            if (!msg.EndsWith("\n")) msg += "\n";
            LockFile lf = new LockFile(keepFile);
            if (!lf.Lock()) return false;
            lf.Write(Constants.encode(msg));
            return lf.Commit();
        }

        /** Remove the <code>.keep</code> file that holds this pack in place. */
        public void Unlock()
        {
            keepFile.Delete();
        }
    }

}