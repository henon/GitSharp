/*
 * Copyright (C) 2008, Johannes E. Schindelin <johannes.schindelin@gmx.de>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
namespace GitSharp.Diff
{
    /**
     * A modified region detected between two versions of roughly the same content.
     * <p>
     * An edit covers the modified region only. It does not cover a common region.
     * <p>
     * Regions should be specified using 0 based notation, so add 1 to the start and
     * end marks for line numbers in a file.
     * <p>
     * An edit where <code>beginA == endA && beginB < endB</code> is an insert edit,
     * that is sequence B inserted the elements in region
     * <code>[beginB, endB)</code> at <code>beginA</code>.
     * <p>
     * An edit where <code>beginA < endA && beginB == endB</code> is a delete edit,
     * that is sequence B has removed the elements between
     * <code>[beginA, endA)</code>.
     * <p>
     * An edit where <code>beginA < endA && beginB < endB</code> is a replace edit,
     * that is sequence B has replaced the range of elements between
     * <code>[beginA, endA)</code> with those found in <code>[beginB, endB)</code>.
     */
    public class Edit
    {
	    /** Type of edit */
	    public enum Type
        {
		    /** Sequence B has inserted the region. */
		    INSERT,

		    /** Sequence B has removed the region. */
		    DELETE,

		    /** Sequence B has replaced the region with different content. */
		    REPLACE,

		    /** Sequence A and B have zero length, describing nothing. */
		    EMPTY
	    }

	    int beginA;
	    int endA;
	    int beginB;
	    int endB;

	    /**
	     * Create a new empty edit.
	     *
	     * @param a_start
	     *            beginA: start and end of region in sequence A; 0 based.
	     * @param b_start
	     *            beginB: start and end of region in sequence B; 0 based.
	     */
	    public Edit(int a_start, int b_start)
            :this(a_start, a_start, b_start, b_start)
        {}

	    /**
	     * Create a new edit.
	     *
	     * @param a_start
	     *            beginA: start of region in sequence A; 0 based.
	     * @param a_end
	     *            endA: end of region in sequence A; must be >= as.
	     * @param b_start
	     *            beginB: start of region in sequence B; 0 based.
	     * @param b_end
	     *            endB: end of region in sequence B; must be >= bs.
	     */
	    public Edit(int a_start, int a_end, int b_start, int b_end)
        {
		    beginA = a_start;
		    endA = a_end;

		    beginB = b_start;
		    endB = b_end;
	    }

	    /** @return the type of this region */
	    public Type getType()
        {
		    if (beginA == endA && beginB < endB)
			    return Type.INSERT;
		    if (beginA < endA && beginB == endB)
			    return Type.DELETE;
		    if (beginA == endA && beginB == endB)
			    return Type.EMPTY;
		    return Type.REPLACE;
	    }

	    /** @return start point in sequence A. */
	    public int getBeginA()
        {
		    return beginA;
	    }

	    /** @return end point in sequence A. */
	    public int getEndA()
        {
		    return endA;
	    }

	    /** @return start point in sequence B. */
	    public int getBeginB() {
		    return beginB;
	    }

	    /** @return end point in sequence B. */
	    public int getEndB() {
		    return endB;
	    }

	    /** Increase {@link #getEndA()} by 1. */
	    public void extendA() {
		    endA++;
	    }

	    /** Increase {@link #getEndB()} by 1. */
	    public void extendB() {
		    endB++;
	    }

	    /** Swap A and B, so the edit goes the other direction. */
	    public void swap() {
		    int sBegin = beginA;
		    int sEnd = endA;

		    beginA = beginB;
		    endA = endB;

		    beginB = sBegin;
		    endB = sEnd;
	    }

	    public int hashCode()
        {
		    return beginA ^ endA;
	    }

        public override bool Equals(Object o)
        {
		    if (o is Edit)
            {
			    Edit e = (Edit) o;
			    return this.beginA == e.beginA && this.endA == e.endA
					    && this.beginB == e.beginB && this.endB == e.endB;
		    }
		    return false;
	    }

	    public String toString() {
		    Type t = getType();
		    return t + "(" + beginA + "-" + endA + "," + beginB + "-" + endB + ")";
	    }
    }
}