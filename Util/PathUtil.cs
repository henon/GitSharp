/*
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Core.Util
{
    public sealed class PathUtil
    {
        public static string Combine(params string[] paths)
        {
            if (paths.Length < 2)
                throw new ArgumentException("Must have at least two paths", "paths");

            string path = paths[0];
            for (int i = 0; i < paths.Length; ++i)
            {
                path = Path.Combine(path, paths[i]);
            }
            return path;
        }


        public static DirectoryInfo CombineDirectoryPath(DirectoryInfo path, string subdir)
        {
            return new DirectoryInfo(Path.Combine(path.FullName, subdir));
        }

        public static FileInfo CombineFilePath(DirectoryInfo path, string filename)
        {
            return new FileInfo(Path.Combine(path.FullName, filename));
        }

        /// <summary>
        /// Delete file without complaining about readonly status
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteFile(FileInfo path)
        {
            DeleteFile(path.FullName);
        }

        /// <summary>
        /// Delete file without complaining about readonly status
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteFile(string path)
        {
            //Check the file actually exists
            if (File.Exists(path))
            {
                //If its readonly set it back to normal
                //Need to "AND" it as it can also be archive, hidden etc 
                if ((File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    File.SetAttributes(path, FileAttributes.Normal);
                //Delete the file
                File.Delete(path);
            }
        }
    }
}
