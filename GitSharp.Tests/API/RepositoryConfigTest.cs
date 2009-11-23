using System.IO;
using NUnit.Framework;

namespace GitSharp.Tests.API
{
    [TestFixture]
    public class RepositoryConfigTest : ApiTestCase
    {
        [Test]
        public void PersistsSavesConfigChangesToDisk()
        {
            string workingDirectory = Path.Combine(trash.FullName, Path.GetRandomFileName());

            // Creating a new repo
            using (var repo = Repository.Init(workingDirectory))
            {
                // Setting the in-memory commitencoding configuration entry
                repo.Config["section.key"] = "value";
                
                // Saving to disk
                repo.Config.Persist();
            }

            using (var repo = new Repository(workingDirectory))
            {
                Assert.AreEqual("value", repo.Config["section.key"]);
            }
        }
    }
}
