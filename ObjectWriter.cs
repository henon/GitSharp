/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.Security.Cryptography;
using System.Text;
using ObjectWritingException = Gitty.Core.Exceptions.ObjectWritingException;

namespace Gitty.Core
{
    public class ObjectWriter
    {
		private static byte[] htree = Encoding.ASCII.GetBytes("tree");

		private static byte[] hparent = Encoding.ASCII.GetBytes("parent");
	
		private static byte[] hauthor = Encoding.ASCII.GetBytes("author");
	
		private static byte[] hcommitter = Encoding.ASCII.GetBytes("committer");
	
		private static byte[] hencoding = Encoding.ASCII.GetBytes("encoding");
	
		private Repository r;
	
		private byte[] buf;
	
		private SHA1CryptoServiceProvider md;
		
        public ObjectWriter(Repository repo)
        {
            this.r = repo;
			buf = new byte[8192];
			md = new SHA1CryptoServiceProvider();			
        }
		
		public ObjectId WriteBlob(byte[] b)
		{
			return WriteBlob(b.Length, new MemoryStream(b));
		}
		
		public ObjectId WriteBlob(FileInfo fileInfo)
        {
        	using(var input = fileInfo.OpenRead())
			{
				return WriteBlob(fileInfo.Length, input);
			}
        }
		
		public ObjectId WriteBlob(long len, Stream input)
		{
			return WriteObject(ObjectType.Blob, len, input, true);
		}

		public ObjectId WriteTree(Tree t)
        {
			var m = new MemoryStream();
            var o = new BinaryWriter(m);
			TreeEntry[] items = t.Members;
			for (int k = 0; k < items.Length; k++) {
				 TreeEntry e = items[k];
				 ObjectId id = e.Id;
	
				if (id == null)
					throw new ObjectWritingException("Object at path \""
							+ e.FullName + "\" does not have an id assigned."
							+ "  All object ids must be assigned prior"
							+ " to writing a tree.");
	
				e.Mode.CopyTo(o);
				o.Write((byte)' ');
				o.Write(e.NameUTF8);
				o.Write((byte)0);
				id.CopyTo(m);
			}
			return WriteCanonicalTree(m.ToArray());
		}
		
		public ObjectId WriteCanonicalTree(byte[] buffer)
		{
			return WriteTree(buffer.Length, new MemoryStream(buffer));
		}
		
		private ObjectId WriteTree(long len, Stream input)
		{
			return WriteObject(ObjectType.Tree, len, input, true);
		}
		
		public ObjectId WriteCommit(Commit c)
        {
#warning TODO: handle encoding
			var os = new MemoryStream();
			var w = new BinaryWriter(os);
			var sw = new StreamWriter(os);
			
			w.Write(htree);
			w.Write(' ');
			c.TreeId.CopyTo(os);
			w.Write('\n');
	
			ObjectId[] ps = c.ParentIds;
			for (int i=0; i<ps.Length; ++i) {
				w.Write(hparent);
				w.Write(' ');
				ps[i].CopyTo(os);
				w.Write('\n');
			}
	
			w.Write(hauthor);
			w.Write(' ');
			sw.Write(c.Author.ToExternalString());
			w.Write('\n');
	
			w.Write(hcommitter);
			w.Write(' ');
			sw.Write(c.Committer.ToExternalString());
			
			w.Write('\n');
	
			w.Write('\n');
			sw.Write(c.Message);
			
			return WriteCommit(os.ToArray());
        }
		
		private ObjectId WriteTag(byte[] b)
		{
			return WriteTag(b.Length, new MemoryStream(b));
		}
		
        public ObjectId WriteTag(Tag tag)
        {
            var stream = new MemoryStream();
			var w = new BinaryWriter(stream);
			var sw = new StreamWriter(stream);
			
			sw.Write("object ");
			tag.Id.CopyTo(stream);
			w.Write('\n');
	
			sw.Write("type ");
			w.Write(tag.TagType);
			w.Write('\n');
	
			sw.Write("tag ");
			tag.TagId.CopyTo(stream);
			w.Write('\n');
	
			sw.Write("tagger ");
			sw.Write(tag.Author.ToExternalString());
			w.Write('\n');
	
			w.Write('\n');
			sw.Write(tag.Message);
			w.Close();
	
			return WriteTag(stream.ToArray());
        }

        private ObjectId WriteCommit(byte[] b)
		{
			return WriteCommit(b.Length, new MemoryStream(b));
		}
		
		private ObjectId WriteCommit( long len, Stream input)
		{
			return WriteObject(ObjectType.Commit, len, input, true);
		}
		
		private ObjectId WriteTag(long len, Stream input)
		{
			return WriteObject(ObjectType.Tag, len, input, true);
		}
        
		public ObjectId ComputeBlobSha1(long length, Stream input)
		{
			return WriteObject(ObjectType.Blob, length, input, false);
		}
		
		private ObjectId WriteObject(ObjectType type, long len, Stream input, bool store) 
		{
			throw new NotImplementedException();
		}
    }
}
