/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;

namespace Gitty.Lib
{

    /**
     * Very much like a map, but specialized to partition the data on the first byte
     * of the key. This is about twice as fast. See test class.
     *
     * Inspiration is taken from the Git pack file format which uses this technique
     * to improve lookup performance.
     *
     * @param <V>
     *            The value we map ObjectId's to.
     *
     */
    public class ObjectIdMap<V> : IDictionary<ObjectId, V>
    {
        IDictionary<ObjectId, V>[] level0 = new Dictionary<ObjectId, V>[256];

        /**
         * Construct an ObjectIdMap with an underlying TreeMap implementation
         */
        public ObjectIdMap()
            : this(new Dictionary<ObjectId, V>())
        {

        }
        /**
         * Construct an ObjectIdMap with the same underlying map implementation as
         * the provided example.
         *
         * @param sample
         */
        public ObjectIdMap(IDictionary<ObjectId, V> sample)
        {
            try
            {
                //MethodInfo m = sample.GetType().GetMethod("Clone");
                //for (int i = 0; i < 256; ++i)
                //{
                //    level0[i] = (IDictionary<ObjectId, V>)m.Invoke(sample, null);
                //}

                for (int i = 0; i < 256; ++i)
                {
                    level0[i] = new Dictionary<ObjectId, V>();
                }
                
            }
            catch (NullReferenceException e)
            {
                throw new ArgumentException(string.Empty, e);
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException(string.Empty, e);
            }
            catch (TargetInvocationException e)
            {
                throw new ArgumentException(string.Empty, e);
            }
            catch (SecurityException e)
            {
                throw new ArgumentException(string.Empty, e);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < 256; ++i)
                level0[i].Clear();
        }

        private IDictionary<ObjectId, V> Submap(ObjectId key)
        {
            return level0[key.GetFirstByte()];
        }


        #region IDictionary<ObjectId,V> Members

        public void Add(ObjectId key, V value)
        {
            this[key] = value;
        }

        public bool ContainsKey(ObjectId key)
        {
            return Submap(key).ContainsKey(key);
        }

        public ICollection<ObjectId> Keys
        {
            get { throw new NotSupportedException(); }
        }

        public bool Remove(ObjectId key)
        {
            return Submap(key).Remove(key);
        }

        public bool TryGetValue(ObjectId key, out V value)
        {
            throw new NotSupportedException();
        }

        public ICollection<V> Values
        {
            get { throw new NotSupportedException(); }
        }

        public V this[ObjectId key]
        {
            get
            {
                return Submap(key)[key];
            }
            set
            {
                var submap = Submap(key);
                if (submap.ContainsKey(key))
                    submap[key] = value;
                else
                    submap.Add(key, value);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<ObjectId,V>> Members

        public void Add(KeyValuePair<ObjectId, V> item)
        {
            this[item.Key] = item.Value;
        }

        public bool Contains(KeyValuePair<ObjectId, V> item)
        {
            return Submap(item.Key).Contains(item);
        }

        public void CopyTo(KeyValuePair<ObjectId, V>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get
            {
                int ret = 0;
                for (int i = 0; i < 256; ++i)
                    ret += level0[i].Count;
                return ret;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<ObjectId, V> item)
        {
            return Submap(item.Key).Remove(item);
        }

        #endregion

        #region IEnumerable<KeyValuePair<ObjectId,V>> Members

        public IEnumerator<KeyValuePair<ObjectId, V>> GetEnumerator()
        {
            return new ObjectIdMapEnumerator(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ObjectIdMapEnumerator(this);
        }

        #endregion

        private class ObjectIdMapEnumerator : IEnumerator<KeyValuePair<ObjectId, V>>
        {
            private int levelIndex;
            private IEnumerator<KeyValuePair<ObjectId, V>> levelIterator;
            private ObjectIdMap<V> map;

            public ObjectIdMapEnumerator(ObjectIdMap<V> Map)
            {
                map = Map;
                levelIterator = map.level0[levelIndex].GetEnumerator();
            }


            #region IEnumerator<KeyValuePair<ObjectId,V>> Members

            public KeyValuePair<ObjectId, V> Current
            {
                get
                {
                    return levelIterator.Current;
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (levelIterator == null) return false;

                while (!levelIterator.MoveNext())
                {
                    if (levelIndex < map.level0.Length-1)
                    {
                        levelIndex++;
                        levelIterator = map.level0[levelIndex].GetEnumerator();
                    }
                    else
                    {
                        levelIterator = null;
                        return false;
                    }
                }
                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            #endregion
        }
    }
}
