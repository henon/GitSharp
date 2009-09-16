using System.IO;
using System.Text;
using GitSharp.Patch;
using Xunit;

namespace GitSharp.Tests.Patch
{
	public abstract class BasePatchTest : XunitBaseFact
	{
		protected const string DiffsDir = "../../../Tests/Diff/Resources/";
		protected const string PatchsDir = "../../../Tests/Patch/Resources/";

		protected static GitSharp.Patch.Patch ParseTestPatchFile(string patchFile)
		{
			try
			{
				using (var inStream = new FileStream(patchFile, System.IO.FileMode.Open))
				{
					var p = new GitSharp.Patch.Patch();
					p.parse(inStream);
					return p;
				}
			}
			catch(IOException)
			{
				Assert.False(true, "No " + patchFile + " test vector");
				return null; // Never happens
			}
		}

		protected static string GetAllErrorsFromPatch(GitSharp.Patch.Patch patch)
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