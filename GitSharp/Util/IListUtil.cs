using System.Collections.Generic;

namespace GitSharp.Util
{

    public static class IListUtil
    {
        public static bool isEmpty<T>(this IList<T> l)
        {
            return l.Count == 0;
        }
    }

}