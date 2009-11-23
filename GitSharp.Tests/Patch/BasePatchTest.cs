using System.IO;
using System.Text;
using GitSharp.Core.Patch;
using NUnit.Framework;

namespace GitSharp.Tests.Patch
{
	public class BasePatchTest
	{
		protected const string DiffsDir = "Resources/Diff/";
		protected const string PatchsDir = "Resources/Patch/";

		protected static GitSharp.Core.Patch.Patch ParseTestPatchFile(string patchFile)
		{
			try
			{
				using (var inStream = new FileStream(patchFile, System.IO.FileMode.Open))
				{
					var p = new GitSharp.Core.Patch.Patch();
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

		protected static string GetAllErrorsFromPatch(GitSharp.Core.Patch.Patch patch)
		{
			if (patch == null || patch.getErrors().Count == 0)
			{
				return string.Empty;
			}

			var sb = new StringBuilder();

			foreach (FormatError formatError in patch.getErrors())
			{
				sb.AppendLine(formatError.getMessage());
			}

			return sb.ToString();
		}
	}
}