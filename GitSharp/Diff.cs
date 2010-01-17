/*
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using System.Linq;
using System.Text;
using GitSharp.Core.Diff;

namespace GitSharp
{
    /// <summary>
    /// A Diff represents the line-based differences between two text sequences given as string or byte array as a list of Sections. The process of 
    /// creating the diff might take a while for large files. 
    /// <para/>
    /// Note: The underlying differencer operates on raw bytes.
    /// </summary>
    public class Diff : IEnumerable<Diff.Section>
    {
        /// <summary>
        /// Creates a line-based diff from the given texts. The strings are expected to be encoded in UTF8.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Diff(string a, string b)
            : this(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b))
        {
        }

        /// <summary>
        /// Creates a line-based diff from the contents of the given blobs.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Diff(Blob a, Blob b)
            : this(a.RawData, b.RawData)
        {
        }

        /// <summary>
        /// Creates a line-based diff from the the given byte arrays.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Diff(byte[] a, byte[] b)
        {
            m_sequence_a = a;
            m_sequence_b = b;
            var diff = new MyersDiff(new RawText(a), new RawText(b));
            m_edits = diff.getEdits();
        }

        private readonly byte[] m_sequence_a;
        private readonly byte[] m_sequence_b;

        public bool HasDifferences
        {
            get
            {
                if (m_edits.Count == 0)
                    return false;
                if (m_edits.Count == 1 && m_edits[0].EditType == Edit.Type.EMPTY)
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Get the changed, unchanged and conflicting sections of this Diff.
        /// </summary>
        public IEnumerable<Section> Sections
        {
            get
            {
                if (m_edits.Count == 0)
                {
                    if (m_sequence_a.Length == 0 && m_sequence_b.Length == 0) // <-- no content so return no sections
                        yield break;
                    // there is content but no edits so return a single unchanged section.
                    yield return new Section(m_sequence_a, m_sequence_b) { Status = SectionStatus.Unchanged, BeginA = 0, BeginB = 0, EndA = m_sequence_a.Length, EndB = m_sequence_b.Length };
                    yield break;
                }
                if (m_edits.Count == 1 && m_edits[0].EditType == Edit.Type.EMPTY)
                    yield break;
                if (m_edits[0].BeginA > 0 || m_edits[0].BeginB > 0) // <-- see if there is an unchanged section before the first edit
                    yield return new Section(m_sequence_a, m_sequence_b) { Status = SectionStatus.Unchanged, BeginA = 0, BeginB = 0, EndA = m_edits[0].BeginA, EndB = m_edits[0].BeginB };
                int index = 0;
                Edit edit = null;
                foreach (var e in m_edits)
                {
                    edit = e;
                    if (edit.EditType == Edit.Type.EMPTY)
                        continue;
                    yield return new Section(m_sequence_a, m_sequence_b, edit);
                    if (index + 1 >= m_edits.Count)
                        break;
                    var next_edit = m_edits[index + 1];
                    if (next_edit.BeginA > edit.EndA || next_edit.BeginB > edit.EndB) // <-- see if there is a unchanged text block between the edits 
                        yield return new Section(m_sequence_a, m_sequence_b, edit) { Status = SectionStatus.Unchanged, BeginA = edit.EndA, BeginB = edit.EndB, EndA = next_edit.BeginA, EndB = next_edit.BeginB };
                    index += 1;
                }
                if (edit == null)
                    yield break;
                if (edit.EndA < m_sequence_a.Length || edit.EndB < m_sequence_b.Length) // <-- see if there is an unchanged section at the end
                    yield return new Section(m_sequence_a, m_sequence_b) { Status = SectionStatus.Unchanged, BeginA = edit.EndA, BeginB = edit.EndB, EndA = m_sequence_a.Length, EndB = m_sequence_b.Length };
            }
        }

        private readonly EditList m_edits;

        public enum SectionStatus { Unchanged, Different, Conflicting }

        public enum EditType { Unchanged, Inserted, Deleted, Replaced }

        #region --> Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<Section> GetEnumerator()
        {
            return Sections.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region --> class Diff Section


        /// <summary>
        /// Section represents a block of text that is unchanged in two text sequences, a corresponding edit in two text sequences (two-way diff)  or a conflict (three-way diff).
        /// </summary>
        public class Section
        {
            public Section(byte[] a, byte[] b)
            {
                m_sequence_a = a;
                m_sequence_b = b;
            }

            private readonly byte[] m_sequence_a;
            private readonly byte[] m_sequence_b;

            internal Section(byte[] a, byte[] b, Edit edit)
                : this(a, b)
            {
                Status = SectionStatus.Different;
                BeginA = edit.BeginA;
                EndA = edit.EndA;
                BeginB = edit.BeginB;
                EndB = edit.EndB;
            }

            public byte[] RawTextA
            {
                get
                {
                    return m_sequence_a.Skip(BeginA).Take(EndA - BeginA).ToArray();
                }
            }

            public byte[] RawTextB
            {
                get
                {
                    return m_sequence_b.Skip(BeginB).Take(EndB - BeginB).ToArray();
                }
            }

            public string TextA
            {
                get
                {
                    return Encoding.UTF8.GetString(RawTextA);
                }
            }

            public string TextB
            {
                get
                {
                    return Encoding.UTF8.GetString(RawTextB);
                }
            }

            public int BeginA
            {
                get;
                internal set;
            }

            public int BeginB
            {
                get;
                internal set;
            }

            public int EndA
            {
                get;
                internal set;
            }

            public int EndB
            {
                get;
                internal set;
            }

            public SectionStatus Status
            {
                get;
                internal set;
            }

            public EditType EditWithRespectToA
            {
                get
                {
                    if (Status == SectionStatus.Unchanged)
                        return EditType.Unchanged;
                    if (BeginB == EndB && BeginA != EndA)
                        return EditType.Deleted;
                    if (BeginA == EndA && BeginB != EndB)
                        return EditType.Inserted;
                    return EditType.Replaced;
                }
            }

            public EditType EditWithRespectToB
            {
                get
                {
                    if (Status == SectionStatus.Unchanged)
                        return EditType.Unchanged;
                    if (BeginB == EndB && BeginA != EndA)
                        return EditType.Inserted;
                    if (BeginA == EndA && BeginB != EndB)
                        return EditType.Deleted;
                    return EditType.Replaced;
                }
            }

        }


        #endregion


    }
}
