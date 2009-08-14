using System.IO;
using System.Text;
using GitSharp.Patch;
using NUnit.Framework;

namespace GitSharp.Tests.Patch
{
	public class BasePatchTest
	{
		protected const string PATCHS_DIR = "../../../Tests/Patch/Resources/";

		protected GitSharp.Patch.Patch parseTestPatchFile(string patchFile)
		{
			try
			{
				using (Stream inStream = new FileStream(patchFile, System.IO.FileMode.Open))
				{
					GitSharp.Patch.Patch p = new GitSharp.Patch.Patch();
					p.parse(inStream);
					return p;
				}
			}
			catch(IOException)
			{
				Assert.Fail("No " + patchFile + " test vector");
				return null; // Never happens
			}
		}

		protected string GetAllErrorsFromPatch(GitSharp.Patch.Patch patch)
		{
			if (patch == null || patch.getErrors().Count == 0)
			{
				return string.Empty;
			}

			StringBuilder sb = new StringBuilder();

			foreach (FormatError formatError in patch.getErrors())
			{
				sb.AppendLine(formatError.getMessage());
			}

			return sb.ToString();
		}
	}
}