/*
 * Created by SharpDevelop.
 * User: rolenun
 * Date: 3/4/2010
 * Time: 11:44 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using NDesk.Options;

namespace GitSharp.CLI
{
	/// <summary>
	/// Description of UnitTest.
	/// </summary>
	[Command(complete = false, common = true, requiresRepository = true, usage = "Checkout a branch or paths to the working tree")]
	public class UnitTest : TextBuiltin
	{
		
		private bool isQuiet = false;
		private bool isVerbose = false;
		private bool processMultiplePaths = false;
		List<String> argumentsRemaining = new List<String>();
		List<String> filePaths = null;
		
		public List<String> ArgumentsRemaining
		{
			get { return argumentsRemaining;}
			set { argumentsRemaining = value; }
		}
		
		public List<String> FilePaths
		{
			get { return filePaths;}
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
                
            } catch (OptionException e) {
                Console.WriteLine(e.Message);
            }
		}
	}
}
