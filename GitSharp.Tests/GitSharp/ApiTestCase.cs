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
using System.IO;
using GitSharp.Core.Tests;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp
{
	public class ApiTestCase : SampleDataRepositoryTestCase
	{
		protected Repository GetTrashRepository()
		{
			return new Repository(db.WorkingDirectory.FullName);
		}

		protected void AssertFileExistsInWD(string path)
		{
			var wd = db.WorkingDirectory.FullName;
			Assert.IsTrue(new FileInfo(Path.Combine(wd, path)).Exists, "Path '" + path + "' should exist in the working directory");
		}

		protected void AssertFileNotExistentInWD(string path)
		{
			var wd = db.WorkingDirectory.FullName;
			Assert.IsFalse(new FileInfo(Path.Combine(wd, path)).Exists, "Path '" + path + "' should *not* exist in the working directory");
		}

		protected void AssertFileContentInWDEquals(string path, string content)
		{
			var wd = db.WorkingDirectory.FullName;
			Assert.AreEqual(content, File.ReadAllText(Path.Combine(wd, path)));
		}

	}
}