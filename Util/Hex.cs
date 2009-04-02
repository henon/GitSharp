/*
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

namespace Gitty.Core.Util
{
    public sealed class Hex
    {
        static byte[] __hexCharToValue;
        static char[] __valueToHexChar;
        static byte[] __valueToHexByte;
        public static int Nibble = 4;
        static Hex()
        {
            __valueToHexChar = new char[] {'0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f'};
            __valueToHexByte = new byte[__valueToHexChar.Length];
            for (int i = 0; i < __valueToHexChar.Length; i++)
                __valueToHexByte[i] = (byte)__valueToHexChar[i];

                __hexCharToValue = new byte['f' + 1];
            for (int i = 0; i < __hexCharToValue.Length; i++)
                __hexCharToValue[i] = byte.MaxValue;
            for (char i = '0'; i <= '9'; i++)
                __hexCharToValue[i] = (byte)(i - '0');
            for (char i = 'a'; i <= 'f'; i++)
                __hexCharToValue[i] = (byte)((i - 'a') + 10);
        }

        public static byte HexCharToValue(Char c)
        {
            return __hexCharToValue[c];
        }

        public static byte HexCharToValue(byte c)
        {
            return __hexCharToValue[c];
        }

        public static int HexStringToUInt32(byte[] bs, int offset)
        {
            int r = __hexCharToValue[bs[offset]];
            r <<= Nibble; // push one nibble

            r |= __hexCharToValue[bs[offset + 1]];
            r <<= Nibble;

            r |= __hexCharToValue[bs[offset + 2]];
            r <<= Nibble;

            r |= __hexCharToValue[bs[offset + 3]];
            r <<= Nibble;

            r |= __hexCharToValue[bs[offset + 4]];
            r <<= Nibble;

            r |= __hexCharToValue[bs[offset + 5]];
            r <<= Nibble;

            r |= __hexCharToValue[bs[offset + 6]];

            int last = __hexCharToValue[bs[offset + 7]];
            if (r < 0 || last < 0)
                throw new IndexOutOfRangeException();

            return (r << Nibble) | last;
        }


        public static void FillHexByteArray(byte[] dest, int offset, int value)
        {
            int curOffset = offset + 7;
            while (curOffset >= offset && value != 0)
            {
                dest[curOffset--] = __valueToHexByte[value & 0xf];
                value >>= Nibble;
            }
            while (curOffset >= offset)
                dest[curOffset--] = __valueToHexByte[0];
        }


        public static void FillHexCharArray(char[] dest, int offset, int value){
            int curOffset = offset + 7;
            while (curOffset >= offset && value != 0)
            {
                dest[curOffset--] = __valueToHexChar[value & 0xf];
                value >>= Nibble;
            }
            while (curOffset >= offset)
                dest[curOffset--] = '0';
        }

    }
}
