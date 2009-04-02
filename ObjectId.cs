/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using System.Linq;
using System.Text;
using Gitty.Util;
using System.IO;

namespace Gitty.Lib
{
    [Complete]
    public class ObjectId : AnyObjectId
    {
        private static string ZeroIdString;
        public static ObjectId ZeroId { get; private set; }

        static ObjectId()
        {
            ZeroId = new ObjectId(0, 0, 0, 0, 0);
            ZeroIdString = ZeroId.ToString();
        }


        public static bool IsId(string id)
        {
            if (id.Length != 2 * Constants.ObjectIdLength)
                return false;

            try
            {
                for (int k = id.Length - 1; k >= 0; k--)
                    if (Hex.HexCharToValue(id[k]) == byte.MaxValue)
                        return false;
                    return true;
            }catch(IndexOutOfRangeException){
                return false;
            }
        }

        public static string ToString(ObjectId i)
        {
            return i != null ? i.ToString() : ZeroIdString;
        }
        public static ObjectId FromString(byte[] bs, int offset)
        {
            return FromHexString(bs, offset);
        }

        public static ObjectId FromString(string s)
        {
            if (s.Length != Constants.StringLength)
                throw new ArgumentException("Invalid id: " + s);
            return FromHexString(ASCIIEncoding.ASCII.GetBytes(s), 0);
        }

        public static ObjectId FromHexString(byte[] bs, int offset)
        {
            try
            {
                int a = Hex.HexStringToUInt32(bs, offset);
                int b = Hex.HexStringToUInt32(bs, offset + 8);
                int c = Hex.HexStringToUInt32(bs, offset + 16);
                int d = Hex.HexStringToUInt32(bs, offset + 24);
                int e = Hex.HexStringToUInt32(bs, offset + 32);
                return new ObjectId(a, b, c, d, e);
            }
            catch (IndexOutOfRangeException)
            {
                string s = new string(Encoding.ASCII.GetChars(bs, offset, Constants.StringLength));
                throw new ArgumentException("Invalid id: " + s, "bs");
            }
        }

        protected ObjectId(int new_1, int new_2, int new_3, int new_4, int new_5)
        {
            this.W1 = new_1;
            this.W2 = new_2;
            this.W3 = new_3;
            this.W4 = new_4;
            this.W5 = new_5;
        }

        public ObjectId(AnyObjectId src)
        {
            this.W1 = src.W1;
            this.W2 = src.W2;
            this.W3 = src.W3;
            this.W4 = src.W4;
            this.W5 = src.W5;
        }



        public override ObjectId ToObjectId()
        {
            return this; ;
        }

        public static ObjectId FromRaw(byte[] buffer)
        {
            return FromRaw(buffer, 0);
        }

        public static ObjectId FromRaw(byte[] buffer, int offset)
        {
            int a = NB.DecodeInt32(buffer, offset);
            int b = NB.DecodeInt32(buffer, offset + 4);
            int c = NB.DecodeInt32(buffer, offset + 8);
            int d = NB.DecodeInt32(buffer, offset + 12);
            int e = NB.DecodeInt32(buffer, offset + 16);
            return new ObjectId(a, b, c, d, e);
        }
        public static ObjectId FromRaw(int[] intbuffer)
        {
            return FromRaw(intbuffer, 0);
        }

        public static ObjectId FromRaw(int[] intbuffer, int offset)
        {
            return new ObjectId(intbuffer[offset], intbuffer[offset + 1], intbuffer[offset + 2], intbuffer[offset + 3], intbuffer[offset + 4]);
        }
    }
}
