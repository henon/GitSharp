using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Core.Transport
{
    public class LongMap<V>
    {
        Dictionary<long, V> _map = new Dictionary<long, V>();

        public bool containsKey(long key)
        {
            return _map.ContainsKey(key);
        }

        public V get(long key)
        {
            return _map.GetValue(key);
        }

        public V remove(long key)
        {
            return _map.RemoveValue(key);
        }

        public V put(long key, V value)
        {
            return _map.put(key, value);
        }
    }
}
