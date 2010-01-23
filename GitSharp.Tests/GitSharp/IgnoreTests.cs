/*
 * Copyright (C) 2009, Matt DeKrey <mattdekrey@gmail.com>
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
using NUnit.Framework;

namespace GitSharp.API.Tests
{
	[TestFixture]
	public class IgnoreTests
	{
		[Test]
		public void TestIgnore()
		{
			var rules = new GitSharp.IgnoreRules(new string[] {
                                                                  "*.[oa]", // ignore all *.o and *.a files
                                                                  "#!*.[oa]", // make sure comments are followed
                                                                  "*.html", // ignore all *.html
                                                                  "!foo.html", // except when they're foo.html
                                                                  "!Documentation/index.html", // and except when they're the index file in any Documentation directory
                                                                  "/Documentation/index.html", // unless it's the root's Documentation/index.html
                                                                  "bin/", // ignore bin directories and all paths under them
                                                                  "!/bin/", // except if it's in the root
                                                              });

			Assert.AreEqual(false, rules.IgnoreFile("project/", "project/Documentation/foo.html"));
			Assert.AreEqual(true, rules.IgnoreFile("project/", "project/Documentation/gitignore.html"));
			Assert.AreEqual(false, rules.IgnoreFile("project/", "project/src/Documentation/index.html"));
			Assert.AreEqual(true, rules.IgnoreFile("project/", "project/Documentation/index.html"));
			Assert.AreEqual(true, rules.IgnoreFile("project/", "project/gitignore.html"));

			Assert.AreEqual(true, rules.IgnoreFile("project/", "project/file.o"));
			Assert.AreEqual(true, rules.IgnoreFile("project/", "project/lib.a"));
			Assert.AreEqual(true, rules.IgnoreFile("project/", "project/src/internal.o"));

			Assert.AreEqual(false, rules.IgnoreFile("project/", "project/Program.cs"));
			Assert.AreEqual(false, rules.IgnoreFile("project/", "project/Program.suo"));

			Assert.AreEqual(false, rules.IgnoreFile("project/", "project/bin"));
			Assert.AreEqual(false, rules.IgnoreDir("project/", "project/bin"));
			Assert.AreEqual(false, rules.IgnoreFile("project/", "project/data/bin"));
			Assert.AreEqual(true, rules.IgnoreDir("project/", "project/src/bin"));
			Assert.AreEqual(true, rules.IgnoreDir("project/", "project/src/bin/Project.dll"));
			Assert.AreEqual(true, rules.IgnoreDir("project/", "project/src/bin/Project.pdb"));
		}
	}
}