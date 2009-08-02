/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp
{

    /**
     * Fast, efficient map specifically for {@link ObjectId} subclasses.
     * <p>
     * This map provides an efficient translation from any ObjectId instance to a
     * cached subclass of ObjectId that has the same value.
     * <p>
     * Raw value equality is tested when comparing two ObjectIds (or subclasses),
     * not reference equality and not <code>.Equals(Object)</code> equality. This
     * allows subclasses to override <code>Equals</code> to supply their own
     * extended semantics.
     * 
     * @param <V>
     *            type of subclass of ObjectId that will be stored in the map.
     */
    public class ObjectIdSubclassMap<V> //: IEnumerable<V> 
        where V : ObjectId
    {
        private int _size;

        private V[] obj_hash;

        /** Create an empty map. */
        public ObjectIdSubclassMap()
        {
            obj_hash = new V[32];
        }

        /** Remove all entries from this map. */
        public void clear()
        {
            _size = 0;
            obj_hash = new V[32];
        }

        /**
         * Lookup an existing mapping.
         * 
         * @param toFind
         *            the object identifier to find.
         * @return the instance mapped to toFind, or null if no mapping exists.
         */
        public V get(AnyObjectId toFind)
        {
            int i = index(toFind);
            V obj;

            while ((obj = obj_hash[i]) != null)
            {
                if (AnyObjectId.Equals(obj, toFind))
                    return obj;
                if (++i == obj_hash.Length)
                    i = 0;
            }
            return null;
        }

        /**
         * Store an object for future lookup.
         * <p>
         * An existing mapping for <b>must not</b> be in this map. Callers must
         * first call {@link #get(AnyObjectId)} to verify there is no current
         * mapping prior to adding a new mapping.
         * 
         * @param newValue
         *            the object to store.
         * @param
         *            <Q>
         *            type of instance to store.
         */
        public void add(V newValue)
        {
            if (obj_hash.Length - 1 <= _size * 2)
                grow();
            insert(newValue);
            _size++;
        }

        /**
         * @return number of objects in map
         */
        public int size()
        {
            return _size;
        }

        public Iterator<V> iterator()
        {
            return new Iterator<V>(this);
        }

        public class Iterator<T>  // [henon] todo: change to implement IEnumerator
            where T : V
        {
            public Iterator(ObjectIdSubclassMap<V> map)
            {
                this.map = map;
            }

            private int found;
            private ObjectIdSubclassMap<V> map;
            private int i;

            public bool hasNext()
            {
                return found < map._size;
            }

            public T next()
            {
                while (i < map.obj_hash.Length)
                {
                    T v = (T)map.obj_hash[i++];
                    if (v != null)
                    {
                        found++;
                        return v;
                    }
                }
                throw new InvalidOperationException();
            }

            public void remove()
            {
                throw new NotSupportedException();
            }
        }

        //#region IEnumerable<V> Members

        //public IEnumerator<V>  GetEnumerator()
        //{
        //    throw new NotImplementedException();
        //}

        //#endregion

        //#region IEnumerable Members

        //System.Collections.IEnumerator  System.Collections.IEnumerable.GetEnumerator()
        //{
        //    throw new NotImplementedException();
        //}

        //#endregion


        private int index(AnyObjectId id)
        {
            return (int)((uint)id.W1 >> 1) % obj_hash.Length;
        }

        private void insert(V newValue)
        {
            int j = index(newValue);
            while (obj_hash[j] != null)
            {
                if (++j >= obj_hash.Length)
                    j = 0;
            }
            obj_hash[j] = newValue;
        }

        private void grow()
        {
            V[] old_hash = obj_hash;
            int old_hash_size = obj_hash.Length;

            obj_hash = createArray(2 * old_hash_size);
            for (int i = 0; i < old_hash_size; i++)
            {
                V obj = old_hash[i];
                if (obj != null)
                    insert(obj);
            }
        }

        //@SuppressWarnings("unchecked")
        private V[] createArray(int sz)
        {
            return (V[])new ObjectId[sz];
        }
    }
}
