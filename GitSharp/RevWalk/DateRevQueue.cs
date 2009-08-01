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
namespace GitSharp.RevWalk
{


    /** A queue of commits sorted by commit time order. */
    public class DateRevQueue : AbstractRevQueue
    {
        private Entry head;

        private Entry free;

        /** Create an empty date queue. */
        public DateRevQueue() : base()
        {
        }

        public DateRevQueue(Generator s)
        {
            for (; ; )
            {
                RevCommit c = s.next();
                if (c == null)
                    break;
                add(c);
            }
        }

        public override void add(RevCommit c)
        {
            Entry q = head;
            long when = c.commitTime;
            Entry n = newEntry(c);
            if (q == null || when > q.commit.commitTime)
            {
                n.next = q;
                head = n;
            }
            else
            {
                Entry p = q.next;
                while (p != null && p.commit.commitTime > when)
                {
                    q = p;
                    p = q.next;
                }
                n.next = q.next;
                q.next = n;
            }
        }

        public override RevCommit next()
        {
            Entry q = head;
            if (q == null)
                return null;
            head = q.next;
            freeEntry(q);
            return q.commit;
        }

        /**
         * Peek at the next commit, without removing it.
         * 
         * @return the next available commit; null if there are no commits left.
         */
        public RevCommit peek()
        {
            return head != null ? head.commit : null;
        }

        public override void clear()
        {
            head = null;
            free = null;
        }

        internal override bool everbodyHasFlag(int f)
        {
            for (Entry q = head; q != null; q = q.next)
            {
                if ((q.commit.flags & f) == 0)
                    return false;
            }
            return true;
        }

        internal override bool anybodyHasFlag(int f)
        {
            for (Entry q = head; q != null; q = q.next)
            {
                if ((q.commit.flags & f) != 0)
                    return true;
            }
            return false;
        }

        public override int outputType()
        {
            return _outputType | SORT_COMMIT_TIME_DESC;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            for (Entry q = head; q != null; q = q.next)
                describe(s, q.commit);
            return s.ToString();
        }

        private Entry newEntry(RevCommit c)
        {
            Entry r = free;
            if (r == null)
                r = new Entry();
            else
                free = r.next;
            r.commit = c;
            return r;
        }

        private void freeEntry(Entry e)
        {
            e.next = free;
            free = e;
        }

        internal class Entry
        {
            public Entry next;

            public RevCommit commit;
        }
    }
}
