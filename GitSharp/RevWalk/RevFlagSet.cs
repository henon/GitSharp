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
using System.Linq;
using System.Collections.Generic;
using GitSharp.Util;

namespace GitSharp.RevWalk
{

    /**
     * Multiple application level mark bits for {@link RevObject}s.
     * 
     * @see RevFlag
     */
    public class RevFlagSet // [henon] was derived from AbstractSet<RevFlag> 
    //TODO: implement C# interfaces IEnumreable and the likes ..
    {
        public int mask;

        private List<RevFlag> active;

        /** Create an empty set of flags. */
        public RevFlagSet()
        {
            active = new List<RevFlag>();
        }

        /**
         * Create a set of flags, copied from an existing set.
         * 
         * @param s
         *            the set to copy flags from.
         */
        public RevFlagSet(RevFlagSet s)
        {
            mask = s.mask;
            active = new List<RevFlag>(s.active);
        }

        /**
         * Create a set of flags, copied from an existing collection.
         * 
         * @param s
         *            the collection to copy flags from.
         */
        public RevFlagSet(IEnumerable<RevFlag> s)
            : this()
        {
            foreach (var f in s)
                Add(f);
        }

        public bool Contains(object o)
        {
            if (o is RevFlag)
                return (mask & ((RevFlag)o).mask) != 0;
            return false;
        }

        public bool ContainsAll(IEnumerable<RevFlag> c) // [henon] was Collection<?> in java
        {
            if (c is RevFlagSet)
            {
                int cMask = ((RevFlagSet)c).mask;
                return (mask & cMask) == cMask;
            }
            return c.All(flag => Contains(flag));
        }

        public bool Add(RevFlag flag)
        {
            if ((mask & flag.mask) != 0)
                return false;
            mask |= flag.mask;
            int p = 0;
            while (p < active.Count && active[p].mask < flag.mask)
                p++;
            active[p] = (flag);
            return true;
        }

        public bool Remove(object o)
        {
            RevFlag flag = (RevFlag)o;
            if ((mask & flag.mask) == 0)
                return false;
            mask &= ~flag.mask;
            for (int i = 0; i < active.Count; i++)
                if (active[i].mask == flag.mask)
                    active.RemoveAt(i);
            return true;
        }

        public Iterator<RevFlag> iterator()
        {
            return new Iterator<RevFlag>(this);
        }

        public class Iterator<T> : ListIterator<T>
            where T : RevFlag
        {

            public Iterator(RevFlagSet set)
                : base(set.active as List<T>)
            {
            }

            private T current;
            private RevFlagSet set;

            //public override bool hasNext() {
            //    return base.hasNext();
            //}

            public override T next()
            {
                return current = base.next();
            }

            public override void remove()
            {
                set.mask &= ~current.mask;
                base.remove();
            }
        }

        public int size()
        {
            return active.Count;
        }
    }
}