using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp
{
	[TestFixture]
	public class AbstractTreeNodeTests : ApiTestCase
	{
		[Ignore("We can't yet filter out branch merges which actually don't change the file but are detected as additions due to lack of a three-way commit diff")]
		[Test]
		public void GetHistory()
		{
			using (var repo = GetTrashRepository())
			{
				var master_txt = repo.Get<Leaf>("master.txt");
				var commits = master_txt.GetHistory().ToArray();
				var history = commits.Select(c => c.Hash).ToArray();

				Assert.AreEqual(new[] { "58be4659bb571194ed4562d04b359d26216f526e", "6c8b137b1c652731597c89668f417b8695f28dd7" }, history);
			}
		}
	}
}
