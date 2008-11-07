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
