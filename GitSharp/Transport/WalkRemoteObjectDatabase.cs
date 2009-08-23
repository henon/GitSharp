/*
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

using System.Collections.Generic;
using System.IO;
using System.Text;
using GitSharp.Exceptions;
using GitSharp.Util;

namespace GitSharp.Transport
{
    public abstract class WalkRemoteObjectDatabase
    {
        public const string ROOT_DIR = "../";
        public const string INFO_PACKS = "info/packs";
        public const string INFO_ALTERNATES = "info/alternates";
        public const string INFO_HTTP_ALTERNATES = "info/http-alternates";
        public static string INFO_REFS = ROOT_DIR + Constants.INFO_REFS;

        public abstract URIish getURI();
        public abstract List<string> getPackNames();
        public abstract List<WalkRemoteObjectDatabase> getAlternates();
        public abstract FileStream open(string path);
        public abstract WalkRemoteObjectDatabase openAlternate(string location);
        public abstract void close();

        public virtual void deleteFile(string path)
        {
            throw new IOException("Deleting '" + path + "' not supported");
        }

        public virtual FileStream writeFile(string path, IProgressMonitor monitor, string monitorTask)
        {
            throw new IOException("Writing of '" + path + "' not supported.");
        }

        public void writeFile(string path, byte[] data)
        {
            FileStream fs = writeFile(path, null, null);
            try
            {
                fs.Write(data, 0, data.Length);
            }
            finally
            {
                fs.Close();
            }
        }

        public void deleteRef(string name)
        {
            deleteFile(ROOT_DIR + name);
        }

        public void deleteRefLog(string name)
        {
            deleteFile(ROOT_DIR + Constants.LOGS + "/" + name);
        }

        public void writeRef(string name, ObjectId value)
        {
            MemoryStream m = new MemoryStream(Constants.OBJECT_ID_LENGTH * 2 + 1);
            BinaryWriter b = new BinaryWriter(m);
            value.CopyTo(b);
            b.Write('\n');
            b.Flush();

            writeFile(ROOT_DIR + name, m.ToArray());
        }

        public void writeInfoPacks(List<string> packNames)
        {
            StringBuilder w = new StringBuilder();
            foreach (string n in packNames)
            {
                w.Append("P ");
                w.Append(n);
                w.Append('\n');
            }

            writeFile(INFO_PACKS, Constants.encodeASCII(w.ToString()));
        }

        public StreamReader openReader(string path)
        {
            FileStream s  = open(path);
            // StreamReader buffers itself
            return new StreamReader(s, Constants.CHARSET);
        }

        public List<WalkRemoteObjectDatabase> readAlternates(string listPath)
        {
            StreamReader sr = openReader(listPath);
            try
            {
                List<WalkRemoteObjectDatabase> alts = new List<WalkRemoteObjectDatabase>();
                for (;;)
                {
                    string line = sr.ReadLine();
                    if (line == null) break;
                    if (!line.EndsWith("/"))
                        line += "/";
                    alts.Add(openAlternate(line));
                }
                return alts;
            }
            finally
            {
                sr.Close();
            }
        }

        public void readPackedRefs(Dictionary<string, Ref> avail)
        {
            try
            {
                StreamReader sr = openReader(ROOT_DIR + Constants.PACKED_REFS);
                try
                {
                    readPackedRefsImpl(avail, sr);
                }
                finally
                {
                    sr.Close();
                }
            }
            catch (FileNotFoundException notPacked)
            {
                
            }
            catch (IOException e)
            {
                throw new TransportException(getURI(), "error in packed-refs", e);
            }
        }

        private void readPackedRefsImpl(Dictionary<string, Ref> avail, StreamReader sr)
        {
            Ref last = null;
            for (;;)
            {
                string line = sr.ReadLine();

                if (line == null)
                    break;
                if (line[0] == '#')
                    continue;

                if (line[0] == '^')
                {
                    if (last == null)
                        throw new TransportException("Peeled line before ref");
                    ObjectId pid = ObjectId.FromString(line.Substring(1));
                    last = new Ref(Ref.Storage.Packed, last.Name, last.ObjectId, pid, true);
                    avail.Add(last.Name, last);
                    continue;
                }

                int sp = line.IndexOf(' ');
                if (sp < 0)
                    throw new TransportException("Unrecognized ref: " + line);
                ObjectId id = ObjectId.FromString(line.Slice(0, sp));
                string name = line.Substring(sp + 1);
                last = new Ref(Ref.Storage.Packed, name, id);
                avail.Add(last.Name, last);
            }
        }
    }
}