/*
 * Copyright (C) 2009, Stefan Schake <caytchen@gmail.com>
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
using GitSharp.Core.FnMatch;

namespace GitSharp.Core
{
    public interface IPattern
    {
        bool IsIgnored(string path);
    }

    public class IgnoreHandler
    {
        private readonly List<IPattern> _excludePatterns = new List<IPattern>();

        public IgnoreHandler(Repository repo)
        {
            try
            {
                ReadPatternsFromFile(Path.Combine(repo.Directory.FullName, "/info/exclude"), _excludePatterns);
                if (repo.Config.getCore().getExcludesFile() != null)
                {
                    ReadPatternsFromFile(repo.Config.getCore().getExcludesFile(), _excludePatterns);
                }
            }
            catch (Exception)
            {
                // optional
            }
        }

        private static void ReadPatternsFromFile(string path, ICollection<IPattern> to)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File not found", path);

            try
            {
                using (FileStream s = new FileStream(path, System.IO.FileMode.Open, FileAccess.Read))
                {
                    StreamReader reader = new StreamReader(s);
                    while (!reader.EndOfStream)
                        AddPattern(reader.ReadLine(), to);
                }
            }
            catch (IOException inner)
            {
                throw new InvalidOperationException("Can't read from " + path, inner);
            }
        }

        private static bool IsIgnored(string path, IEnumerable<IPattern> patterns)
        {
            return patterns.Any(p => p.IsIgnored(path));
        }

        public bool IsIgnored(string path)
        {
            //TODO: read path specific patterns

            if (IsIgnored(path, _excludePatterns))
                return true;

            return false;
        }

        private static void AddPattern(string line, ICollection<IPattern> to)
        {
            if (line.Length == 0)
                return;

            // Comment
            if (line.StartsWith("#"))
                return;

            // Negated
            if (line.StartsWith("!"))
            {
                line = line.Substring(1);
                to.Add(new NegatedPattern(new FnMatchPattern(line)));
                return;
            }

            to.Add(new FnMatchPattern(line));
        }

        private class FnMatchPattern : IPattern
        {
            private readonly FileNameMatcher _matcher;

            public FnMatchPattern(string line)
            {
                _matcher = new FileNameMatcher(line, null);
            }

            public bool IsIgnored(string path)
            {
                _matcher.Reset();
                _matcher.Append(path);
                return _matcher.IsMatch();
            }
        }

        private class NegatedPattern : IPattern
        {
            private readonly IPattern _original;

            public NegatedPattern(IPattern pattern)
            {
                _original = pattern;
            }

            public bool IsIgnored(string path)
            {
                return !(_original.IsIgnored(path));
            }
        }
    }
}