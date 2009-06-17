/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2007, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp
{
    [Complete]
    public abstract class TreeEntry : IComparable
    {
        public static int MODIFIED_ONLY = 1 << 0;

        public static int LOADED_ONLY = 1 << 1;

        public static int CONCURRENT_MODIFICATION = 1 << 2;


        public Tree Parent { get; private set; }
        public byte[] NameUTF8 { get; private set; }

        public TreeEntry(Tree myParent, ObjectId id, byte[] nameUTF8)
        {
            this.NameUTF8 = nameUTF8;
            this.Parent = myParent;
            this._id = id;
        }

        public void Delete()
        {
            this.Parent.RemoveEntry(this);
            DetachParent();
        }



        public void DetachParent()
        {
            this.Parent = null;
        }

        public void AttachParent(Tree p)
        {
            this.Parent = p;
        }
        public virtual Repository Repository
        {
            get
            {
                return this.Parent.Repository;
            }
        }

        public string Name
        {
            get
            {
                return this.NameUTF8 != null ? new string(Encoding.ASCII.GetChars(this.NameUTF8)) : null;
            }
        }


        public void Rename(String n)
        {
            Rename(Encoding.ASCII.GetBytes(n));
        }

        public void Rename(byte[] n)
        {
            Tree t = this.Parent;
            if (t != null)
            {
                Delete();
            }
            this.NameUTF8 = n;
            if (t != null)
            {
                t.AddEntry(this);
            }
        }


        #region IComparable Members

        public int CompareTo(object o)
        {

            if (this == o)
                return 0;

			TreeEntry t = o as TreeEntry;
			
			if (t != null)
                return Tree.CompareNames(NameUTF8, t.NameUTF8, LastChar(this), LastChar(t));
            return -1;
        }

        #endregion


        public static int LastChar(TreeEntry treeEntry)
        {
            if (treeEntry is FileTreeEntry)
                return '\0';
            else
                return '/';
        }

        private ObjectId _id;
        public ObjectId Id
        {
            get
            {
                return _id;
            }
            set
            {
                //
                Tree p = Parent;
                if (p != null && _id != value)
                    if ((_id == null && value != null) || (_id != null && value == null) || !_id.Equals(value))
                        p.Id = null;

                _id = value;
            }
        }

        public bool IsModified
        {
            get
            {
                return _id == null;
            }
        }

        public void SetModified()
        {
            this.Id = null;
        }

        public string FullName
        {
            get
            {
                StringBuilder r = new StringBuilder();
                AppendFullName(r);
                return r.ToString();
            }
        }

        public byte[] FullNameUTF8
        {
            get
            {
                return Encoding.UTF8.GetBytes(this.FullName);
            }
        }

        private void AppendFullName(StringBuilder r)
        {
            TreeEntry p = this.Parent;
            String n = this.Name;
            if (p != null)
            {
                p.AppendFullName(r);
                if (r.Length > 0)
                {
                    r.Append('/');
                }
            }
            if (n != null)
            {
                r.Append(n);
            }
        }

        public void Accept(TreeVisitor tv)
        {
            Accept(tv, 0);
        }

        public abstract void Accept(TreeVisitor tv, int flags);

        public abstract FileMode Mode { get; }

    }
}
