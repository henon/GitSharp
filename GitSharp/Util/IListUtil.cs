using System.Collections.Generic;

namespace GitSharp.Core.Util
{
    public static class IListUtil
    {
        public static bool isEmpty<T>(this IList<T> l)
        {
            return l.Count == 0;
        }

        public static bool isEmpty<TK, TV>(this IDictionary<TK, TV> d)
        {
            return (d.Count == 0);
        }
    }
}