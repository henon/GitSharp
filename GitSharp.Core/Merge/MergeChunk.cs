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

/**
 * One chunk from a merge result. Each chunk contains a range from a
 * single sequence. In case of conflicts multiple chunks are reported for one
 * conflict. The conflictState tells when conflicts start and end.
 */
namespace GitSharp.Core.Merge
{
	public class MergeChunk {
		/**
	 * A state telling whether a MergeChunk belongs to a conflict or not. The
	 * first chunk of a conflict is reported with a special state to be able to
	 * distinguish the border between two consecutive conflicts
	 */
		public enum ConflictState {
			/**
		 * This chunk does not belong to a conflict
		 */
			NO_CONFLICT = 0,

			/**
		 * This chunk does belong to a conflict and is the first one of the
		 * conflicting chunks
		 */
			FIRST_CONFLICTING_RANGE = 1,

			/**
		 * This chunk does belong to a conflict but is not the first one of the
		 * conflicting chunks. It's a subsequent one.
		 */
			NEXT_CONFLICTING_RANGE = 2
		}

		private int sequenceIndex;

		private int begin;

		private int end;

		private ConflictState conflictState;

		/**
	 * Creates a new empty MergeChunk
	 *
	 * @param sequenceIndex
	 *            determines to which sequence this chunks belongs to. Same as
	 *            in {@link MergeResult#add(int, int, int, ConflictState)}
	 * @param begin
	 *            the first element from the specified sequence which should be
	 *            included in the merge result. Indexes start with 0.
	 * @param end
	 *            specifies the end of the range to be added. The element this
	 *            index points to is the first element which not added to the
	 *            merge result. All elements between begin (including begin) and
	 *            this element are added.
	 * @param conflictState
	 *            the state of this chunk. See {@link ConflictState}
	 */

		public MergeChunk(int sequenceIndex, int begin, int end,
			ConflictState conflictState) {
			this.sequenceIndex = sequenceIndex;
			this.begin = begin;
			this.end = end;
			this.conflictState = conflictState;
			}

		/**
	 * @return the index of the sequence to which sequence this chunks belongs
	 *         to. Same as in
	 *         {@link MergeResult#add(int, int, int, ConflictState)}
	 */
		public int getSequenceIndex() {
			return sequenceIndex;
		}

		/**
	 * @return the first element from the specified sequence which should be
	 *         included in the merge result. Indexes start with 0.
	 */
		public int getBegin() {
			return begin;
		}

		/**
	 * @return the end of the range of this chunk. The element this index
	 *         points to is the first element which not added to the merge
	 *         result. All elements between begin (including begin) and this
	 *         element are added.
	 */
		public int getEnd() {
			return end;
		}

		/**
	 * @return the state of this chunk. See {@link ConflictState}
	 */
		public ConflictState getConflictState() {
			return conflictState;
		}
	}
}