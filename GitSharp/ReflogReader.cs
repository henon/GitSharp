/*
 * Copyright (C) 2009, Robin Rosenberg
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
using GitSharp.Util;

namespace GitSharp
{
	/// <summary>
	/// Utility for reading reflog entries.
	/// </summary>
	public class ReflogReader
	{
		private readonly FileInfo _logName;

		///	<summary>
		/// Parsed reflog entry.
		/// </summary>
		public ReflogReader(Repository db, string refname)
		{
			_logName = new FileInfo(
				Path.Combine(db.Directory.FullName, 
					Path.Combine("logs", refname.Replace("/", Path.DirectorySeparatorChar.ToString()))));
		}

		///	<summary>
		/// Get the last entry in the reflog.
		/// </summary>
		/// <returns>The latest reflog entry, or null if no log.</returns>
		/// <exception cref="IOException"></exception>
		public Entry getLastEntry()
		{
			var entries = getReverseEntries(1);
			return entries.Count > 0 ? entries[0] : null;
		}

		/// <summary></summary>
		/// <returns> all reflog entries in reverse order.
		/// </returns>
		/// <exception cref="IOException"></exception>
		public IList<Entry> getReverseEntries()
		{
			return getReverseEntries(int.MaxValue);
		}

		///	<param name="max">Max number of entries to read.</param>
		///	<returns>All reflog entries in reverse order.</returns>
		///	<exception cref="IOException"></exception>
		public IList<Entry> getReverseEntries(int max)
		{
			byte[] log;

			try
			{
				log = NB.ReadFully(_logName);
			}
			catch (FileNotFoundException)
			{
				return new List<Entry>();
			}

			var ret = new List<Entry>();
			int rs = RawParseUtils.prevLF(log, log.Length);

			while (rs >= 0 && max-- > 0)
			{
				rs = RawParseUtils.prevLF(log, rs);
				rs = rs < 0 ? 0 : rs + 2;
				var entry = new Entry(log, rs);
				ret.Add(entry);
			}

			return ret;
		}

		#region Nested Types

		public class Entry
		{
			private readonly ObjectId _oldId;
			private readonly ObjectId _newId;
			private readonly PersonIdent _who;
			private readonly string _comment;

			public Entry(byte[] raw, int pos)
			{
				_oldId = ObjectId.FromString(raw, pos);
				pos += Constants.OBJECT_ID_LENGTH * 2;
				if (raw[pos++] != ' ')
				{
					throw new ArgumentException("Raw log message does not parse as log entry");
				}

				_newId = ObjectId.FromString(raw, pos);
				pos += Constants.OBJECT_ID_LENGTH * 2;
				if (raw[pos++] != ' ')
				{
					throw new ArgumentException("Raw log message does not parse as log entry");
				}

				_who = RawParseUtils.parsePersonIdentOnly(raw, pos);
				int p0 = RawParseUtils.next(raw, pos, '\t');

				if (p0 == -1)
				{
					throw new ArgumentException("Raw log message does not parse as log entry");
				}

				int p1 = RawParseUtils.nextLF(raw, p0);
				if (p1 == -1)
				{
					throw new ArgumentException("Raw log message does not parse as log entry");
				}

				_comment = RawParseUtils.decode(raw, p0, p1 - 1);
			}

			/// <summary>
			/// Gets the commit id before the change.
			/// </summary>
			public ObjectId OldId
			{
				get { return _oldId; }
			}

			/// <summary>
			/// Gets the commit id after the change.
			/// </summary>
			public ObjectId NewId
			{
				get { return _newId; }
			}

			/// <summary>
			/// Gets the user performing the change.
			/// </summary>
			public PersonIdent Who
			{
				get { return _who; }
			}

			/// <summary>
			/// Gets the textual description of the change.
			/// </summary>
			public string Comment
			{
				get { return _comment; }
			}

			public override string ToString()
			{
				return "Entry[" + _oldId.Name + ", " + _newId.Name + ", " + Who + ", " + Comment + "]";
			}
		}

		#endregion
	}
}