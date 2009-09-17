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
using System.Globalization;
using System.Text;
using GitSharp.Util;

namespace GitSharp
{
	/// <summary>
	/// A combination of a person identity and time in Git.
	/// <para />
	/// Git combines Name + email + time + time zone to specify who wrote or
	/// committed something.
	/// </summary>
	public class PersonIdent
	{
		private readonly DateTimeOffset _when;
		private readonly TimeSpan _tzOffset;
		private readonly TimeZone _timeZone;

		///	<summary>
		/// Creates new PersonIdent from config info in repository, with current time.
		///	This new PersonIdent gets the info from the default committer as available
		///	from the configuration.
		///	</summary>
		///	<param name="repo"></param>
		public PersonIdent(Repository repo)
		{
			RepositoryConfig config = repo.Config;
			Name = config.getCommitterName();
			EmailAddress = config.getCommitterEmail();
			_when = DateTimeOffset.Now;
			_timeZone = System.TimeZone.CurrentTimeZone;
			_tzOffset = _when.Offset;
		}

		///	<summary>
		/// Copy a <seealso cref="PersonIdent"/>.
		///	</summary>
		///	<param name="pi">Original <seealso cref="PersonIdent"/>.</param>
		public PersonIdent(PersonIdent pi)
			: this(pi.Name, pi.EmailAddress)
		{
		}

		///	<summary>
		/// Construct a new <seealso cref="PersonIdent"/> with current time.
		///	</summary>
		///	<param name="name"> </param>
		///	<param name="emailAddress"></param>
		public PersonIdent(string name, string emailAddress)
			: this(name, emailAddress, DateTimeOffset.Now, System.TimeZone.CurrentTimeZone)
		{
		}

		///	<summary>
		/// Copy a PersonIdent, but alter the clone's time stamp
		///	</summary>
		///	<param name="pi">Original <seealso cref="PersonIdent"/>.</param>
		///	<param name="when">Local time.</param>
		///	<param name="tz">Time zone.</param>
		public PersonIdent(PersonIdent pi, DateTime when, TimeZone tz)
			: this(pi.Name, pi.EmailAddress, when, tz)
		{
		}

		/// <summary>
		/// Copy a <seealso cref="PersonIdent"/>, but alter the clone's time stamp
		/// </summary>
		/// <param name="pi">Original <seealso cref="PersonIdent"/>.</param>
		///	<param name="when">Local time stamp.</param>
		public PersonIdent(PersonIdent pi, DateTimeOffset when)
		{
			Name = pi.Name;
			EmailAddress = pi.EmailAddress;
			_when = when;
		}

		///	<summary>
		/// Construct a PersonIdent from simple data
		///	</summary>
		///	<param name="name"></param>
		///	<param name="emailAddress"></param>
		///	<param name="when">Local time stamp.</param>
		///	<param name="tz">Time zone.</param>
		public PersonIdent(string name, string emailAddress, DateTimeOffset when, TimeZone tz)
		{
			Name = name;
			EmailAddress = emailAddress;
			_when = when;
			_tzOffset = _when.Offset;
		}

		///	<summary>
		/// Construct a <seealso cref="PersonIdent"/>
		///	</summary>
		///	<param name="name"></param>
		///	<param name="emailAddress"> </param>
		///	<param name="when">Local time stamp.</param>
		///	<param name="tz">Time zone.</param>
		public PersonIdent(string name, string emailAddress, long when, int tz)
		{
			Name = name;
			EmailAddress = emailAddress;
			_when = (when * 1000).GitTimeToDateTimeOffset(tz);
			_tzOffset = TimeSpan.FromMinutes(tz);
		}

		///	<summary>
		/// Copy a PersonIdent, but alter the clone's time stamp
		///	</summary>
		///	<param name="pi">Original <seealso cref="PersonIdent"/>.</param>
		///	<param name="when">Local time stamp.</param>
		///	<param name="tz">Time zone.</param>
		public PersonIdent(PersonIdent pi, long when, int tz)
		{
			Name = pi.Name;
			EmailAddress = pi.EmailAddress;
			_when = when.GitTimeToDateTimeOffset(tz);
			_tzOffset = TimeSpan.FromMinutes(tz);
		}

		///	<summary>
		/// Construct a PersonIdent from a string with full name, email, time time
		///	zone string. The input string must be valid.
		///	</summary>
		///	<param name="str">A Git internal format author/committer string.</param>
		public PersonIdent(string str)
		{
			int lt = str.IndexOf('<');
			if (lt == -1)
			{
				throw new ArgumentException("Malformed PersonIdent string (no < was found): " + str);
			}

			int gt = str.IndexOf('>', lt);
			if (gt == -1)
			{
				throw new ArgumentException("Malformed PersonIdent string (no > was found): " + str);
			}

			int sp = str.IndexOf(' ', gt + 2);
			if (sp == -1)
			{
				_when = new DateTimeOffset(0, TimeSpan.Zero);
			}
			else
			{
				string tzHoursStr = str.Substring(sp + 1, sp + 4 - sp + 1).Trim();

				int tzHours = tzHoursStr[0] == '+' ? Convert.ToInt32(tzHoursStr.Substring(1, 2)) : Convert.ToInt32(tzHoursStr) / 100;
				int tzMins = Convert.ToInt32(str.Substring(sp + 4).Trim());
				var offsetInMinutes = tzHours * 60 + tzMins;
				_tzOffset = TimeSpan.FromMinutes(offsetInMinutes);

				string ticksString = str.Substring(gt + 1, sp - gt).Trim();
				long ticks = Convert.ToInt64(ticksString) * 1000;
				_when = ticks.GitTimeToDateTimeOffset(offsetInMinutes);
			}

			Name = str.Slice(0, lt).Trim();
			EmailAddress = str.Slice(lt + 1, gt).Trim();
		}

		///	<summary>
		/// Gets the name of person.
		/// </summary>
		public string Name { get; private set; }

		///	<summary>
		/// Gets the email address of person.
		/// </summary>
		public string EmailAddress { get; private set; }

		///	<summary>Timestamp</summary>
		public DateTimeOffset When
		{
			get { return _when; }
		}

		///	<summary>
		/// Gets this person's declared time zone. It will return NULL if time zone is unknown.
		/// </returns>
		public double TimeZone
		{
			get { return _tzOffset.TotalMinutes; }
		}

		///	<summary>
		/// Gets this person's declared time zone as minutes east of UTC.
		/// If the timezone is to the west of UTC it is negative.
		/// </summary>
		public TimeSpan TimeZoneOffset
		{
			get { return _tzOffset; }
		}

		public override int GetHashCode()
		{
			return EmailAddress.GetHashCode() ^ _when.GetHashCode();
		}

		public override bool Equals(object o)
		{
			var p = o as PersonIdent;
			if (p == null) return false;

			return Name == p.Name
				&& EmailAddress == p.EmailAddress
				&& _when == p._when;
		}

		///	<summary>
		/// Format for Git storage.
		///	</summary>
		///	<returns> a string in the git author format </returns>
		public string ToExternalString()
		{
			var r = new StringBuilder();

			r.Append(Name);
			r.Append(" <");
			r.Append(EmailAddress);
			r.Append("> ");
			r.Append(_when.ToGitInternalTime());
			r.Append(' ');
			AppendTimezone(r);

			return r.ToString();
		}

		private void AppendTimezone(StringBuilder r)
		{
			double offset = _tzOffset.TotalMinutes;
			char sign;

			if (offset < 0)
			{
				sign = '-';
				offset *= -1;
			}
			else
			{
				sign = '+';
			}

			double offsetHours = Math.Truncate(offset / 60);
			double offsetMins = offset % 60;

			r.AppendFormat(CultureInfo.InvariantCulture, "{2}{0:0#}{1:0#}", offsetHours, offsetMins, sign);
		}

		public override string ToString()
		{
			var r = new StringBuilder();

			r.Append("PersonIdent[");
			r.Append(Name);
			r.Append(", ");
			r.Append(EmailAddress);
			r.Append(", ");
			r.Append(_when.ToIsoFormatDate());
			r.Append("]");

			return r.ToString();
		}
	}
}