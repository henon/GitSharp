using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    [Complete]
    public class PersonIdent
    {

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

        public PersonIdent(PersonIdent pi, long when, TimeSpan tz)
        {
            this.Name = pi.Name;
            this.EmailAddress = pi.EmailAddress;
            this._whenTicks = when;
            this._tzOffset = tz;
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
                String tzHoursStr = str.Substring(sp + 1, sp + 4).Trim();
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
                _whenTicks = long.Parse(str.Substring(gt + 1, sp).Trim()) * 1000;
                _tzOffset = TimeSpan.FromMinutes(tzHours * 60 + tzMins);
            }

            this.Name = str.Substring(0, lt).Trim ();
            this.EmailAddress = str.Substring(lt + 1, gt).Trim ();
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
            StringBuilder r = new StringBuilder();

            r.Append("PersonIdent[");
            r.Append(Name);
            r.Append(", ");
            r.Append(EmailAddress);
            r.Append(", ");
            r.Append(new DateTimeOffset(_whenTicks, _tzOffset));
            r.Append("]");

            return r.ToString();
        }
    }
}
