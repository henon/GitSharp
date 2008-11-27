/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Gitty.Lib
{
    [Complete]
    public class InflaterCache
    {

        private static int SZ = 4;
        
        private static Inflater[] inflaterCache;

        private static int openInflaterCount;

        static InflaterCache()
        {
            inflaterCache = new Inflater[SZ];
        }

        /**
         * Obtain an Inflater for decompression.
         * <p>
         * Inflaters obtained through this cache should be returned (if possible) by
         * {@link #release(Inflater)} to avoid garbage collection and reallocation.
         * 
         * @return an available inflater. Never null.
         */
        public static Inflater GetInflater()
        {
            lock (typeof(InflaterCache))
            {
                if (openInflaterCount > 0)
                {
                    Inflater r = inflaterCache[--openInflaterCount];
                    inflaterCache[openInflaterCount] = null;
                    return r;
                }
                return new Inflater(false);
            }
        }

        /**
         * Release an inflater previously obtained from this cache.
         * 
         * @param i
         *            the inflater to return. May be null, in which case this method
         *            does nothing.
         */
        public static void Release(Inflater i)
        {
            if (i == null)
                return;
            
            i.Reset();

            lock (typeof(InflaterCache))
            {
                if (openInflaterCount == SZ)
                    return;
                else
                    inflaterCache[openInflaterCount++] = i;
            }
        }
        
        private InflaterCache()
        {
            throw new InvalidOperationException();
        }
    }
}
