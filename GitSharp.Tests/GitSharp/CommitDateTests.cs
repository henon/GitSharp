/*
 * Copyright (C) 2009, James Gregory
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
using GitSharp.Tests.GitSharp;
using NUnit.Framework;

namespace GitSharp.API.Tests
{
	[TestFixture]
	public class CommitDateTests : ApiTestCase
	{
		private const string KnownCommit = "49322bb17d3acc9146f98c97d078513228bbf3c0";
		private static readonly DateTimeOffset _expectedDate = new DateTimeOffset(2005, 04, 07, 15, 32, 13, new TimeSpan(-7, 0, 0));

		[Test]
		public void ShouldBeAbleToReadAuthorDate()
		{
			var expectedDate = new DateTimeOffset();

			try
			{
				expectedDate = DateTimeOffset.Parse("2005-04-07 15:32:13 -07:00");
			}
			catch (NotImplementedException)
			{
				Assert.Ignore("Doesn't pass on Mono because of 'System.NotImplementedException : The requested feature is not implemented.at System.DateTimeOffset.Parse'.");
			}

			using (var repos = GetTrashRepository())
			{
				var commit = new Commit(repos, KnownCommit);

				Assert.AreEqual(expectedDate, commit.AuthorDate);
			}
		}

		[Test]
		public void ShouldBeAbleToReadAuthorDate2()
		{
			using (var repos = GetTrashRepository())
			{
				var commit = new Commit(repos, KnownCommit);

				Assert.AreEqual(_expectedDate, commit.AuthorDate);
			}
		}

		[Test]
		public void ShouldBeAbleToReadCommitDate()
		{
			using (var repos = GetTrashRepository())
			{
				var commit = new Commit(repos, KnownCommit);

				Assert.AreEqual(_expectedDate, commit.CommitDate);
			}
		}
	}
}