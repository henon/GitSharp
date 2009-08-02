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

using System.Collections.Generic;
namespace GitSharp.RevWalk.Filter
{

    /** Matches only commits with some/all RevFlags already set. */
    public abstract class RevFlagFilter : RevFilter
    {
        /**
         * Create a new filter that tests for a single flag.
         * 
         * @param a
         *            the flag to test.
         * @return filter that selects only commits with flag <code>a</code>.
         */
        public static RevFilter has(RevFlag a)
        {
            RevFlagSet s = new RevFlagSet();
            s.Add(a);
            return new HasAll(s);
        }

        /**
         * Create a new filter that tests all flags in a set.
         * 
         * @param a
         *            set of flags to test.
         * @return filter that selects only commits with all flags in <code>a</code>.
         */
        public static RevFilter hasAll(params RevFlag[] a)
        {
            RevFlagSet set = new RevFlagSet();
            foreach (RevFlag flag in a)
                set.Add(flag);
            return new HasAll(set);
        }

        /**
         * Create a new filter that tests all flags in a set.
         * 
         * @param a
         *            set of flags to test.
         * @return filter that selects only commits with all flags in <code>a</code>.
         */
        public static RevFilter hasAll(RevFlagSet a)
        {
            return new HasAll(new RevFlagSet(a));
        }

        /**
         * Create a new filter that tests for any flag in a set.
         * 
         * @param a
         *            set of flags to test.
         * @return filter that selects only commits with any flag in <code>a</code>.
         */
        public static RevFilter hasAny(params RevFlag[] a)
        {
            RevFlagSet set = new RevFlagSet();
            foreach (RevFlag flag in a)
                set.Add(flag);
            return new HasAny(set);
        }

        /**
         * Create a new filter that tests for any flag in a set.
         * 
         * @param a
         *            set of flags to test.
         * @return filter that selects only commits with any flag in <code>a</code>.
         */
        public static RevFilter hasAny(RevFlagSet a)
        {
            return new HasAny(new RevFlagSet(a));
        }

        public RevFlagSet flags;

        public RevFlagFilter(RevFlagSet m)
        {
            flags = m;
        }

        public override RevFilter Clone()
        {
            return this;
        }

        public override string ToString()
        {
            return base.ToString() + flags;
        }

        public class HasAll : RevFlagFilter
        {
            public HasAll(RevFlagSet m)
                : base(m)
            {
            }

            public override bool include(RevWalk walker, RevCommit c)
            {
                return c.hasAll(flags);
            }
        }

        public class HasAny : RevFlagFilter
        {
            public HasAny(RevFlagSet m)
                : base(m)
            {
            }

            public override bool include(RevWalk walker, RevCommit c)
            {
                return c.hasAny(flags);
            }
        }
    }
}
