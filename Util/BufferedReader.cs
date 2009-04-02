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
using System.IO;

namespace Gitty.Core.Util
{
    public class BufferedReader : TextReader
    {
        private TextReader _reader;
        private char[] _buffer;
        private int _bufferPosition;
        private int _readPosition;
        private int _eofPosition;
        private int _markLevel;
        private int[] _marks;
        private int _bufferSize;


        public const int DEFAULT_BUFFER_LEN = 4096;

        public BufferedReader(Stream stream)
            : this(new StreamReader(stream))
        {
            
        }

        public BufferedReader(string filename)
            : this(new StreamReader(filename))
        {

        }

        public BufferedReader(TextReader reader)
            : this(reader, DEFAULT_BUFFER_LEN)
        {

        }

        public BufferedReader(TextReader reader, int bufferSize)            
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (bufferSize < 0) throw new ArgumentOutOfRangeException("bufferSize");

            this._bufferSize = bufferSize;
            this._reader = reader;
            this._buffer = new char[_bufferSize];
            this._buffer[0] = Char.MinValue;
            this._bufferPosition = 0;
            this._readPosition = 0;
            this._eofPosition = -1;
            this._markLevel = 0;
            this._marks = new int[32];
        }

        public char CurrentChar
        {
            get
            {
                if (_bufferPosition == 0)
                    throw new InvalidOperationException("A Read operation must occur prior to accessing the CurrentChar field.");
                return this._buffer[this._bufferPosition - 1];
            }
        }


        /// <summary>
        /// Gets a string from the last Mark() to the current character
        /// </summary>
        /// <returns></returns>
        public string GetString()
        {
            int begin = this._marks[this._markLevel - 1];
            int end = this._bufferPosition;

            return new String(this._buffer, begin, end - begin);
        }

        /// <summary>
        /// Returns the current character then
        /// reads the next character in the stream.
        /// 
        /// 
        /// </summary>
        /// <returns>-1 if no more chars are available or the next character</returns>
        public override int Read()
        {
            _bufferPosition++;
            if (PastEndOfBuffer())
                if (!FillBuffer())
                {
                    //_bufferPosition = _eofPosition;
                    return -1;
                }

            return CurrentChar;
        }

        public override int Peek()
        {
            if (PastEndOfBuffer(1))
                if (!FillBuffer())
                    return -1;

            return this._buffer[this._bufferPosition];
        }
        protected override void Dispose(bool disposing)
        {
            _reader.Dispose();
        }

        public bool ReadIf(int c)
        {
            if (this.Peek() == c)
            {
                this.Read();
                return true;
            }
            else
            {
                return false;
            }
        }

        public int ReadIf(Predicate<int> match)
        {
            if (match(this.Peek()))
            {
                return this.Read();
            }
            else
            {
                return -1;
            }
        }
        /// <summary>
        /// Checks the next character for a match, if it matches then it reads in the character
        /// </summary>
        /// <param name="match">delegate to match with in the form of bool(int)</param>
        /// <returns>Returns the number of matched characters</returns>
        public int ReadWhile(Predicate<int> match)
        {
            int i = 0;
            while (match(this.Peek()))
            {
                this.Read();
                i++;
            }
            return i;
        }


        /// <summary>
        /// Checks the next character for a match, if it matches then it reads in the character
        /// </summary>
        /// <param name="match">delegate to match with in the form of bool(int)</param>
        /// <returns>Returns the number of matched characters</returns>
        public int ReadWhile(int maxReads, Predicate<int> match)
        {
            int i = 0;
            while (match(this.Peek()) && maxReads-- > 0)
            {
                this.Read();
                i++;
            }
            return i;
        }

        private bool PastEndOfBuffer()
        {
            return PastEndOfBuffer(0);
        }
        /// <summary>
        /// Returns a value indicating is the bufferPosition is more then the number of characters read into the buffer.
        /// </summary>
        /// <returns></returns>
        private bool PastEndOfBuffer(int peekOffset)
        {
            return _bufferPosition > _readPosition + peekOffset;
        }

        /// <summary>
        /// Private function to fill the character buffer. Doubles the buffer sizeif it is not big enough.
        /// </summary>
        private bool FillBuffer()
        {
            if (_bufferPosition >= _eofPosition && _eofPosition != -1)
                return false;

            if (_bufferPosition > _buffer.Length) //double buffer
            {
                char[] charBuffer = new char[_buffer.Length * 2];
                Buffer.BlockCopy(_buffer, 0, charBuffer, 0, _readPosition * 2);
                _buffer = charBuffer;
            }

            int charsRead = _reader.Read(_buffer, _readPosition, _buffer.Length - _readPosition);
            if (charsRead == 0)
            {
                _eofPosition = _readPosition + 1;
                return false;
            }

            _readPosition += charsRead;
            return true;
        }

        /// <summary>
        /// Gets the character prior to the last character. If there is no previous character it returns -1;
        /// </summary>
        /// <returns></returns>
        public int GetPrevious()
        {
            if (this._bufferPosition < 2)
                return -1;
            else
                return this._buffer[this._bufferPosition - 2];
        }

        /// <summary>
        /// Marks a location in the stream. This can be used for marking undo points to go back in the event of incorrect parsing or a failure.
        /// </summary>        
        public void Mark()
        {
            if (_markLevel == 32) throw new InvalidOperationException("Cannot create more then 32 marks deep.");
            this._marks[this._markLevel] = this._bufferPosition;
            this._markLevel++;
        }

        /// <summary>
        /// Unmarks the last location in the stream that was set.
        /// </summary>        
        public void Unmark()
        {
            if (this._markLevel < 1)
                throw new InvalidOperationException("No more marks to unmark");

            this._markLevel--;
        }

        public Marker GetMarker()
        {
            return new Marker(this);
        }

        /// <summary>
        /// Reverts reading back to the previous mark. Performs an Unmark.
        /// </summary>
        public void Reset()
        {
            Unmark();
            this._bufferPosition = this._marks[this._markLevel];
        }

        public class Marker : IDisposable
        {
            private BufferedReader _reader;
            private bool _resetCalled;

            public Marker(BufferedReader reader)
            {
                if (reader == null) throw new ArgumentNullException("reader");
                _reader = reader;
                _reader.Mark();
            }

            public string GetString()
            {
                return _reader.GetString();
            }

            public void Reset()
            {
                _reader.Reset();
                _resetCalled = true;
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (!_resetCalled)
                    _reader.Unmark();
            }

            #endregion
        }

    }

}
