/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using System.Text;

namespace GitSharp.RevWalk.Filter
{

    /**
     * Includes a commit if any subfilters include the same commit.
     * <p>
     * Classic shortcut behavior is used, so evaluation of the
     * {@link RevFilter#include(RevWalk, RevCommit)} method stops as soon as a true
     * result is obtained. Applications can improve filtering performance by placing
     * faster filters that are more likely to accept a result earlier in the list.
     */
    public abstract class OrRevFilter : RevFilter
    {
        /**
         * Create a filter with two filters, one of which must match.
         * 
         * @param a
         *            first filter to test.
         * @param b
         *            second filter to test.
         * @return a filter that must match at least one input filter.
         */
        public static RevFilter create(RevFilter a, RevFilter b)
        {
            if (a == ALL || b == ALL)
                return ALL;
            return new Binary(a, b);
        }

        /**
         * Create a filter around many filters, one of which must match.
         * 
         * @param list
         *            list of filters to match against. Must contain at least 2
         *            filters.
         * @return a filter that must match at least one input filter.
         */
        public static RevFilter create(RevFilter[] list)
        {
            if (list.Length == 2)
                return create(list[0], list[1]);
            if (list.Length < 2)
                throw new ArgumentException("At least two filters needed.");
            RevFilter[] subfilters = new RevFilter[list.Length];
            Array.Copy(list, 0, subfilters, 0, list.Length);
            return new List(subfilters);
        }

        /**
         * Create a filter around many filters, one of which must match.
         * 
         * @param list
         *            list of filters to match against. Must contain at least 2
         *            filters.
         * @return a filter that must match at least one input filter.
         */
        public static RevFilter create(IEnumerable<RevFilter> list)
        {
            if (list.Count() < 2)
                throw new ArgumentException("At least two filters needed.");
            RevFilter[] subfilters = list.ToArray();
            if (subfilters.Length == 2)
                return create(subfilters[0], subfilters[1]);
            return new List(subfilters);
        }

        private class Binary : OrRevFilter
        {
            private RevFilter a;

            private RevFilter b;

            public Binary(RevFilter one, RevFilter two)
            {
                a = one;
                b = two;
            }

            public override bool include(RevWalk walker, RevCommit c)
            {
                return a.include(walker, c) || b.include(walker, c);
            }

            public override RevFilter Clone()
            {
                return new Binary(a.Clone(), b.Clone());
            }

            public override string ToString()
            {
                return "(" + a.ToString() + " OR " + b.ToString() + ")";
            }
        }

        private class List : OrRevFilter
        {
            private RevFilter[] subfilters;

            public List(RevFilter[] list)
            {
                subfilters = list;
            }

            public override bool include(RevWalk walker, RevCommit c)
            {
                foreach (RevFilter f in subfilters)
                {
                    if (f.include(walker, c))
                        return true;
                }
                return false;
            }

            public override RevFilter Clone()
            {
                RevFilter[] s = new RevFilter[subfilters.Length];
                for (int i = 0; i < s.Length; i++)
                    s[i] = subfilters[i].Clone();
                return new List(s);
            }

            public override string ToString()
            {
                StringBuilder r = new StringBuilder();
                r.Append("(");
                for (int i = 0; i < subfilters.Length; i++)
                {
                    if (i > 0)
                        r.Append(" OR ");
                    r.Append(subfilters[i].ToString());
                }
                r.Append(")");
                return r.ToString();
            }
        }
    }
}