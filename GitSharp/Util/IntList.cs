/*
 * Copyright (C) 2008, Google Inc.
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
using System.Text;
namespace GitSharp.Util
{
    /** A more efficient List<Integer> using a primitive integer array. */
    public class IntList
    {
	    private int[] entries;

	    private int count;

	    /** Create an empty list with a default capacity. */
	    public IntList() : this(10) {}

	    /**
	     * Create an empty list with the specified capacity.
	     *
	     * @param capacity
	     *            number of entries the list can initially hold.
	     */
	    public IntList(int capacity)
        {
		    entries = new int[capacity];
	    }

	    /** @return number of entries in this list */
	    public int size()
        {
		    return count;
	    }

	    /**
	     * @param i
	     *            index to read, must be in the range [0, {@link #size()}).
	     * @return the number at the specified index
	     * @throws ArrayIndexOutOfBoundsException
	     *             the index outside the valid range
	     */
	    public int get(int i)
        {
		    if (count <= i)
			    throw new IndexOutOfRangeException();
		    return entries[i];
	    }

	    /** Empty this list */
	    public void clear()
        {
		    count = 0;
	    }

	    /**
	     * Add an entry to the end of the list.
	     *
	     * @param n
	     *            the number to add.
	     */
	    public void add(int n)
        {
		    if (count == entries.Length)
			    grow();
		    entries[count++] = n;
	    }

	    /**
	     * Pad the list with entries.
	     *
	     * @param toIndex
	     *            index position to stop filling at. 0 inserts no filler. 1
	     *            ensures the list has a size of 1, adding <code>val</code> if
	     *            the list is currently empty.
	     * @param val
	     *            value to insert into padded positions.
	     */
	    public void fillTo(int toIndex, int val)
        {
		    while (count < toIndex)
			    add(val);
	    }

	    private void grow()
        {
		    int[] n = new int[(entries.Length + 16) * 3 / 2];
		    Array.Copy(entries, 0, n, 0, count);
		    entries = n;
	    }

	    public string toString()
        {
		    StringBuilder r = new StringBuilder();
		    r.Append('[');
		    for (int i = 0; i < count; i++) {
			    if (i > 0)
				    r.Append(", ");
			    r.Append(entries[i]);
		    }
		    r.Append(']');
		    return r.ToString();
	    }
    }
}