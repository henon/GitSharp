/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

using System;
using System.IO;
using System.Text;
using GitSharp.Exceptions;
using GitSharp.Util;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace GitSharp
{
    public class ObjectWriter
    {
        // Fields
        private static readonly byte[] HAuthor = Encoding.ASCII.GetBytes("author");
        private static readonly byte[] HCommitter = Encoding.ASCII.GetBytes("committer");
        private static readonly byte[] HEncoding = Encoding.ASCII.GetBytes("encoding");
        private static readonly byte[] HParent = Encoding.ASCII.GetBytes("parent");
        private static readonly byte[] HTree = Encoding.ASCII.GetBytes("tree");
        private readonly byte[] _buf;
        private readonly Deflater _def;
        private readonly MessageDigest _md;
        private readonly Repository _r;

        // Methods
        public ObjectWriter(Repository repo)
        {
            _r = repo;
            _buf = new byte[0x2000];
            _md = new MessageDigest();
            _def = new Deflater(_r.Config.getCore().getCompression());
        }

        public ObjectId ComputeBlobSha1(long length, Stream input)
        {
            return WriteObject(ObjectType.Blob, length, input, false);
        }

        public ObjectId WriteBlob(FileInfo fileInfo)
        {
            using (FileStream stream = fileInfo.OpenRead())
            {
                return WriteBlob(fileInfo.Length, stream);
            }
        }

        public ObjectId WriteBlob(byte[] b)
        {
            return WriteBlob(b.Length, new MemoryStream(b));
        }

        public ObjectId WriteBlob(long len, Stream input)
        {
            return WriteObject(ObjectType.Blob, len, input, true);
        }

        public ObjectId WriteCanonicalTree(byte[] buffer)
        {
            return WriteTree(buffer.Length, new MemoryStream(buffer));
        }

        public ObjectId WriteCommit(Commit c)
        {
            Encoding encoding = c.Encoding ?? Constants.CHARSET;
            var output = new MemoryStream();
            var s = new BinaryWriter(output, encoding);
            s.Write(HTree);
            s.Write(' ');
            c.TreeId.CopyTo(s);
            s.Write('\n');
            ObjectId[] parentIds = c.ParentIds;
            for (int i = 0; i < parentIds.Length; i++)
            {
                s.Write(HParent);
                s.Write(' ');
                parentIds[i].CopyTo(s);
                s.Write('\n');
            }
            s.Write(HAuthor);
            s.Write(' ');
            s.Write(c.Author.ToExternalString().ToCharArray());
            s.Write('\n');
            s.Write(HCommitter);
            s.Write(' ');
            s.Write(c.Committer.ToExternalString().ToCharArray());
            s.Write('\n');
            if (encoding != Encoding.UTF8)
            {
                s.Write(HEncoding);
                s.Write(' ');
                s.Write(Constants.encodeASCII(encoding.HeaderName.ToUpper()));
                s.Write('\n');
            }
            s.Write('\n');
            s.Write(c.Message.ToCharArray());
            return WriteCommit(output.ToArray());
        }

        private ObjectId WriteCommit(byte[] b)
        {
            return WriteCommit(b.Length, new MemoryStream(b));
        }

        private ObjectId WriteCommit(long len, Stream input)
        {
            return WriteObject(ObjectType.Commit, len, input, true);
        }

        internal ObjectId WriteObject(ObjectType type, long len, Stream input, bool store)
        {
            FileInfo info;
            DeflaterOutputStream stream;
            FileStream stream2;
            ObjectId objectId = null;
            if (store)
            {
                info = _r.ObjectsDirectory.CreateTempFile("noz");
                stream2 = info.OpenWrite();
            }
            else
            {
                info = null;
                stream2 = null;
            }
            _md.Reset();
            if (store)
            {
                _def.Reset();
                stream = new DeflaterOutputStream(stream2, _def);
            }
            else
            {
                stream = null;
            }
            try
            {
                int num;
                byte[] bytes = Codec.EncodedTypeString(type);
                _md.Update(bytes);
                if (stream != null)
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
                _md.Update(0x20);
                if (stream != null)
                {
                    stream.WriteByte(0x20);
                }
                bytes = Encoding.ASCII.GetBytes(len.ToString());
                _md.Update(bytes);
                if (stream != null)
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
                _md.Update(0);
                if (stream != null)
                {
                    stream.WriteByte(0);
                }
                while ((len > 0L) && ((num = input.Read(_buf, 0, (int) Math.Min(len, _buf.Length))) > 0))
                {
                    _md.Update(_buf, 0, num);
                    if (stream != null)
                    {
                        stream.Write(_buf, 0, num);
                    }
                    len -= num;
                }
                if (len != 0L)
                {
                    throw new IOException("Input did not match supplied Length. " + len + " bytes are missing.");
                }
                if (stream != null)
                {
                    stream.Close();
                    if (info != null)
                    {
                        info.IsReadOnly = true;
                    }
                }
                objectId = ObjectId.FromRaw(_md.Digest());
            }
            finally
            {
                if ((objectId == null) && (stream != null))
                {
                    try
                    {
                        stream.Close();
                    }
                    finally
                    {
                        info.DeleteFile();
                    }
                }
            }
            if (info != null)
            {
                if (_r.HasObject(objectId))
                {
                  info.DeleteFile();
                }
                else
                {
                    FileInfo info2 = _r.ToFile(objectId);
                    if (!info.RenameTo(info2.FullName))
                    {
                        if (info2.Directory != null)
                        {
                            info2.Directory.Create();
                        }
                        if (!info.RenameTo(info2.FullName) && !_r.HasObject(objectId))
                        {
                            info.DeleteFile();
                            throw new ObjectWritingException("Unable to create new object: " + info2);
                        }
                    }
                }
            }
            return objectId;
        }

        public ObjectId WriteTag(Tag tag)
        {
            var output = new MemoryStream();
            var s = new BinaryWriter(output);
            s.Write("object ".ToCharArray());
            tag.Id.CopyTo(s);
            s.Write('\n');
            s.Write("type ".ToCharArray());
            s.Write(tag.TagType.ToCharArray());
            s.Write('\n');
            s.Write("tag ".ToCharArray());
            s.Write(tag.TagName.ToCharArray());
            s.Write('\n');
            s.Write("tagger ".ToCharArray());
            s.Write(tag.Author.ToExternalString().ToCharArray());
            s.Write('\n');
            s.Write('\n');
            s.Write(tag.Message.ToCharArray());
            s.Close();
            return WriteTag(output.ToArray());
        }

        private ObjectId WriteTag(byte[] b)
        {
            return WriteTag(b.Length, new MemoryStream(b));
        }

        private ObjectId WriteTag(long len, Stream input)
        {
            return WriteObject(ObjectType.Tag, len, input, true);
        }

        public ObjectId WriteTree(Tree t)
        {
            var output = new MemoryStream();
            var writer = new BinaryWriter(output);
            foreach (TreeEntry entry in t.Members)
            {
                ObjectId id = entry.Id;
                if (id == null)
                {
                    throw new ObjectWritingException("object at path \"" + entry.FullName +
                                                     "\" does not have an id assigned.  All object ids must be assigned prior to writing a tree.");
                }
                entry.Mode.CopyTo(output);
                writer.Write((byte) 0x20);
                writer.Write(entry.NameUTF8);
                writer.Write((byte) 0);
                id.copyRawTo(output);
            }
            return WriteCanonicalTree(output.ToArray());
        }

        private ObjectId WriteTree(long len, Stream input)
        {
            return WriteObject(ObjectType.Tree, len, input, true);
        }
    }
}