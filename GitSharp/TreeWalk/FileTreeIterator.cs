/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
namespace GitSharp.TreeWalk
{


    /**
     * Working directory iterator for standard Java IO.
     * <p>
     * This iterator uses the standard <code>java.io</code> package to read the
     * specified working directory as part of a {@link TreeWalk}.
     */
    public class FileTreeIterator : WorkingTreeIterator
    {
        private DirectoryInfo directory;

        /**
         * Create a new iterator to traverse the given directory and its children.
         * 
         * @param root
         *            the starting directory. This directory should correspond to
         *            the root of the repository.
         */
        public FileTreeIterator(DirectoryInfo root)
        {
            directory = root;
            init(entries());
        }

        /**
         * Create a new iterator to traverse a subdirectory.
         * 
         * @param p
         *            the parent iterator we were created from.
         * @param root
         *            the subdirectory. This should be a directory contained within
         *            the parent directory.
         */
        public FileTreeIterator(FileTreeIterator p, DirectoryInfo root)
            : base(p)
        {
            directory = root;
            init(entries());
        }

        public override AbstractTreeIterator createSubtreeIterator(Repository repo)
        {
            return new FileTreeIterator(this, ((FileEntry)current()).file as DirectoryInfo);
        }

        private Entry[] entries()
        {
            FileInfo[] all = directory.GetFiles();
            if (all == null)
                return EOF;
            Entry[] r = new Entry[all.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = new FileEntry(all[i]);
            return r;
        }

        /**
         * Wrapper for a standard file
         */
        public class FileEntry : Entry
        {
            public FileSystemInfo file;

            private FileMode mode;

            private long Length = -1;

            private long lastModified;

            public FileEntry(DirectoryInfo f)
            {
                file = f;

                if (new DirectoryInfo(f + "/.git").Exists)
                    mode = FileMode.GitLink;
                else
                    mode = FileMode.Tree;
            }

            public FileEntry(FileInfo f)
            {
                file = f;
                if (FS.canExecute(f))
                    mode = FileMode.ExecutableFile;
                else
                    mode = FileMode.RegularFile;
            }

            public override FileMode getMode()
            {
                return mode;
            }

            public override string getName()
            {
                return file.Name;
            }

            public override long getLength()
            {
                if (file is DirectoryInfo)
                    return 0;
                if (Length < 0)
                    Length = (file as FileInfo).Length;
                return Length;
            }

            public override long getLastModified()
            {
                if (lastModified == 0)
                    lastModified = file.LastWriteTime.Ticks;
                return lastModified;
            }

            public override FileStream openInputStream()
            {
                return (file as FileInfo).Open(System.IO.FileMode.Open, FileAccess.Read);
            }

            /**
             * Get the underlying file of this entry.
             *
             * @return the underlying file of this entry
             */
            public FileSystemInfo getFile()
            {
                return file;
            }
        }
    }
}