using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
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
                return Tree.CompareNames(NameUTF8, t.NameUTF8, lastChar(this), lastChar(t));
            return -1;
        }

        #endregion


        public static int lastChar(TreeEntry treeEntry)
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
