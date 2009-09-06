
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
using System.IO;
using System.Text;
using GitSharp.Exceptions;
using GitSharp.Util;
using Tamir.SharpSsh.java.lang;

namespace GitSharp
{

    public class Config
    {
        private static readonly string[] EMPTY_STRING_ARRAY = new string[0];
        private const long KiB = 1024;
        private const long MiB = 1024 * KiB;
        private const long GiB = 1024 * MiB;

        private State state;
        private readonly Config baseConfig;

        private static readonly string MAGIC_EMPTY_VALUE = string.Empty;

        public Config()
            : this(null)
        {

        }

        public Config(Config defaultConfig)
        {
            baseConfig = defaultConfig;
            state = newState();
        }

        private static string escapeValue(string x)
        {
            bool inquote = false;
            int lineStart = 0;
            StringBuilder r = new StringBuilder(x.Length);
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
                return (int) val;
            throw new ArgumentException("Integer value " + section + "." + name + " out of range");
        }

        public long getLong(string section, string subsection, string name, long defaultValue)
        {
            string str = getString(section, subsection, name);
            if (str == null)
                return defaultValue;

            string n = str.Trim();
            if (n.Length == 0)
                return defaultValue;

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
                n = n.Slice(0, n.Length - 1).Trim();
            if (n.Length == 0)
                return defaultValue;

            try
            {
                return mul*long.Parse(n);
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

            if (MAGIC_EMPTY_VALUE == n || StringUtils.equalsIgnoreCase("yes", n)
                || StringUtils.equalsIgnoreCase("true", n)
                || StringUtils.equalsIgnoreCase("1", n)
                || StringUtils.equalsIgnoreCase("on", n))
            {
                return true;
            }

            if (StringUtils.equalsIgnoreCase("no", n)
                || StringUtils.equalsIgnoreCase("false", n)
                || StringUtils.equalsIgnoreCase("0", n)
                || StringUtils.equalsIgnoreCase("off", n))
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
            string[] baseList = baseConfig != null ? baseConfig.getStringList(section, subsection, name) : EMPTY_STRING_ARRAY;

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

        public T get<T>(SectionParser<T> parser)
        {
            State myState = getState();
            T obj;
            if (!myState.Cache.ContainsKey(parser))
            {
                obj = parser.parse(this);
                myState.Cache.Add(parser, obj);
            }
            else
            {
                obj = (T) myState.Cache[parser];
            }
            return obj;
        }

        public void uncache<T>(SectionParser<T> parser)
        {
            state.Cache.Remove(parser);
        }

        private string getRawString(string section, string subsection, string name)
        {
            List<string> lst = getRawStringList(section, subsection, name);
            if (lst != null && lst.Count > 0)
                return lst[0];
            if (baseConfig != null)
                return baseConfig.getRawString(section, subsection, name);
            return null;
        }

        private List<string> getRawStringList(string section, string subsection, string name)
        {
            List<string> r = null;
            foreach (Entry e in state.EntryList)
            {
                if (e.match(section, subsection, name))
                    r = add(r, e.value);
            }
            return r;
        }

        private static List<string> add(List<string> curr, string value)
        {
            if (curr == null)
                return new List<string>{value};
            
            curr.Add(value);
            return curr;
        }

        private State getState()
        {
            State cur = state;
            State @base = getBaseState();
            if (cur.baseState == @base)
                return cur;
            State upd = new State(cur.EntryList, @base);
            state = upd;
            return upd;
        }

        private State getBaseState()
        {
            return baseConfig != null ? baseConfig.state : null;
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
            setStringList(section, subsection, name, new List<string> {value});
        }

        public void unset(string section, string subsection, string name)
        {
            setStringList(section, subsection, name, new List<string>());
        }

        public void setStringList(string section, string subsection, string name, List<string> values)
        {
            State src = state;
            State res = replaceStringList(src, section, subsection, name, values);
            state = res;
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
                if (e.match(section, subsection, name))
                {
                    entries[entryIndex] = e.forValue(values[valueIndex++]);
                    insertPosition = entryIndex + 1;
                }
                entryIndex++;
            }

            if (valueIndex == values.Count && entryIndex < entries.Count)
            {
                while (entryIndex < entries.Count)
                {
                    Entry e = entries[entryIndex++];
                    if (e.match(section, subsection, name))
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
                    Entry e = new Entry {section = section, subsection = subsection};
                    entries.Add(e);
                    insertPosition = entries.Count;
                }
                
                while (valueIndex < values.Count)
                {
                    Entry e = new Entry
                                  {
                                      section = section,
                                      subsection = subsection,
                                      name = name,
                                      value = values[valueIndex++]
                                  };
                    entries.Insert(insertPosition++, e);
                }
            }

            return newState(entries);
        }

        private static List<Entry> copy(State src, ICollection<string> values)
        {
            int max = src.EntryList.Count + values.Count + 1;
            List<Entry> r = new List<Entry>(max);
            r.AddRange(src.EntryList);
            return r;
        }

        private static int findSectionEnd(IList<Entry> entries, string section, string subsection)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.match(section, subsection, null))
                {
                    i++;
                    while (i < entries.Count)
                    {
                        e = entries[i];
                        if (e.match(section, subsection, e.name))
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
            StringBuilder o = new StringBuilder();
            foreach (Entry e in state.EntryList)
            {
                if (e.prefix != null)
                    o.Append(e.prefix);
                if (e.section != null && e.name == null)
                {
                    o.Append('[');
                    o.Append(e.section);
                    if (e.subsection != null)
                    {
                        o.Append(' ');
                        o.Append('"');
                        o.Append(escapeValue(e.subsection));
                        o.Append('"');
                    }
                    o.Append(']');
                }
                else if (e.section != null && e.name != null)
                {
                    if (e.prefix == null || "".Equals(e.prefix))
                        o.Append('\t');
                    o.Append(e.name);
                    if (e.value != null)
                    {
                        if (MAGIC_EMPTY_VALUE != e.value)
                        {
                            o.Append(" = ");
                            o.Append(escapeValue(e.value));
                        }
                    }
                    if (e.suffix != null)
                        o.Append(' ');
                }
                if (e.suffix != null)
                    o.Append(e.suffix);
                o.Append('\n');
            }
            return o.ToString();
        }

        protected void AddEntry(Entry e)
        {
            state.EntryList.Add(e);
        }

        public void fromText(string text)
        {
            List<Entry> newEntries = new List<Entry>();
            ConfigReader i = new ConfigReader(text);
            Entry last = null;
            Entry e = new Entry();

            for (;;)
            {
                int input = i.Read();
                if (-1 == input)
                    break;

                char c = (char) input;
                if ('\n' == c)
                {
                    newEntries.Add(e);
                    if (e.section != null)
                        last = e;
                    e = new Entry();
                }
                else if (e.suffix != null)
                {
                    e.suffix += c;
                }
                else if (';' == c || '#' == c)
                {
                    e.suffix = new string(c, 1);
                }
                else if (e.section == null && char.IsWhiteSpace(c))
                {
                    if (e.prefix == null)
                        e.prefix = "";
                    e.prefix += c;
                }
                else if ('[' == c)
                {
                    e.section = readSectionName(i);
                    input = i.Read();
                    if ('"' == input)
                    {
                        e.subsection = readValue(i, true, '"');
                        input = i.Read();
                    }
                    if (']' != input)
                        throw new ConfigInvalidException("Bad group header");
                    e.suffix = "";
                }
                else if (last != null)
                {
                    e.section = last.section;
                    e.subsection = last.subsection;
                    i.Reset();
                    e.name = readKeyName(i);
                    if (e.name.EndsWith("\n"))
                    {
                        e.name = e.name.Slice(0, e.name.Length - 1);
                        e.value = MAGIC_EMPTY_VALUE;
                    }
                    else
                    {
                        e.value = readValue(i, false, -1);
                    }
                }
                else
                {
                    throw new ConfigInvalidException("Invalid line in config file");
                }
            }

            state = newState(newEntries);
        }

        private static string readValue(ConfigReader i, bool quote, int eol)
        {
            StringBuilder value = new StringBuilder();
            bool space = false;
            for (;;)
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
                    if (char.IsWhiteSpace((char) c))
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
                            throw new ConfigInvalidException("Bad escape: " + ((char) c));
                    }
                }

                if ('"' == c)
                {
                    quote = !quote;
                    continue;
                }

                value.Append((char) c);
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
            state = newState();
        }
        
        private static string readSectionName(ConfigReader i)
        {
            StringBuilder name = new StringBuilder();
            for (;;)
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
                    for (;;)
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
                    name.Append((char) c);
                else
                    throw new ConfigInvalidException("Bad section entry: " + name);
            }
            return name.ToString();
        }

        private static string readKeyName(ConfigReader i)
        {
            StringBuffer name = new StringBuffer();
            for (;;)
            {
                int c = i.Read();
                if (c < 0)
                    throw new ConfigInvalidException("Unexpected end of config file");

                if ('=' == c)
                    break;

                if (' ' == c || '\t' == c)
                {
                    for (;;)
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

                if (char.IsLetterOrDigit((char) c) || c == '-')
                {
                    name.append((char) c);
                }
                else if ('\n' == c)
                {
                    i.Reset();
                    name.append((char) c);
                    break;
                }
                else
                    throw new ConfigInvalidException("Bad entry name: " + name);
            }

            return name.ToString();
        }

        public interface SectionParser<T>
        {
            T parse(Config cfg);
        }

        private class SubsectionNames : SectionParser<List<string>>
        {
            private readonly string section;

            public SubsectionNames(string sectionName)
            {
                section = sectionName;
            }

            public override int GetHashCode()
            {
                return section.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is SubsectionNames)
                    return section.Equals(((SubsectionNames)obj).section);
                return false;
            }

            public List<string> parse(Config cfg)
            {
                List<string> result = new List<string>();
                while (cfg != null)
                {
                    foreach (Entry e in cfg.state.EntryList)
                    {
                        if (e.subsection != null && e.name == null && StringUtils.equalsIgnoreCase(section, e.section))
                            result.Add(e.subsection);
                    }
                    cfg = cfg.baseConfig;
                }
                return result;
            }
        }

        private class State
        {
            public readonly List<Entry> EntryList;
            public readonly Dictionary<object, object> Cache;
            public readonly State baseState;

            public State(List<Entry> entries, State @base)
            {
                EntryList = entries;
                Cache = new Dictionary<object, object>();
                baseState = @base;
            }
        }

        protected class Entry
        {
            public string prefix;
            public string section;
            public string subsection;
            public string name;
            public string value;
            public string suffix;

            public Entry forValue(string newValue)
            {
                Entry e = new Entry
                              {
                                  prefix = prefix,
                                  section = section,
                                  subsection = subsection,
                                  name = name,
                                  value = newValue,
                                  suffix = suffix
                              };
                return e;
            }

            public bool match(string aSection, string aSubsection, string aKey)
            {
                return eqIgnoreCase(section, aSection)
                       && eqSameCase(subsection, aSubsection)
                       && eqIgnoreCase(name, aKey);
            }

            private static bool eqIgnoreCase(string a, string b)
            {
                if (a == null && b == null)
                    return true;
                if (a == null || b == null)
                    return false;
                return StringUtils.equalsIgnoreCase(a, b);
            }

            private static bool eqSameCase(string a, string b)
            {
                if (a == null && b == null)
                    return true;
                if (a == null || b == null)
                    return false;
                return a.Equals(b);
            }
        }

        private class ConfigReader
        {
            private readonly string data;
            private int position;
            private readonly int len;

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
    }
}