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

using System;
using System.Text;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /**
 * Describes how refs in one repository copy into another repository.
 * <para />
 * A ref specification provides matching support and limited rules to rewrite a
 * reference in one repository to another reference in another repository.
 */
    public class RefSpec
    {
        public const string WILDCARD_SUFFIX = "/*";

        public static bool IsWildcard(string s)
        {
            return s != null && s.EndsWith(WILDCARD_SUFFIX);
        }

        public bool Force { get; private set; }
        public bool Wildcard { get; private set; }
        public string Source { get; private set; }
        public string Destination { get; private set; }

        public RefSpec()
        {
            Force = false;
            Wildcard = false;
            Source = Constants.HEAD;
            Destination = null;
        }

        public RefSpec(string source, string destination)
            : this()
        {
            Source = source;
            Destination = destination;
        }

        public RefSpec(string spec)
        {
            string s = spec;
            if (s.StartsWith("+"))
            {
                Force = true;
                s = s.Substring(1);
            }

            int c = s.LastIndexOf(':');
            if (c == 0)
            {
                s = s.Substring(1);
				if (IsWildcard(s))
				{
					throw new ArgumentException("Invalid Wildcards " + spec);
				}
            	Destination = s;
            }
            else if (c > 0)
            {
                Source = s.Slice(0, c);
                Destination = s.Substring(c + 1);
				if (IsWildcard(Source) && IsWildcard(Destination))
				{
					Wildcard = true;
				}
				else if (IsWildcard(Source) || IsWildcard(Destination))
				{
					throw new ArgumentException("Invalid Wildcards " + spec);
				}
            }
            else
            {
				if (IsWildcard(s))
				{
					throw new ArgumentException("Invalid Wildcards " + spec);
				}
            	Source = s;
            }
        }

        private RefSpec(RefSpec p)
        {
            Force = p.Force;
            Wildcard = p.Wildcard;
            Source = p.Source;
            Destination = p.Destination;
        }

        public RefSpec SetForce(bool force)
        {
            return new RefSpec(this) { Force = force };
        }

        public RefSpec SetSource(string source)
        {
            return new RefSpec(this) { Source = source };
        }

        public RefSpec SetDestination(string destination)
        {
            RefSpec r = new RefSpec(this);
            r.Destination = destination;

            if (IsWildcard(r.Destination) && r.Source == null)
            {
                throw new ArgumentException("Source is not a wildcard.");
            }
            if (IsWildcard(r.Source) != IsWildcard(r.Destination))
            {
                throw new ArgumentException("Source/Destination must match.");
            }
            return r;
        }

        public RefSpec SetSourceDestination(string source, string destination)
        {
            if (IsWildcard(source) != IsWildcard(destination))
            {
                throw new ArgumentException("Source/Destination must match.");
            }

            return new RefSpec(this) { Wildcard = IsWildcard(source), Source = source, Destination = destination };
        }

        public bool MatchSource(string r)
        {
            return match(r, Source);
        }

        public bool MatchSource(Ref r)
        {
            return match(r.Name, Source);
        }

        public bool MatchDestination(string r)
        {
            return match(r, Destination);
        }

        public bool MatchDestination(Ref r)
        {
            return match(r.Name, Destination);
        }

        public RefSpec ExpandFromSource(string r)
        {
            return Wildcard ? new RefSpec(this).expandFromSourceImp(r) : this;
        }

        private RefSpec expandFromSourceImp(string name)
        {
            string psrc = Source, pdst = Destination;
            Wildcard = false;
            Source = name;
            Destination = pdst.Slice(0, pdst.Length - 1) + name.Substring(psrc.Length - 1);
            return this;
        }

        public RefSpec ExpandFromSource(Ref r)
        {
            return ExpandFromSource(r.Name);
        }

        public RefSpec ExpandFromDestination(string r)
        {
            return Wildcard ? new RefSpec(this).expandFromDstImp(r) : this;
        }

        public RefSpec ExpandFromDestination(Ref r)
        {
            return ExpandFromDestination(r.Name);
        }

        private RefSpec expandFromDstImp(string name)
        {
            string psrc = Source, pdst = Destination;
            Wildcard = false;
            Source = psrc.Slice(0, psrc.Length - 1) + name.Substring(pdst.Length - 1);
            Destination = name;
            return this;
        }

        private bool match(string refName, string s)
        {
			if (string.IsNullOrEmpty(s))
			{
				return false;
			}

            if (Wildcard)
            {
                return refName.StartsWith(s.Slice(0, s.Length - 1));
            }

            return refName.Equals(s);
        }

        private static bool eq(string a, string b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RefSpec))
                return false;

            RefSpec b = (RefSpec)obj;
            if (Force != b.Force) return false;
            if (Wildcard != b.Wildcard) return false;
            if (!eq(Source, b.Source)) return false;
            if (!eq(Destination, b.Destination)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hc = 0;
            if (Source != null)
                hc = hc * 31 + Source.GetHashCode();
            if (Destination != null)
                hc = hc * 31 + Destination.GetHashCode();
            return hc;
        }

        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            if (Force)
                r.Append('+');
            if (Source != null)
                r.Append(Source);
            if (Destination != null)
            {
                r.Append(':');
                r.Append(Destination);
            }
            return r.ToString();
        }
    }
}