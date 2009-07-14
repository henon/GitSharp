/*
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

namespace GitSharp.Util
{
    /// <summary>
    /// Java style iterator with remove capability (which is not supported by IEnumerator).
    /// This iterator is able to iterate over a list without being corrupted by removal of elements
    /// via the remove() method.
    /// </summary>
    public class ListIterator<T>
    {
        protected List<T> list;
        protected int index = -1;
        protected bool can_remove = false;

        public ListIterator(List<T> list)
        {
            this.list = list;
        }

        public virtual bool hasNext()
        {
            if (index >= list.Count - 1)
                return false;
            return true;
        }

        public virtual T next()
        {
            if (index >= list.Count)
                throw new InvalidOperationException();
            can_remove = true;
            return list[index++];
        }

        public virtual void remove()
        {
            if (index >= list.Count || index == -1 )
                throw new InvalidOperationException("Index is out of bounds of underlying list! "+index);
             if (!can_remove)
                 throw new InvalidOperationException("Can not remove (twice), call next first!");
             can_remove = false; // <--- remove can only be called once per call to next
            list.RemoveAt(index);
            index--;
        }
    }
}
