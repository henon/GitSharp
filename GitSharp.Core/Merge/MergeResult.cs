/*
 * Copyright (C) 2009, Christian Halstrick <christian.halstrick@sap.com>
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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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

/*
 * The result of merging a number of {@link Sequence} objects. These sequences
 * have one common predecessor sequence. The result of a merge is a list of
 * MergeChunks. Each MergeChunk contains either a range (a subsequence) from
 * one of the merged sequences, a range from the common predecessor or a
 * conflicting range from one of the merged sequences. A conflict will be
 * reported as multiple chunks, one for each conflicting range. The first chunk
 * for a conflict is marked specially to distinguish the border between two
 * consecutive conflicts.
 * <p>
 * This class does not know anything about how to present the merge result to
 * the end-user. MergeFormatters have to be used to construct something human
 * readable.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using GitSharp.Core.Diff;
using GitSharp.Core.Util;

public class MergeResult
{
    public IEnumerator<MergeChunk> GetEnumerator()
    {
       return new MergeChunkIterator(this);
    }

    private List<Sequence> _sequences;

    private IntList _chunks = new IntList();

    private bool _containsConflicts = false;

    /*
     * Creates a new empty MergeResult
     *
     * @param sequences
     *            contains the common predecessor sequence at position 0
     *            followed by the merged sequences. This list should not be
     *            modified anymore during the lifetime of this {@link MergeResult}.
     */
    public MergeResult(List<Sequence> sequences)
    {
        this._sequences = sequences;
    }

    /*
     * Adds a new range from one of the merged sequences or from the common
     * predecessor. This method can add conflicting and non-conflicting ranges
     * controlled by the conflictState parameter
     *
     * @param srcIdx
     *            determines from which sequence this range comes. An index of
     *            x specifies the x+1 element in the list of sequences
     *            specified to the constructor
     * @param begin
     *            the first element from the specified sequence which should be
     *            included in the merge result. Indexes start with 0.
     * @param end
     *            specifies the end of the range to be added. The element this
     *            index points to is the first element which not added to the
     *            merge result. All elements between begin (including begin) and
     *            this element are added.
     * @param conflictState
     *            when set to NO_CONLICT a non-conflicting range is added.
     *            This will end implicitly all open conflicts added before.
     */
    public void add(int srcIdx, int begin, int end, MergeChunk.ConflictState conflictState)
    {
        _chunks.add((int)conflictState);
        _chunks.add(srcIdx);
        _chunks.add(begin);
        _chunks.add(end);
        if (conflictState != MergeChunk.ConflictState.NO_CONFLICT)
            _containsConflicts = true;
    }

    /*
     * Returns the common predecessor sequence and the merged sequence in one
     * list. The common predecessor is is the first element in the list
     *
     * @return the common predecessor at position 0 followed by the merged
     *         sequences.
     */
    public List<Sequence> getSequences()
    {
        return _sequences;
    }

    /*
     * @return an iterator over the MergeChunks. The iterator does not support
     * the remove operation
     */
    public MergeChunkIterator iterator()
    {
        return new MergeChunkIterator(this);
    }

    /*
    * @return true if this merge result contains conflicts
    */
    public bool containsConflicts()
    {
        return _containsConflicts;
    }

    public class MergeChunkIterator : IEnumerator<MergeChunk>
    {
        private readonly MergeResult _mergeResult;
        int idx;
        private MergeChunk _current;

        public MergeChunkIterator(MergeResult mergeResult)
        {
            _mergeResult = mergeResult;
        }

        public bool hasNext()
        {
            return (idx < _mergeResult._chunks.size());
        }

        public MergeChunk next()
        {
            MergeChunk.ConflictState state = (MergeChunk.ConflictState)(_mergeResult._chunks.get(idx++));
            int srcIdx = _mergeResult._chunks.get(idx++);
            int begin = _mergeResult._chunks.get(idx++);
            int end = _mergeResult._chunks.get(idx++);
            return new MergeChunk(srcIdx, begin, end, state);
        }

        public void remove()
        {
            throw new NotSupportedException();
        }

        public bool MoveNext()
        {
            if (!hasNext())
            {
                return false;
            }

            _current = next();
            return true;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public MergeChunk Current
        {
            get { return _current; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
