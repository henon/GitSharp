/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Thad Hughes <thadh@thad.corp.google.com>
 * Copyright (C) 2009, JetBrains s.r.o.
 * Copyright (C) 2009, Google, Inc.
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
using System.Text;
using GitSharp.Exceptions;
using GitSharp.Util;
using Tamir.SharpSsh.java.lang;

namespace GitSharp
{
	public class Config
	{
		private const long GiB = 1024 * MiB;
		private const long KiB = 1024;
		private const long MiB = 1024 * KiB;
		private static readonly string[] EmptyStringArray = new string[0];
		private static readonly string MagicEmptyValue = string.Empty;

		private readonly Config _baseConfig;
		private State _state;

		public Config()
			: this(null)
		{
		}

		public Config(Config defaultConfig)
		{
			_baseConfig = defaultConfig;
			_state = newState();
		}

		private static string EscapeValue(string x)
		{
			bool inquote = false;
			int lineStart = 0;
			var r = new StringBuilder(x.Length);
			for (int k = 0; k < x.Length; k++)
			{
				char c = x[k];
				switch (c)
				{
					case '\n':
						if (inquote)
						{
							r.Append('"');
							inquote = false;
						}
						r.Append("\\n\\\n");
						lineStart = r.Length;
						break;

					case '\t':
						r.Append("\\t");
						break;

					case '\b':
						r.Append("\\b");
						break;

					case '\\':
						r.Append("\\\\");
						break;

					case '"':
						r.Append("\\\"");
						break;

					case ';':
					case '#':
						if (!inquote)
						{
							r.Insert(lineStart, '"');
							inquote = true;
						}
						r.Append(c);
						break;

					case ' ':
						if (!inquote && r.Length > 0 && r[r.Length - 1] == ' ')
						{
							r.Insert(lineStart, '"');
							inquote = true;
						}
						r.Append(' ');
						break;

					default:
						r.Append(c);
						break;
				}
			}
			if (inquote) r.Append('"');
			return r.ToString();
		}

		public int getInt(string section, string name, int defaultValue)
		{
			return getInt(section, null, name, defaultValue);
		}

		public int getInt(string section, string subsection, string name, int defaultValue)
		{
			long val = getLong(section, subsection, name, defaultValue);
			if (int.MinValue <= val && val <= int.MaxValue)
				return (int)val;
			throw new ArgumentException("Integer value " + section + "." + name + " out of range");
		}

		public long getLong(string section, string subsection, string name, long defaultValue)
		{
			string str = getString(section, subsection, name);
			if (str == null)
			{
				return defaultValue;
			}

			string n = str.Trim();
			if (n.Length == 0)
			{
				return defaultValue;
			}

			long mul = 1;
			switch (StringUtils.toLowerCase(n[n.Length - 1]))
			{
				case 'g':
					mul = GiB;
					break;

				case 'm':
					mul = MiB;
					break;

				case 'k':
					mul = KiB;
					break;
			}

			if (mul > 1)
			{
				n = n.Slice(0, n.Length - 1).Trim();
			}

			if (n.Length == 0)
			{
				return defaultValue;
			}

			try
			{
				return mul * long.Parse(n);
			}
			catch (FormatException nfe)
			{
				throw new ArgumentException("Invalid long value: " + section + "." + name + "=" + str, nfe);
			}
		}

		public bool getBoolean(string section, string name, bool defaultValue)
		{
			return getBoolean(section, null, name, defaultValue);
		}

		public bool getBoolean(string section, string subsection, string name, bool defaultValue)
		{
			string n = getRawString(section, subsection, name);
			if (n == null)
				return defaultValue;

			if (MagicEmptyValue == n
				|| "yes".Equals(n, StringComparison.InvariantCultureIgnoreCase)
				|| "true".Equals(n, StringComparison.InvariantCultureIgnoreCase)
				|| "1".Equals(n, StringComparison.InvariantCultureIgnoreCase)
				|| "on".Equals(n, StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}

			if ("no".Equals(n, StringComparison.InvariantCultureIgnoreCase)
				|| "false".Equals(n, StringComparison.InvariantCultureIgnoreCase)
				|| Constants.RepositoryFormatVersion.Equals(n, StringComparison.InvariantCultureIgnoreCase)
				|| "off".Equals(n, StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}

			throw new ArgumentException("Invalid boolean value: " + section + "." + name + "=" + n);
		}

		public string getString(string section, string subsection, string name)
		{
			return getRawString(section, subsection, name);
		}

		public string[] getStringList(string section, string subsection, string name)
		{
			string[] baseList = _baseConfig != null ? _baseConfig.getStringList(section, subsection, name) : EmptyStringArray;

			List<string> lst = getRawStringList(section, subsection, name);
			if (lst != null)
			{
				var res = new string[baseList.Length + lst.Count];
				int idx = baseList.Length;

				Array.Copy(baseList, 0, res, 0, idx);

				foreach (string val in lst)
				{
					res[idx++] = val;
				}
				return res;
			}

			return baseList;
		}

		public List<string> getSubsections(string section)
		{
			return get(new SubsectionNames(section));
		}

		public T get<T>(ISectionParser<T> parser)
		{
			State myState = getState();
			T obj;
			if (!myState.Cache.ContainsKey(parser))
			{
				obj = parser.Parse(this);
				myState.Cache.Add(parser, obj);
			}
			else
			{
				obj = (T)myState.Cache[parser];
			}
			return obj;
		}

		public void uncache<T>(ISectionParser<T> parser)
		{
			_state.Cache.Remove(parser);
		}

		private string getRawString(string section, string subsection, string name)
		{
			List<string> lst = getRawStringList(section, subsection, name);
			if (lst != null && lst.Count > 0)
				return lst[0];
			if (_baseConfig != null)
				return _baseConfig.getRawString(section, subsection, name);
			return null;
		}

		private List<string> getRawStringList(string section, string subsection, string name)
		{
			List<string> r = null;
			foreach (Entry e in _state.EntryList)
			{
				if (e.Match(section, subsection, name))
					r = add(r, e.Value);
			}
			return r;
		}

		private static List<string> add(List<string> curr, string value)
		{
			if (curr == null)
				return new List<string> { value };

			curr.Add(value);
			return curr;
		}

		private State getState()
		{
			State cur = _state;
			State @base = getBaseState();
			if (cur.BaseState == @base)
				return cur;
			var upd = new State(cur.EntryList, @base);
			_state = upd;
			return upd;
		}

		private State getBaseState()
		{
			return _baseConfig != null ? _baseConfig._state : null;
		}

		public void setInt(string section, string subsection, string name, int value)
		{
			setLong(section, subsection, name, value);
		}

		public void setLong(string section, string subsection, string name, long value)
		{
			string s;
			if (value >= GiB && (value % GiB) == 0)
				s = (value / GiB) + " g";
			else if (value >= MiB && (value % MiB) == 0)
				s = (value / MiB) + " m";
			else if (value >= KiB && (value % KiB) == 0)
				s = (value / KiB) + " k";
			else
				s = value.ToString();

			setString(section, subsection, name, s);
		}

		public void setBoolean(string section, string subsection, string name, bool value)
		{
			setString(section, subsection, name, value ? "true" : "false");
		}

		public void setString(string section, string subsection, string name, string value)
		{
			setStringList(section, subsection, name, new List<string> { value });
		}

		public void unset(string section, string subsection, string name)
		{
			setStringList(section, subsection, name, new List<string>());
		}

		public void setStringList(string section, string subsection, string name, List<string> values)
		{
			State src = _state;
			State res = replaceStringList(src, section, subsection, name, values);
			_state = res;
		}

		private State replaceStringList(State srcState, string section, string subsection, string name, IList<string> values)
		{
			List<Entry> entries = copy(srcState, values);
			int entryIndex = 0;
			int valueIndex = 0;
			int insertPosition = -1;

			while (entryIndex < entries.Count && valueIndex < values.Count)
			{
				Entry e = entries[entryIndex];
				if (e.Match(section, subsection, name))
				{
					entries[entryIndex] = e.ForValue(values[valueIndex++]);
					insertPosition = entryIndex + 1;
				}
				entryIndex++;
			}

			if (valueIndex == values.Count && entryIndex < entries.Count)
			{
				while (entryIndex < entries.Count)
				{
					Entry e = entries[entryIndex++];
					if (e.Match(section, subsection, name))
					{
						entries.RemoveAt(--entryIndex);
					}
				}
			}

			if (valueIndex < values.Count && entryIndex == entries.Count)
			{
				if (insertPosition < 0)
				{
					insertPosition = findSectionEnd(entries, section, subsection);
				}

				if (insertPosition < 0)
				{
					var e = new Entry { Section = section, Subsection = subsection };
					entries.Add(e);
					insertPosition = entries.Count;
				}

				while (valueIndex < values.Count)
				{
					var e = new Entry
								{
									Section = section,
									Subsection = subsection,
									Name = name,
									Value = values[valueIndex++]
								};
					entries.Insert(insertPosition++, e);
				}
			}

			return newState(entries);
		}

		private static List<Entry> copy(State src, ICollection<string> values)
		{
			int max = src.EntryList.Count + values.Count + 1;
			var r = new List<Entry>(max);
			r.AddRange(src.EntryList);
			return r;
		}

		private static int findSectionEnd(IList<Entry> entries, string section, string subsection)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				Entry e = entries[i];
				if (e.Match(section, subsection, null))
				{
					i++;
					while (i < entries.Count)
					{
						e = entries[i];
						if (e.Match(section, subsection, e.Name))
							i++;
						else
							break;
					}
					return i;
				}
			}
			return -1;
		}

		public string toText()
		{
			var o = new StringBuilder();
			foreach (Entry e in _state.EntryList)
			{
				if (e.Prefix != null)
					o.Append(e.Prefix);
				if (e.Section != null && e.Name == null)
				{
					o.Append('[');
					o.Append(e.Section);

					if (e.Subsection != null)
					{
						o.Append(' ');
						o.Append('"');
						o.Append(EscapeValue(e.Subsection));
						o.Append('"');
					}

					o.Append(']');
				}
				else if (e.Section != null && e.Name != null)
				{
					if (e.Prefix == null || string.Empty.Equals(e.Prefix))
					{
						o.Append('\t');
					}

					o.Append(e.Name);

					if (e.Value != null)
					{
						if (MagicEmptyValue != e.Value)
						{
							o.Append(" = ");
							o.Append(EscapeValue(e.Value));
						}
					}

					if (e.Suffix != null)
					{
						o.Append(' ');
					}
				}

				if (e.Suffix != null)
					o.Append(e.Suffix);
				{
					o.Append('\n');
				}
			}
			return o.ToString();
		}

		protected void AddEntry(Entry e)
		{
			_state.EntryList.Add(e);
		}

		public void fromText(string text)
		{
			var newEntries = new List<Entry>();
			var i = new ConfigReader(text);
			Entry last = null;
			var e = new Entry();

			while (true)
			{
				int input = i.Read();
				if (-1 == input)
				{
					break;
				}

				var c = (char)input;
				if ('\n' == c)
				{
					newEntries.Add(e);
					if (e.Section != null)
						last = e;
					e = new Entry();
				}
				else if (e.Suffix != null)
				{
					e.Suffix += c;
				}
				else if (';' == c || '#' == c)
				{
					e.Suffix = new string(c, 1);
				}
				else if (e.Section == null && char.IsWhiteSpace(c))
				{
					if (e.Prefix == null)
					{
						e.Prefix = string.Empty;
					}
					e.Prefix += c;
				}
				else if ('[' == c)
				{
					e.Section = readSectionName(i);
					input = i.Read();
					if ('"' == input)
					{
						e.Subsection = ReadValue(i, true, '"');
						input = i.Read();
					}

					if (']' != input)
					{
						throw new ConfigInvalidException("Bad group header");
					}

					e.Suffix = string.Empty;
				}
				else if (last != null)
				{
					e.Section = last.Section;
					e.Subsection = last.Subsection;
					i.Reset();
					e.Name = readKeyName(i);
					if (e.Name.EndsWith("\n"))
					{
						e.Name = e.Name.Slice(0, e.Name.Length - 1);
						e.Value = MagicEmptyValue;
					}
					else
					{
						e.Value = ReadValue(i, false, -1);
					}
				}
				else
				{
					throw new ConfigInvalidException("Invalid line in config file");
				}
			}

			_state = newState(newEntries);
		}

		private static string ReadValue(ConfigReader i, bool quote, int eol)
		{
			var value = new StringBuilder();
			bool space = false;
			for (; ; )
			{
				int c = i.Read();
				if (c < 0)
				{
					if (value.Length == 0)
						throw new ConfigInvalidException("Unexpected end of config file");
					break;
				}

				if ('\n' == c)
				{
					if (quote)
						throw new ConfigInvalidException("Newline in quotes not allowed");
					i.Reset();
					break;
				}

				if (eol == c)
					break;

				if (!quote)
				{
					if (char.IsWhiteSpace((char)c))
					{
						space = true;
						continue;
					}

					if (';' == c || '#' == c)
					{
						i.Reset();
						break;
					}
				}

				if (space)
				{
					if (value.Length > 0)
						value.Append(' ');
					space = false;
				}

				if ('\\' == c)
				{
					c = i.Read();
					switch (c)
					{
						case -1:
							throw new ConfigInvalidException("End of file in escape");

						case '\n':
							continue;

						case 't':
							value.Append('\t');
							continue;

						case 'b':
							value.Append('\b');
							continue;

						case 'n':
							value.Append('\n');
							continue;

						case '\\':
							value.Append('\\');
							continue;

						case '"':
							value.Append('"');
							continue;

						default:
							throw new ConfigInvalidException("Bad escape: " + ((char)c));
					}
				}

				if ('"' == c)
				{
					quote = !quote;
					continue;
				}

				value.Append((char)c);
			}
			return value.Length > 0 ? value.ToString() : null;
		}

		private State newState()
		{
			return new State(new List<Entry>(), getBaseState());
		}

		private State newState(List<Entry> entries)
		{
			return new State(entries, getBaseState());
		}

		protected void clear()
		{
			_state = newState();
		}

		private static string readSectionName(ConfigReader i)
		{
			var name = new StringBuilder();
			for (; ; )
			{
				int c = i.Read();
				if (c < 0)
					throw new ConfigInvalidException("Unexpected end of config file");

				if (']' == c)
				{
					i.Reset();
					break;
				}

				if (' ' == c || '\t' == c)
				{
					for (; ; )
					{
						c = i.Read();
						if (c < 0)
							throw new ConfigInvalidException("Unexpected end of config file");

						if ('"' == c)
						{
							i.Reset();
							break;
						}

						if (' ' == c || '\t' == c)
						{
							continue;
						}
						throw new ConfigInvalidException("Bad section entry: " + name);
					}
					break;
				}

				if (char.IsLetterOrDigit((char)c) || '.' == c || '-' == c)
					name.Append((char)c);
				else
					throw new ConfigInvalidException("Bad section entry: " + name);
			}
			return name.ToString();
		}

		private static string readKeyName(ConfigReader i)
		{
			var name = new StringBuffer();
			for (; ; )
			{
				int c = i.Read();
				if (c < 0)
					throw new ConfigInvalidException("Unexpected end of config file");

				if ('=' == c)
					break;

				if (' ' == c || '\t' == c)
				{
					for (; ; )
					{
						c = i.Read();
						if (c < 0)
							throw new ConfigInvalidException("Unexpected end of config file");

						if ('=' == c)
							break;

						if (';' == c || '#' == c || '\n' == c)
						{
							i.Reset();
							break;
						}

						if (' ' == c || '\t' == c)
							continue;
						throw new ConfigInvalidException("Bad entry delimiter");
					}
					break;
				}

				if (char.IsLetterOrDigit((char)c) || c == '-')
				{
					name.append((char)c);
				}
				else if ('\n' == c)
				{
					i.Reset();
					name.append((char)c);
					break;
				}
				else
					throw new ConfigInvalidException("Bad entry name: " + name);
			}

			return name.ToString();
		}

		#region Nested type: ConfigReader

		private class ConfigReader
		{
			private readonly string data;
			private readonly int len;
			private int position;

			public ConfigReader(string text)
			{
				data = text;
				len = data.Length;
			}

			public int Read()
			{
				int ret = -1;
				if (position < len)
				{
					ret = data[position];
					position++;
				}
				return ret;
			}

			public void Reset()
			{
				// no idea what the java pendant actually does..
				//position = 0;
				--position;
			}
		}

		#endregion

		#region Nested type: Entry

		protected class Entry
		{
			public string Name;
			public string Prefix;
			public string Section;
			public string Subsection;
			public string Suffix;
			public string Value;

			public Entry ForValue(string newValue)
			{
				var e = new Entry
							{
								Prefix = Prefix,
								Section = Section,
								Subsection = Subsection,
								Name = Name,
								Value = newValue,
								Suffix = Suffix
							};
				return e;
			}

			public bool Match(string aSection, string aSubsection, string aKey)
			{
				return EqualsIgnoreCase(Section, aSection)
					   && EqualsSameCase(Subsection, aSubsection)
					   && EqualsIgnoreCase(Name, aKey);
			}

			private static bool EqualsIgnoreCase(string a, string b)
			{
				if (a == null && b == null) return true;
				if (a == null || b == null) return false;

				return a.Equals(b, StringComparison.InvariantCultureIgnoreCase);
			}

			private static bool EqualsSameCase(string a, string b)
			{
				if (a == null && b == null) return true;
				if (a == null || b == null) return false;

				return a.Equals(b, StringComparison.InvariantCulture);
			}
		}

		#endregion

		#region Nested type: ISectionParser

		public interface ISectionParser<T>
		{
			T Parse(Config cfg);
		}

		#endregion

		#region Nested type: State

		private class State
		{
			public readonly State BaseState;
			public readonly Dictionary<object, object> Cache;
			public readonly List<Entry> EntryList;

			public State(List<Entry> entries, State baseState)
			{
				EntryList = entries;
				Cache = new Dictionary<object, object>();
				BaseState = baseState;
			}
		}

		#endregion

		#region Nested type: SubsectionNames

		private class SubsectionNames : ISectionParser<List<string>>
		{
			private readonly string _section;

			public SubsectionNames(string sectionName)
			{
				_section = sectionName;
			}

			#region ISectionParser<List<string>> Members

			public List<string> Parse(Config cfg)
			{
				var result = new List<string>();
				while (cfg != null)
				{
					foreach (Entry e in cfg._state.EntryList)
					{
						if (e.Subsection != null && e.Name == null && 
							_section.Equals(e.Section, StringComparison.InvariantCultureIgnoreCase))
						{
							result.Add(e.Subsection);
						}
					}
					cfg = cfg._baseConfig;
				}
				return result;
			}

			#endregion

			public override int GetHashCode()
			{
				return _section.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (obj is SubsectionNames)
				{
					return _section.Equals(((SubsectionNames)obj)._section);
				}

				return false;
			}
		}

		#endregion
	}
}