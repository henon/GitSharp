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

using System.Text;
using GitSharp.Tests.GitSharp;
using NUnit.Framework;
using System.IO;

namespace GitSharp.API.Tests
{
	[TestFixture]
	public class BlobTests : ApiTestCase
	{

		[Test]
		public void WriteBlob() // corresponds to T0003_Basic_Write.Write_Blob
		{
			using (var repo = GetTrashRepository())
			{
				var blob = Blob.CreateFromFile(repo, "Resources/single_file_commit/i-am-a-file");
				Assert.AreEqual("95ea6a6859af6791464bd8b6de76ad5a6f9fad81", blob.Hash);
				Assert.AreEqual(File.ReadAllText("Resources/single_file_commit/i-am-a-file"), blob.Data);
				var same_blob = new Blob(repo, blob.Hash);
				Assert.AreEqual(File.ReadAllBytes("Resources/single_file_commit/i-am-a-file"), same_blob.RawData);

				blob = Blob.Create(repo, "and this is the data in me\r\n\r\n");
				Assert.AreEqual("95ea6a6859af6791464bd8b6de76ad5a6f9fad81", blob.Hash);

				blob = Blob.Create(repo, Encoding.UTF8.GetBytes("and this is the data in me\r\n\r\n"));
				Assert.AreEqual("95ea6a6859af6791464bd8b6de76ad5a6f9fad81", blob.Hash);
			}
		}
	}
}