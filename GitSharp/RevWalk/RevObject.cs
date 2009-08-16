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

using System.Text;
using System;

namespace GitSharp.RevWalk
{


    /** Base object type accessed during revision walking. */
    public abstract class RevObject : ObjectId, IDisposable
    {

        public static int PARSED = 1;

        public int flags;

        public RevObject(AnyObjectId name)
            : base(name)
        {
            
        }

        internal abstract void parse(RevWalk walk);

        /**
         * Get Git object type. See {@link Constants}.
         * 
         * @return object type
         */
        public abstract int getType();

        /**
         * Get the name of this object.
         * 
         * @return unique hash of this object.
         */
        public ObjectId getId()
        {
            return this;
        }

        public override bool Equals(AnyObjectId o)
        {
            return this == o;
        }

        public override bool Equals(object o)
        {
            return this == (o as RevObject);
        }

        /**
         * Test to see if the flag has been set on this object.
         * 
         * @param flag
         *            the flag to test.
         * @return true if the flag has been added to this object; false if not.
         */
        public bool has(RevFlag flag)
        {
            return (flags & flag.mask) != 0;
        }

        /**
         * Test to see if any flag in the set has been set on this object.
         * 
         * @param set
         *            the flags to test.
         * @return true if any flag in the set has been added to this object; false
         *         if not.
         */
        public bool hasAny(RevFlagSet set)
        {
            return (flags & set.mask) != 0;
        }

        /**
         * Test to see if all flags in the set have been set on this object.
         * 
         * @param set
         *            the flags to test.
         * @return true if all flags of the set have been added to this object;
         *         false if some or none have been added.
         */
        public bool hasAll(RevFlagSet set)
        {
            return (flags & set.mask) == set.mask;
        }

        /**
         * Add a flag to this object.
         * <p>
         * If the flag is already set on this object then the method has no effect.
         * 
         * @param flag
         *            the flag to mark on this object, for later testing.
         */
        public void add(RevFlag flag)
        {
            flags |= flag.mask;
        }

        /**
         * Add a set of flags to this object.
         * 
         * @param set
         *            the set of flags to mark on this object, for later testing.
         */
        public void add(RevFlagSet set)
        {
            flags |= set.mask;
        }

        /**
         * Remove a flag from this object.
         * <p>
         * If the flag is not set on this object then the method has no effect.
         * 
         * @param flag
         *            the flag to remove from this object.
         */
        public void remove(RevFlag flag)
        {
            flags &= ~flag.mask;
        }

        /**
         * Remove a set of flags from this object.
         * 
         * @param set
         *            the flag to remove from this object.
         */
        public void remove(RevFlagSet set)
        {
            flags &= ~set.mask;
        }

        /** Release as much memory as possible from this object. */
        public virtual void dispose()
        {
            // Nothing needs to be done for most objects.
        }

        public virtual void Dispose()
        {
            // Nothing needs to be done for most objects.
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append(GitSharp.Constants.typeString(getType()));
            s.Append(' ');
            s.Append(GetType().Name);
            s.Append(' ');
            appendCoreFlags(s);
            return s.ToString();
        }

        /**
         * @param s
         *            buffer to Append a debug description of core RevFlags onto.
         */
        internal void appendCoreFlags(StringBuilder s)
        {
            s.Append((flags & RevWalk.TOPO_DELAY) != 0 ? 'o' : '-');
            s.Append((flags & RevWalk.TEMP_MARK) != 0 ? 't' : '-');
            s.Append((flags & RevWalk.REWRITE) != 0 ? 'r' : '-');
            s.Append((flags & RevWalk.UNINTERESTING) != 0 ? 'u' : '-');
            s.Append((flags & RevWalk.SEEN) != 0 ? 's' : '-');
            s.Append((flags & RevWalk.PARSED) != 0 ? 'p' : '-');
        }
    }
}
