/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

namespace GitSharp.Util
{
	public static class DateTimeExtensions
	{
		/// <summary>
		/// Calculates the git time from a DateTimeOffset instance.
		/// Git's internal time representation are the seconds since 1970.1.1 00:00:00 GMT.
		/// C# has a different representation: 100 nanosecs since 0001.1.1 12:00:00.
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static long ToGitInternalTime(this DateTimeOffset time)
		{
			return (time.Ticks - time.Offset.Ticks - Constants.EpochTicks) / Constants.TicksPerSecond;
		}

		/// <summary>
		/// Calculates the git time from a DateTimeOffset instance.
		/// Git's internal time representation are the seconds since 1970.1.1 00:00:00 GMT.
		/// C# has a different representation: 100 nanosecs since 0001.1.1 12:00:00.
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static long ToGitInternalTime(this DateTime time)
		{
			return new DateTimeOffset(time, TimeSpan.Zero).ToGitInternalTime();
		}

		/// <summary>
		/// Calculates the DateTimeOffset of a given git time and time zone offset in minutes.
		/// Git's internal time representation are the seconds since 1970.1.1 00:00:00 GMT.
		/// C# has a different representation: 100 nanosecs since 0001.1.1 12:00:00.
		/// </summary>
		/// <param name="gitTime"></param>
		/// <param name="offsetMinutes"></param>
		/// <returns></returns>
		public static DateTimeOffset GitTimeToDateTimeOffset(this long gitTime, long offsetMinutes)
		{
			var offset = TimeSpan.FromMinutes(offsetMinutes);
			var utcTicks = Constants.EpochTicks + gitTime * 10000;
			return new DateTimeOffset(utcTicks + offset.Ticks, offset);
		}

		/// <summary>
		/// Calculates the UTC DateTime of a given git time.
		/// Git's internal time representation are the seconds since
		/// 1970.1.1 00:00:00 GMT.
		/// C# has a different representation: 100 nanosecs since 0001.1.1 12:00:00.
		/// </summary>
		/// <param name="gitTime"></param>
		/// <returns></returns>
		public static DateTime GitTimeToDateTime(this long gitTime)
		{
			var utcTicks = Constants.EpochTicks + gitTime * Constants.TicksPerSecond;
			return new DateTime(utcTicks);
		}

		/// <summary>
		/// Gets the DateTime in the sortable ISO format.
		/// </summary>
		/// <param name="when"></param>
		/// <returns></returns>
		public static string ToIsoFormatDate(this DateTime when)
		{
			return when.ToString("s", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Gets the DateTimeOffset in the sortable ISO format.
		/// </summary>
		/// <param name="when"></param>
		/// <returns></returns>
		public static string ToIsoFormatDate(this DateTimeOffset when)
		{
			return when.ToString("s", CultureInfo.InvariantCulture);
		}
	}
}