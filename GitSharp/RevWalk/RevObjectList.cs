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

namespace GitSharp.RevWalk
{


    /**
     * An ordered list of {@link RevObject} subclasses.
     * 
     * @param <E>
     *            type of subclass of RevObject the list is storing.
     */
    public class RevObjectList<E> : IEnumerable<E> // [henon] was AbstractList
    where E : RevObject
    {
        public static int BLOCK_SHIFT = 8;

        public static int BLOCK_SIZE = 1 << BLOCK_SHIFT;

        public Block contents;

        public int _size;

        /** Create an empty object list. */
        public RevObjectList()
        {
            clear();
        }

        public void add(int index, E element)
        {
            if (index != _size)
                throw new InvalidOperationException("Not add-at-end: " + index);
            set(index, element);
            _size++;
        }

        public void add(E element)
        {
            add(_size, element);
        }

        public E set(int index, E element)
        {
            Block s = contents;
            while (index >> s.shift >= BLOCK_SIZE)
            {
                s = new Block(s.shift + BLOCK_SHIFT);
                s.contents[0] = contents;
                contents = s;
            }
            while (s.shift > 0)
            {
                int i = index >> s.shift;
                index -= i << s.shift;
                if (s.contents[i] == null)
                    s.contents[i] = new Block(s.shift - BLOCK_SHIFT);
                s = (Block)s.contents[i];
            }
            object old = s.contents[index];
            s.contents[index] = element;
            return (E)old;
        }

        public E get(int index)
        {
            Block s = contents;
            if (index >> s.shift >= 1024)
                return null;
            while (s != null && s.shift > 0)
            {
                int i = index >> s.shift;
                index -= i << s.shift;
                s = (Block)s.contents[i];
            }
            return s != null ? (E)s.contents[index] : null;
        }

        public int size()
        {
            return _size;
        }

        public virtual void clear()
        {
            contents = new Block(0);
            _size = 0;
        }

        public class Block
        {
            public object[] contents = new object[BLOCK_SIZE];

            public int shift;

            public Block(int s)
            {
                shift = s;
            }
        }

        #region IEnumerable<E> Members

        public IEnumerator<E> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}