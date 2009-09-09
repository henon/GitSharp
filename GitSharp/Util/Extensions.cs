/*
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.IO;
using System.Security;

namespace GitSharp
{
    public static class Extensions
    {
        private static string alphaNumeric = "abcdefghijklmnopqrstuvwxyz0123456789";

        public static long UnsignedRightShift(this long n, int s) //Overloaded function where n is a long
        {
            if (n > 0)
            {
                return n >> s;
            }
            
            return (n >> s) + (((long) 2) << ~s);
        }

        public static int UnsignedRightShift(this int n, int s)
        {
            if (n > 0)
            {
                return n >> s;
            }
            
            return (n >> s) + (2 << ~s);
        }

        /// <summary>
        /// Adds or replaces the a value based on a key.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dict">The dict.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void AddOrReplace<K, V>(this Dictionary<K, V> dict, K key, V value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }

        /// <summary>
        /// Returns a value from a dictionary or the values default
        /// </summary>
        /// <typeparam name="K">Key Type</typeparam>
        /// <typeparam name="V">Value Type</typeparam>
        /// <param name="dict">dictionary to search</param>
        /// <param name="key">Key to search for</param>
        /// <returns>default(V) or item if Key is found</returns>
        public static V GetValue<K, V>(this Dictionary<K, V> dict, K key)
        {
            try
            {
                return dict[key];
            }
            catch (Exception)
            {
                return default(V);
            }
        }

        public static void Write(this BinaryWriter writer, ObjectId o)
        {
            o.CopyTo(writer);
        }

        public static FileInfo CreateTempFile(this DirectoryInfo d, string prefix)
        {
            return CreateTempFile(d, prefix, null);
        }

        public static FileInfo CreateTempFile(this DirectoryInfo d, string prefix, string suffix)
        {
            string name = string.Empty;
            var rnd = new Random((int) DateTime.Now.Ticks);
            if (!string.IsNullOrEmpty(prefix))
                name += prefix;

            int i = 8;
            while (i-- > 0)
                name += alphaNumeric[rnd.Next(alphaNumeric.Length - 1)];

            if (suffix == null)
                name += ".tmp";
            else
                name += suffix;

            return new FileInfo(Path.Combine(d.FullName, name));
        }

        public static bool RenameTo(this FileInfo file, string newPath)
        {
            try
            {
                file.MoveTo(newPath);
                return true;
            }
            catch (IOException)
            {
            }
            catch (ArgumentNullException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (NotSupportedException)
            {
            }

            return false;
        }

        public static string DirectoryName(this FileSystemInfo fileSystemInfo)
        {
            return fileSystemInfo.FullName;
        }

        public static bool IsDirectory(this FileSystemInfo fileSystemInfo)
        {
            return Directory.Exists(DirectoryName(fileSystemInfo));
        }

        public static bool IsFile(this FileSystemInfo fileSystemInfo)
        {
            return File.Exists(fileSystemInfo.FullName);
        } 
    }
}