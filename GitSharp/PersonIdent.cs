/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using GitSharp.Util;

namespace GitSharp
{
    public class PersonIdent
    {
        public static readonly long EPOCH_TICKS = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

        public string Name { get; private set; }
        public string EmailAddress { get; private set; }

        public long _whenTicks;
        public DateTimeOffset When {
            get { return new DateTimeOffset(_whenTicks, _tzOffset); }
        }
        /// <summary>
        /// TimeZone offset in minutes
        /// </summary>
        private TimeSpan _tzOffset;
        public TimeZoneInfo TimeZone
        {
            get
            {
                foreach (TimeZoneInfo tzi in System.TimeZoneInfo.GetSystemTimeZones())
                {
                    if (tzi.BaseUtcOffset == _tzOffset)
                        return tzi;
                }
                return null;
            }
        }

        public PersonIdent(Repository repo)
        {
            RepositoryConfig config = repo.Config;
            String username = config.GetString("user", null, "name");
            String email = config.GetString("user", null, "email");
             
            this.Name = username;
            this.EmailAddress = email;
            DateTimeOffset n = DateTimeOffset.Now;
            this._whenTicks = n.Ticks;
            this._tzOffset = n.Offset;            
        }

        public PersonIdent(PersonIdent pi)
            : this(pi.Name, pi.EmailAddress)
        {
            
        }

        public PersonIdent(string name, string emailAddress)
            : this(name, emailAddress, DateTime.Now, TimeZoneInfo.Local)
        {

        }

        public PersonIdent(PersonIdent pi, DateTime when, TimeZoneInfo tz)
            : this(pi.Name, pi.EmailAddress, when, tz)
        {

        }

        public PersonIdent(PersonIdent pi, DateTime when)
        {
            this.Name = pi.Name;
            this.EmailAddress = pi.EmailAddress;
            this._whenTicks = when.Ticks;
            this._tzOffset = pi._tzOffset;

        }

        public PersonIdent(string name, string emailAddress, DateTime when, TimeZoneInfo tz)
        {
            this.Name = name;
            this.EmailAddress = emailAddress;
            this._whenTicks = when.Ticks;
            this._tzOffset = tz.GetUtcOffset(when);
        }

        public PersonIdent(string name, string emailAddress, long when, TimeSpan tz)
        {
            this.Name = name;
            this.EmailAddress = emailAddress;
            this._whenTicks = when;
            this._tzOffset = tz;
        }

        public PersonIdent(string name, string emailAddress, long when, int offset_in_minutes)
        {
            this.Name = name;
            this.EmailAddress = emailAddress;
            this._whenTicks = when;
            this._tzOffset = new TimeSpan(0, offset_in_minutes, 0);
        }

        public PersonIdent(PersonIdent pi, long when, TimeSpan tz)
        {
            this.Name = pi.Name;
            this.EmailAddress = pi.EmailAddress;
            this._whenTicks = when;
            this._tzOffset = tz;
        }

        public PersonIdent(PersonIdent pi, long when, int offset_in_minutes)
        {
            this.Name = pi.Name;
            this.EmailAddress = pi.EmailAddress;
            this._whenTicks = when;
            this._tzOffset = new TimeSpan(0, offset_in_minutes, 0); ;
        }

        public PersonIdent(string str)
        {
            int lt = str.IndexOf('<');
            if (lt == -1)
            {
                throw new ArgumentException("Malformed PersonIdent string"
                        + " (no < was found): " + str);
            }
            int gt = str.IndexOf('>', lt);
            if (gt == -1)
            {
                throw new ArgumentException("Malformed PersonIdent string"
                        + " (no > was found): " + str);
            }
            int sp = str.IndexOf(' ', gt + 2);
            if (sp == -1)
            {
                _whenTicks = 0;
                _tzOffset = TimeSpan.Zero;
            }
            else
            {
                String tzHoursStr = str.Slice(sp + 1, sp + 4).Trim();
                int tzHours;
                if (tzHoursStr[0] == '+')
                {
                    tzHours = int.Parse(tzHoursStr.Substring(1));
                }
                else
                {
                    tzHours = int.Parse(tzHoursStr);
                }
                int tzMins = int.Parse(str.Substring(sp + 4).Trim());
                var s = str.Slice(gt + 1, sp);
                _whenTicks = EPOCH_TICKS +  long.Parse(str.Slice(gt + 1, sp).Trim()) * 10000000;
                _tzOffset = TimeSpan.FromMinutes(tzHours * 60 + tzMins);
                _whenTicks += _tzOffset.Ticks;
            }

            this.Name = str.Slice(0, lt).Trim ();
            this.EmailAddress = str.Slice(lt + 1, gt).Trim ();
        }

        public override int GetHashCode()
        {
            return this.EmailAddress.GetHashCode() ^ (int)_whenTicks;
        }

        public override bool Equals(object o)
        {
            PersonIdent p = o as PersonIdent;
            if (p == null)
                return false;

            return this.Name == p.Name 
                && this.EmailAddress == p.EmailAddress 
                && _whenTicks == p._whenTicks;
        }

        public string ToExternalString()
        {
            StringBuilder r = new StringBuilder();
            int offset = (int)(_tzOffset.Ticks / TimeSpan.TicksPerMinute);
            char sign;
            int offsetHours;
            int offsetMins;

            if (offset < 0)
            {
                sign = '-';
                offset = -offset;
            }
            else
            {
                sign = '+';
            }

            offsetHours = offset / 60;
            offsetMins = offset % 60;

            r.Append(Name);
            r.Append(" <");
            r.Append(EmailAddress);
            r.Append("> ");
            r.Append(_whenTicks / 1000);
            r.Append(' ');
            r.Append(sign);
            if (offsetHours < 10)
            {
                r.Append('0');
            }
            r.Append(offsetHours);
            if (offsetMins < 10)
            {
                r.Append('0');
            }
            r.Append(offsetMins);
            return r.ToString();
        }

        public override string ToString()
        {
            return Name + "<" + EmailAddress + "> " + When;
        }
    }
}
