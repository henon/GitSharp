/*
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using NDesk.Options;
using NUnit.Framework;
using GitSharp.CLI;

namespace Git.Tests.CLI
{
	[TestFixture]
	public class CustomOptionTests
	{
		[Test]
		public void CanParseOptions()
		{
			string[] args = { "--quiet", "--unused", "--verbose", "--", "path1", "path2" };

			//Test without multi-path option
			//Simulates method that uses: argumentsRemaining = ParseOptions(args);
			var cmd = new UnitTest { ProcessMultiplePaths = false };
			cmd.Run(args);
			Assert.AreEqual(new List<String> { "--unused" }, cmd.ArgumentsRemaining);
			Assert.IsNull(cmd.FilePaths);

			//Test with multi-path option
			//Simulates method that uses: ParseOptions(args, out filePaths, out argumentsRemaining)
			cmd = new UnitTest { ProcessMultiplePaths = true };
			cmd.Run(args);
			Assert.AreEqual(new List<String> { "--unused" }, cmd.ArgumentsRemaining);
			Assert.AreEqual(new List<String> { "path1", "path2" }, cmd.FilePaths);
		}
	}

	public class UnitTest : TextBuiltin
	{

		private bool isQuiet = false;
		private bool isVerbose = false;
		private bool processMultiplePaths = false;
		List<String> argumentsRemaining = new List<String>();
		List<String> filePaths = null;

		public List<String> ArgumentsRemaining
		{
			get { return argumentsRemaining; }
			set { argumentsRemaining = value; }
		}

		public List<String> FilePaths
		{
			get { return filePaths; }
			set { filePaths = value; }
		}

		public bool ProcessMultiplePaths
		{
			get { return processMultiplePaths; }
			set { processMultiplePaths = value; }
		}

		override public void Run(string[] args)
		{
			options = new CmdParserOptionSet()
            {
				{ "v|verbose", "Be verbose", v=>{isVerbose = true;}},
				{ "q|quiet", "Be quiet", v=>{isQuiet = true;}},
			};

			try
			{
				if (isVerbose || isQuiet)
				{
					//Do something 
				}

				if (processMultiplePaths)
					ParseOptions(args, out filePaths, out argumentsRemaining);
				else
					argumentsRemaining = ParseOptions(args);

			}
			catch (OptionException e)
			{
				Console.WriteLine(e.Message);
			}
		}
	}
}
