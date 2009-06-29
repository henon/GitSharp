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

namespace GitSharp.RevWalk
{


    public abstract class BlockRevQueue : AbstractRevQueue
    {
        internal BlockFreeList free;

        /** Create an empty revision queue. */
        internal BlockRevQueue()
        {
            free = new BlockFreeList();
        }

        BlockRevQueue(Generator s)
        {
            free = new BlockFreeList();
            _outputType = s.outputType();
            s.shareFreeList(this);
            for (; ; )
            {
                RevCommit c = s.next();
                if (c == null)
                    break;
                add(c);
            }
        }

        /**
         * Reconfigure this queue to share the same free list as another.
         * <p>
         * Multiple revision queues can be connected to the same free list, making
         * it less expensive for applications to shuttle commits between them. This
         * method arranges for the receiver to take from / return to the same free
         * list as the supplied queue.
         * <p>
         * Free lists are not thread-safe. Applications must ensure that all queues
         * sharing the same free list are doing so from only a single thread.
         * 
         * @param q
         *            the other queue we will steal entries from.
         */
        public void shareFreeList(BlockRevQueue q)
        {
            free = q.free;
        }

        public class BlockFreeList
        {
            private Block next;

            public Block newBlock()
            {
                Block b = next;
                if (b == null)
                    return new Block();
                next = b.next;
                b.clear();
                return b;
            }

            public void freeBlock(Block b)
            {
                b.next = next;
                next = b;
            }

            public void clear()
            {
                next = null;
            }
        }

        public class Block
        {
            public static int BLOCK_SIZE = 256;

            /** Next block in our chain of blocks; null if we are the last. */
            public Block next;

            /** Our table of queued commits. */
            public RevCommit[] commits = new RevCommit[BLOCK_SIZE];

            /** Next valid entry in {@link #commits}. */
            public int headIndex;

            /** Next free entry in {@link #commits} for addition at. */
            public int tailIndex;

            public bool isFull()
            {
                return tailIndex == BLOCK_SIZE;
            }

            public bool isEmpty()
            {
                return headIndex == tailIndex;
            }

            public bool canUnpop()
            {
                return headIndex > 0;
            }

            public void add(RevCommit c)
            {
                commits[tailIndex++] = c;
            }

            public void unpop(RevCommit c)
            {
                commits[--headIndex] = c;
            }

            public RevCommit pop()
            {
                return commits[headIndex++];
            }

            public RevCommit peek()
            {
                return commits[headIndex];
            }

            public void clear()
            {
                next = null;
                headIndex = 0;
                tailIndex = 0;
            }

            public void resetToMiddle()
            {
                headIndex = tailIndex = BLOCK_SIZE / 2;
            }

            public void resetToEnd()
            {
                headIndex = tailIndex = BLOCK_SIZE;
            }
        }
    }
}
