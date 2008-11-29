/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Thad Hughes <thadh@thad.corp.google.com>
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
using System.Diagnostics;
using Gitty.Util;

namespace Gitty.Lib
{
    [Complete]
    public class RepositoryConfig
    {
        public sealed class Constants
        {
            public static readonly string MagicEmptyValue = "%%magic%%empty%%";
            public static readonly string RemoteSection = "remote";
            public static readonly string BranchSection = "branch";
        }


        #region private variables
        private List<Entry> _entries;
        private Dictionary<string, object> _byName;
        private bool _readFile;

        #endregion


        #region constructors
        public RepositoryConfig(Repository repo)
            : this(OpenUserConfig(), new FileInfo(Path.Combine(repo.Directory.FullName, "config")))
        {
         
        }

        public RepositoryConfig(RepositoryConfig baseConfig, FileInfo configFile)
        {
            this.BaseConfig = baseConfig;
            this.ConfigFile = configFile;
            this.Clear();
        }
        #endregion

        #region properties
        public CoreConfig Core { get; private set; }
        public RepositoryConfig BaseConfig { get; private set; }
        public Repository Repository { get; private set; }
        public FileInfo ConfigFile { get; private set; }

        #endregion


        #region static methods

        public static RepositoryConfig OpenUserConfig()
        {
	    string bd;

	    int p = (int) Environment.OSVersion.Platform;
	    if (p == (int) PlatformID.Unix || p == (int) PlatformID.MacOSX || p == 128)
		bd = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
	    else
		bd = Environment.GetEnvironmentVariable( "USERPROFILE");
	    
            return new RepositoryConfig(null, new FileInfo(Path.Combine(bd, ".gitconfig")));
        }



        private static String ReadName(BufferedReader r)
        {
            StringBuilder name = new StringBuilder();

            while (true)
            {
                using (r.GetMarker())
                {
                    int c = r.Read();
                    if (c < 0)
                    {
                        throw new IOException("Unexpected end of config file.");
                    }
                    else if ('=' == c)
                    {
                        break;
                    }
                    else if (' ' == c || '\t' == c)
                    {
                        while (true)
                        {
                            using (r.GetMarker())
                            {
                                c = r.Read();
                                if (c < 0)
                                {
                                    throw new IOException("Unexpected end of config file.");
                                }
                                else if ('=' == c)
                                {
                                    break;
                                }
                                else if (';' == c || '#' == c || '\n' == c)
                                {
                                    r.Reset();
                                    break;
                                }
                                else if (' ' == c || '\t' == c)
                                {
                                    // Skipped...
                                }
                                else
                                {
                                    throw new IOException("Bad entry delimiter.");
                                }
                            }
                        }
                        break;
                    }
                    else if (Char.IsLetterOrDigit((char)c) || c == '-')
                    {
                        // From the git-config man page:
                        //     The variable names are case-insensitive and only
                        //     alphanumeric characters and - are allowed.
                        name.Append((char)c);
                    }
                    else if ('\n' == c)
                    {
                        r.Reset();
                        name.Append((char)c);
                        break;
                    }
                    else
                    {
                        throw new IOException("Bad config entry name: " + name + (char)c);
                    }
                }
            }
            return name.ToString();
        }

        private string ReadValue(BufferedReader r, bool quote, int eol)
        {
            StringBuilder value = new StringBuilder();
            bool space = false;

            while (true)
            {
                using (r.GetMarker())
                {
                    int c = r.Read();
                    if (c < 0)
                    {
                        if (value.Length == 0)
                            throw new IOException("Unexpected end of config file.");
                        break;
                    }
                    if ('\n' == c)
                    {
                        if (quote)
                        {
                            throw new IOException("Newline in quotes not allowed.");
                        }
                        r.Reset();
                        break;
                    }
                    if (eol == c)
                    {
                        break;
                    }
                    if (!quote)
                    {
                        if (Char.IsWhiteSpace((char)c))
                        {
                            space = true;
                            continue;
                        }
                        if (';' == c || '#' == c)
                        {
                            r.Reset();
                            break;
                        }
                    }
                    if (space)
                    {
                        if (value.Length > 0)
                        {
                            value.Append(' ');
                        }
                        space = false;
                    }
                    if ('\\' == c)
                    {
                        c = r.Read();
                        switch (c)
                        {
                            case -1:
                                throw new IOException("End of file in escape.");
                            case '\n':
                                continue;
                            case 't':
                                r.Read();
                                value.Append('\t');
                                continue;
                            case 'b':
                                r.Read();
                                value.Append('\b');
                                continue;
                            case 'n':
                                r.Read();
                                value.Append('\n');
                                continue;
                            case '\\':
                                r.Read();
                                value.Append('\\');
                                continue;
                            case '"':
                                r.Read();
                                value.Append('"');
                                continue;
                            default:
                                throw new IOException("Bad escape: " + ((char)c));
                        }
                    }
                    if ('"' == c)
                    {
                        quote = !quote;
                        continue;
                    }
                    value.Append((char) c);
                }
            }
            return value.Length > 0 ? value.ToString() : null;
        }

        private static string ReadBase(BufferedReader r)
        {
            StringBuilder Base = new StringBuilder();
            while (true)
            {
                using (r.GetMarker())
                {
                    int c = r.Read();
                    if (c < 0)
                    {
                        throw new IOException("Unexpected end of config file.");
                    }
                    else if (']' == c)
                    {
                        r.Reset();
                        break;
                    }
                    else if (' ' == c || '\t' == c)
                    {
                        while (true)
                        {
                            r.Mark();
                            c = r.Read();
                            if (c < 0)
                            {
                                throw new IOException("Unexpected end of config file.");
                            }
                            else if ('"' == c)
                            {
                                r.Reset();
                                break;
                            }
                            else if (' ' == c || '\t' == c)
                            {
                                // Skipped...
                            }
                            else
                            {
                                throw new IOException("Bad base entry. : " + Base + "," + c);
                            }
                        }
                        break;
                    }
                    else if (Char.IsLetterOrDigit((char)c) || '.' == c || '-' == c)
                    {
                        Base.Append((char)c);
                    }
                    else
                    {
                        throw new IOException("Bad base entry. : " + Base + ", " + c);
                    }
                }
            }
            return Base.ToString();
        }

        private static String EscapeValue(string x)
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
            if (inquote)
            {
                r.Append('"');
            }
            return r.ToString();
        }
        #endregion
        #region public methods
        private void Clear()
        {
            _entries = new List<Entry>();
            _byName = new Dictionary<string, object>();
        }

        public void Load()
        {
            Clear();
            _readFile = true;

            using (BufferedReader r = new BufferedReader(ConfigFile.FullName))
            {

                Entry last = null;
                Entry e = new Entry();
                while (true)
                {
                    r.Mark();

                    int input = r.Read();
                    char inc = (char)input;
                    if (-1 == input)
                    {
                        break;
                    }
                    else if ('\n' == inc)
                    {
                        // End of this entry.
                        Add(e);
                        if (e.Base != null)
                            last = e;

                        e = new Entry();
                    }
                    else if (e.Suffix != null)
                    {
                        // Everything up until the end-of-line is in the suffix.
                        e.Suffix += inc;
                    }
                    else if (';' == inc || '#' == inc)
                    {
                        // The rest of this line is a comment; put into suffix.
                        e.Suffix = inc.ToString();
                    }
                    else if (e.Base == null && Char.IsWhiteSpace(inc))
                    {
                        // Save the leading whitespace (if any).
                        if (e.Prefix == null)
                        {
                            e.Prefix = "";
                        }
                        e.Prefix += inc;
                    }
                    else if ('[' == inc)
                    {
                        // This is a group header line.
                        e.Base = ReadBase(r);
                        input = r.Read();
                        if ('"' == input)
                        {
                            e.ExtendedBase = ReadValue(r, true, '"');
                            input = r.Read();
                        }
                        if (']' != input)
                        {
                            throw new IOException("Bad group header.");
                        }
                        e.Suffix = "";
                    }
                    else if (last != null)
                    {
                        // Read a value.
                        e.Base = last.Base;
                        e.ExtendedBase = last.ExtendedBase;

                        r.Reset();

                        e.Name = ReadName(r);
                        if (e.Name.EndsWith("\n"))
                        {
                            e.Name = e.Name.Substring(0, e.Name.Length - 1);
                            e.Value = Constants.MagicEmptyValue;
                        }
                        else
                            e.Value = ReadValue(r, false, -1);

                        continue;
                    }
                    else
                    {
                        throw new IOException("Invalid line in config file.");
                    }
                }
            }
            this.Core = new CoreConfig(this);
        }

        public override string ToString()
        {
            return "RepositoryConfig[" + ConfigFile.Directory.FullName + "]";
        }

        public void Create()
        {
            Entry e;

            Clear();
            _readFile = true;

            e = new Entry();
            e.Base = "core";
            Add(e);

            e = new Entry();
            e.Base = "core";
            e.Name = "repositoryformatversion";
            e.Value = "0";
            Add(e);

            e = new Entry();
            e.Base = "core";
            e.Name = "filemode";
            e.Value = "true";
            Add(e);

            this.Core = new CoreConfig(this);
        }

        private void Add(Entry e)
        {
            _entries.Add(e);
            if (e.Base != null)
            {

                String group = e.Base.ToLower();
                if (e.ExtendedBase != null)
                {
                    group += "." + e.ExtendedBase;
                }

                if (e.Name != null)
                {
                    String n = e.Name.ToLower();
                    String key = group + "." + n;
                    Object o = Collections.GetValue(_byName, key);
                    if (o == null)
                    {
                        Collections.AddOrInsert(_byName, key, e);
                    }
                    else if (o is Entry)
                    {
                        List<Entry> l = new List<Entry>();
                        l.Add((Entry)o);
                        l.Add(e);
                        Collections.AddOrInsert(_byName, key, l);
                    }
                    else if (o is List<Entry>)
                    {
                        ((List<Entry>)o).Add(e);
                    }
                }
            }
        }

        public void Save()
        {
            FileInfo tmp = new FileInfo(Path.Combine(ConfigFile.Directory.FullName, ConfigFile.Name + ".lock"));
            bool ok = false;

            using (StreamWriter r = new StreamWriter(tmp.FullName))
            {
                List<Entry>.Enumerator i = _entries.GetEnumerator();

                while (i.MoveNext())
                {
                    Entry e = i.Current;
                    if (e.Prefix != null)
                    {
                        r.Write(e.Prefix);
                    }

                    if (e.Base != null && e.Name == null)
                    {
                        r.Write('[');
                        r.Write(e.Base);
                        if (e.ExtendedBase != null)
                        {
                            r.Write(' ');
                            r.Write('"');
                            r.Write(EscapeValue(e.ExtendedBase));
                            r.Write('"');
                        }
                        r.Write(']');
                    }
                    else if (e.Base != null && e.Name != null)
                    {
                        if (e.Prefix == null || "".Equals(e.Prefix))
                        {
                            r.Write('\t');
                        }
                        r.Write(e.Name);
                        if (e.Value != null)
                        {
                            if (!Constants.MagicEmptyValue.Equals(e.Value))
                            {
                                r.Write(" = ");
                                r.Write(EscapeValue(e.Value));
                            }
                        }
                        if (e.Suffix != null)
                        {
                            r.Write(' ');
                        }
                    }
                    if (e.Suffix != null)
                    {
                        r.Write(e.Suffix);
                    }
                    r.Write('\n');
                }
                ok = true;
                r.Close();
                try
                {
                    tmp.MoveTo(ConfigFile.FullName);
                }
                catch (Exception)
                {
                    try
                    {
                        ConfigFile.Delete();
                        tmp.MoveTo(ConfigFile.FullName);
                    }
                    catch (Exception)
                    {
                        throw new IOException("Cannot save config file " + ConfigFile + ", rename failed");
                    }
                }
            }
            if (tmp.Exists)
            {
                try
                {
                    tmp.Delete();
                }
                catch (Exception)
                {
                    #warning should log this "(warning) failed to delete tmp config file: " + tmp)
                }
            }

            _readFile = ok;
        }

        #endregion
        #region public variable accessors
        public int GetInt(string section, string name, int defaultValue)
        {
            return GetInt(section, null, name, defaultValue);
        }

        public int GetInt(string section, string subsection, string name, int defaultValue)
        {
            string str = GetString(section, subsection, name);
            if (str == null)
                return defaultValue;

            string n = str.Trim();
            if (n.Length == 0)
                return defaultValue;

            int mul = 1;
            switch (n[n.Length - 1].ToString().ToLower())
            {
                case "g":
                    mul = 1024 * 1024 * 1024;
                    break;
                case "m":
                    mul = 1024 * 1024;
                    break;
                case "k":
                    mul = 1024;
                    break;
                default:
                    break;
            }

            if (mul > 1)
                n = n.Substring(0, n.Length - 1).Trim();

            if (n.Length == 0)
                return defaultValue;

            try
            {
                return mul * int.Parse(n);
            }
            catch (FormatException)
            {
                throw new ArgumentException(string.Format("Invalid integer value: {0}.{1} = {2}", section, name, str));
            }
        }

        public bool GetBoolean(string section, string subsection, string name, bool defaultValue)
        {
            string n = GetRawString(section, subsection, name);

            if (n == null)
                return defaultValue;

            n = n.ToLower();
            if (Constants.MagicEmptyValue.Equals(n) || "yes".Equals(n) || "true".Equals(n) || "1".Equals(n))
                return true;
            else if ("no".Equals(n) || "false".Equals(n) || "0".Equals(n))
                return false;
            else
                throw new ArgumentException(string.Format("Invalid boolean value: {0}.{1} = {2}", section, name, n));

        }

        public bool GetBoolean(string section, string name, bool defaultValue)
        {
            return GetBoolean(section, null, name, defaultValue);
        }

        public string GetString(string section, string subsection, string name)
        {
            string val = GetRawString(section, subsection, name);
            if (Constants.MagicEmptyValue.Equals(val))
            {
                return "";
            }

            return val;
        }

        public string[] GetStringList(string section, string subsection, string name)
        {
            Object o = GetRawEntry(section, subsection, name);
            if (o is List<Entry>)
            {
                List<Entry> lst = (List<Entry>)o;
                String[] r = new String[lst.Count];
                for (int i = 0; i < r.Length; i++)
                {
                    string val = ((Entry)lst[i]).Value;
                    r[i] = Constants.MagicEmptyValue.Equals(val) ? "" : val;
                }
                return r;
            }
            else if (o is Entry)
            {
                string val = ((Entry)o).Value;
                return new String[] { Constants.MagicEmptyValue.Equals(val) ? "" : val };
            }

            if (this.BaseConfig != null)
                return this.BaseConfig.GetStringList(section, subsection, name);

            return new String[0];
        }


        public void SetString(string section, string subsection, string name, string value) 
        {
            List<string> list = new List<string>();
            list.Add(value);
		    SetStringList(section, subsection, name, list);
        }

        public void UnsetString(string section, string subsection, string name) 
        {
		    SetStringList(section, subsection, name, new List<string>());
        }

        public void SetStringList(string section, string subsection, string name, List<string> values)
        {
            // Update our parsed cache of values for future reference.
            //
            String key = section.ToLower();
            if (subsection != null)
                key += "." + subsection.ToLower();

            key += "." + name.ToLower();

            if (values.Count == 0)
                _byName.Remove(key);
            else if (values.Count == 1)
                Collections.AddOrInsert(_byName, key, values[0]);
            else
                Collections.AddOrInsert(_byName, key, new List<String>(values));

            int entryIndex = 0;
            int valueIndex = 0;
            int insertPosition = -1;

            // Reset the first n Entry objects that match this input name.
            //
            while (entryIndex < _entries.Count && valueIndex < values.Count)
            {
                Entry e = _entries[entryIndex++];
                if (e.Match(section, subsection, name))
                {
                    e.Value = values[valueIndex++];
                    insertPosition = entryIndex;
                }
            }

            // Remove any extra Entry objects that we no longer need.
            //
            if (valueIndex == values.Count && entryIndex < _entries.Count)
            {
                while (entryIndex < _entries.Count)
                {
                    Entry e = _entries[entryIndex++];
                    if (e.Match(section, subsection, name))
                        _entries.RemoveAt(--entryIndex);
                }
            }

            // Insert new Entry objects for additional/new values.
            //
            if (valueIndex < values.Count && entryIndex == _entries.Count)
            {
                if (insertPosition < 0)
                {
                    // We didn't find a matching key above, but maybe there
                    // is already a section available that matches.  Insert
                    // after the last key of that section.
                    //
                    insertPosition = findSectionEnd(section, subsection);
                }
                if (insertPosition < 0)
                {
                    // We didn't find any matching section header for this key,
                    // so we must create a new section header at the end.
                    //
                    Entry e = new Entry();
                    e.Prefix = null;
                    e.Suffix = null;
                    e.Base = section;
                    e.ExtendedBase = subsection;
                    _entries.Add(e);
                    insertPosition = _entries.Count;
                }
                while (valueIndex < values.Count)
                {
                    Entry e = new Entry();
                    e.Prefix = null;
                    e.Suffix = null;
                    e.Base = section;
                    e.ExtendedBase = subsection;
                    e.Name = name;
                    e.Value = values[valueIndex++];
                    _entries.Insert(insertPosition++, e);
                }
            }
        }

        #endregion

        #region helper methods

        private int findSectionEnd(String section, String subsection)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.Match(section, subsection, null))
                {
                    i++;
                    while (i < _entries.Count)
                    {
                        e = _entries[i];
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

        private object GetRawEntry(string section, string subsection, string name)
        {
            if (!_readFile)
            {
                try
                {
                    Load();
                }
                catch (FileNotFoundException)
                {
                    // Oh well. No sense in complaining about it.
                    //
                }
                catch (Exception)
                {
                    Debugger.Break();
                }
            }

            String ss;
            if (subsection != null)
                ss = "." + subsection.ToLower();
            else
                ss = "";
            try
            {
                return _byName[section.ToLower() + ss + "." + name.ToLower()];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            
        }

        private string GetRawString(string section, string subsection, string name)
        {
            object o = GetRawEntry(section, subsection, name);
            if (o is List<Entry>)
            {
                return ((List<Entry>)o)[0].Value;
            }
            else if (o is Entry)
            {
                return ((Entry)o).Value;
            }
            else if (this.BaseConfig != null)
                return this.BaseConfig.GetRawString(section, subsection, name);
            else
                return null;
        }

        #endregion


        class Entry {
            public string Prefix { get; set; }
            public string Base { get; set; }
            public string ExtendedBase { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public string Suffix { get; set; }

		    public bool Match(String aBase, String aExtendedBase, String aName) {
			    return eq(this.Base, aBase) 
                    && eq(this.ExtendedBase, aExtendedBase)
					&& eq(this.Name, aName);
		    }

		    private static bool eq(String a, String b) {
			    if (a == b)
				    return true;
			    if (a == null || b == null)
				    return false;
			    return a.Equals(b);
		    }
	    }


    }
}
