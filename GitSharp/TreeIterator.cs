/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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
using System.IO;
using System.Linq;
using System.Text;

namespace GitSharp
{
    public class TreeIterator : IEnumerator<TreeEntry>
    {

        private Tree tree;

        private int index;

        private TreeIterator sub;

        private Order order;

        private bool visitTreeNodes;

        private bool hasVisitedTree;

        /**
         * Traversal order
         */
        public enum Order
        {
            /**
             * Visit node first, then leaves
             */
            PREORDER,

            /**
             * Visit leaves first, then node
             */
            POSTORDER
        };

        /**
         * Construct a {@link TreeIterator} for visiting all non-tree nodes.
         *
         * @param start
         */
        public TreeIterator(Tree start) :
            this(start, Order.PREORDER, false)
        {
        }

        /**
         * Construct a {@link TreeIterator} visiting all nodes in a tree in a given
         * order.
         *
         * @param start Root node
         * @param order {@link Order}
         */
        public TreeIterator(Tree start, Order order)
            : this(start, order, true)
        {
        }

        /**
         * Construct a {@link TreeIterator}
         *
         * @param start First node to visit
         * @param order Visitation {@link Order}
         * @param visitTreeNode True to include tree node
         */
        private TreeIterator(Tree start, Order order, bool visitTreeNode)
        {
            tree = start;
            visitTreeNodes = visitTreeNode;
            index = -1;
            this.order = order;
            if (!visitTreeNodes)
                hasVisitedTree = true;

            try
            {
                Step();
            }
            catch (IOException e)
            {
                throw new Exception(string.Empty, e);
            }
        }

        public bool MoveNext()
        {
            try
            {
                TreeEntry ret = NextTreeEntry();
                Step();
                Current = ret;
                return true;
            }
            catch (IOException e)
            {
                throw new Exception(string.Empty, e);
            }
        }

        private TreeEntry NextTreeEntry()
        {
            if (sub != null)
                return sub.NextTreeEntry();
            
            if (index < 0 && order == Order.PREORDER)
                return tree;

            if (order == Order.POSTORDER && index == tree.MemberCount)
                return tree;

            return tree.Members[index];
        }

        // Commented out since hasNext is not used my IEnumerator
        //
        //public bool hasNext()
        //{
        //    try
        //    {
        //        return HasNextTreeEntry();
        //    }
        //    catch (IOException e)
        //    {
        //        throw new Exception(string.Empty, e);
        //    }
        //}

        private bool HasNextTreeEntry()
        {
            if (tree == null)
                return false;

            return sub != null || index < tree.MemberCount || order == Order.POSTORDER && index == tree.MemberCount;
        }

        private bool Step()
        {
            if (tree == null)
                return false;

            if (sub != null)
            {
                if (sub.Step())
                    return true;
                sub = null;
            }

            if (index < 0 && !hasVisitedTree && order == Order.PREORDER)
            {
                hasVisitedTree = true;
                return true;
            }

            while (++index < tree.MemberCount)
            {
                TreeEntry e = tree.Members[index];
                if (e is Tree)
                {
                    sub = new TreeIterator((Tree)e, order, visitTreeNodes);
                    if (sub.HasNextTreeEntry())
                        return true;
                    sub = null;
                    continue;
                }
                return true;
            }

            if (index == tree.MemberCount && !hasVisitedTree && order == Order.POSTORDER)
            {
                hasVisitedTree = true;
                return true;
            }
            return false;
        }

        // 
        //public void remove()
        //{
        //    throw new InvalidOperationException("TreeIterator does not suppport remove()");
        //}


        #region IEnumerator<TreeEntry> Members

        public TreeEntry Current{ get; internal set; }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
