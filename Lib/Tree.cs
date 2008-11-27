/*
 * Copyright (C) 2007, Robin Rosenberg <me@lathund.dewire.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

namespace Gitty.Lib
{
	public class Tree : TreeEntry, Treeish
	{
		#region Internals
		private static readonly TreeEntry[] EmptyTree = new TreeEntry[0];

		private readonly Repository _db;
		private TreeEntry[] _contents;
		#endregion

		#region Constructors
		public Tree(Repository repo)
			: base(null, null, null)
		{
			_db = repo;
			_contents = EmptyTree;
		}

		public Tree(Repository repo, ObjectId id, byte[] raw)
			: base(null, id, null)
		{
			_db = repo;
			ReadTree(raw);
		}

		public Tree(Tree parent, byte[] nameUTF8)
			: base(parent, null, nameUTF8)
		{
			_db = Repository;
			_contents = EmptyTree;
		}

		public Tree(Tree parent, ObjectId id, byte[] nameUTF8)
			: base(parent, id, nameUTF8)
		{
			_db = Repository;
		}
		#endregion

		#region Properties

		public bool IsRoot
		{
			get { return Parent == null; }
		}

		public override Repository Repository
		{
			get
			{
				return _db;
			}
		}

		public override FileMode Mode
		{
			get { return FileMode.Tree; }
		}
		#endregion

		public static int CompareNames(byte[] a, byte[] b, int lasta, int lastb)
		{
			return CompareNames(a, b, 0, b.Length, lasta, lastb);
		}

		private static int CompareNames(byte[] a, byte[] nameUTF8, int nameStart, int nameEnd, int lasta, int lastb)
		{
			// There must be a .NET way of doing this! I assume there are both UTF8 names, 
			// perhaps Encoding.UTF8.GetString on both then .Compare on the strings?
			// I'm pretty sure this is just doing that but the long way round, however 
			// I could be wrong so we'll leave it at this for now. - NR
			int j = 0;
			int k = 0;

			for (j = 0; j < a.Length && k < nameEnd; j++, k++)
			{
				int aj = a[j] & 0xff;
				int bk = nameUTF8[k] & 0xff;
				if (aj < bk)
					return -1;
				else if (aj > bk)
					return 1;
			}

			if (j < a.Length)
			{
				int aj = a[j] & 0xff;
				if (aj < lastb)
					return -1;
				else if (aj > lastb)
					return 1;
				else
					if (j == a.Length - 1)
						return 0;
					else
						return -1;
			}

			if (k < nameEnd)
			{
				int bk = nameUTF8[k] & 0xff;
				if (lasta < bk)
					return -1;
				else if (lasta > bk)
					return 1;
				else
					if (k == nameEnd - 1)
						return 0;
					else
						return -1;
			}

			if (lasta < lastb)
				return -1;
			else if (lasta > lastb)
				return 1;

			int nameLength = nameEnd - nameStart;
			if (a.Length == nameLength)
				return 0;
			else if (a.Length < nameLength)
				return -1;
			else
				return 1;
		}

		private static byte[] SubString(byte[] s, int nameStart, int nameEnd)
		{
			if (nameStart == 0 && nameStart == s.Length)
				return new byte[]{ };

			byte[] n = new byte[nameEnd - nameStart];
			Array.Copy(s, nameStart, n, 0, n.Length);
			return n;
		}

		private static int BinarySearch(
			TreeEntry[] entries, byte[] nameUTF8, int nameUTF8last, int nameStart, int nameEnd)
		{
			if (entries.Length == 0)
				return -1;
			int high = entries.Length;
			int low = 0;
			do
			{
				int mid = (low + high) / 2;
				int cmp = CompareNames(entries[mid].NameUTF8, nameUTF8, 
					nameStart, nameEnd, TreeEntry.lastChar(entries[mid]), nameUTF8last);

				if (cmp < 0)
					low = mid + 1;
				else if (cmp == 0)
					return mid;
				else
					return high = mid;

			} while (low < high);
			return -(low + 1);
		}

		public override void Accept(TreeVisitor tv, int flags)
		{
			throw new NotImplementedException();
		}

		private void ReadTree(byte[] raw)
		{
			throw new NotImplementedException();
		}

		#region Treeish Members
		public ObjectId GetTreeId()
		{
			return Id;
		}

		public Tree GetTree()
		{
			return this;
		}
		#endregion

		internal void RemoveEntry(TreeEntry treeEntry)
		{
			throw new NotImplementedException();
		}

		internal void AddEntry(TreeEntry treeEntry)
		{
			throw new NotImplementedException();
		}
	}
}
