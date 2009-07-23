/*
 * Copyright (C) 2009, Google Inc.
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
using System.Collections;
using System.Collections.Generic;

namespace GitSharp.Diff
{
    /** Specialized list of {@link Edit}s in a document. */
    public class EditList : List<Edit>
    {
	    private List<Edit> container;

	    /** Create a new, empty edit list. */
	    public EditList()
        {
            container = new List<Edit>();
	    }

	    public int size()
        {
		    return container.Count;
        }

	    public Edit get(int index)
        {
            return (Edit)container[index];
	    }

	    public Edit set(int index, Edit element)
        {
		    return container[index] = element;
	    }

	    public void add(int index, Edit element)
        {
		    container.Insert(index, element);
	    }

	    public void remove(int index)
        {
		    container.RemoveAt(index);
	    }

	    public int hashCode()
        {
		    return container.GetHashCode();
	    }

	    public bool equals(Object o)
        {
		    if (o is EditList)
			    return container.Equals(((EditList) o).container);
		    return false;
	    }

	    public String toString()
        {
		    return "EditList" + container.ToString();
	    }

        /* This method did not exist in the original Java code.
         * In Java, the AbstractList has a method named isEmpty
         * C#'s AbstractList has no such method
         */
        public bool isEmpty()
        {
            return (container.Count == 0);
        }
    }
}