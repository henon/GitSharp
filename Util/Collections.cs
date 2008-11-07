using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Util
{
    public sealed class Collections
    {
        public static void AddOrInsert<K,V>(Dictionary<K,V> dict,K key, V value)            
        {
            if (dict.ContainsKey(key))
                dict[key] = value;
            else
                dict.Add(key, value);
        }
        /// <summary>
        /// Returns a value from a dictionary or the values default
        /// </summary>
        /// <typeparam name="K">Key Type</typeparam>
        /// <typeparam name="V">Value Type</typeparam>
        /// <param name="dict">dictionary to search</param>
        /// <param name="key">Key to search for</param>
        /// <returns>default(V) or item if Key is found</returns>
        public static V GetValue<K, V>(Dictionary<K, V> dict, K key)
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
    }
}
