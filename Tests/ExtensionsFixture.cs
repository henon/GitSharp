using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace GitSharp.Tests
{
    public class ExtensionsFixture
    {
        [Fact]
        public void IsDirectory()
        {
            var filePath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Environment.CurrentDirectory;

			Assert.NotNull(filePath);

            Assert.True(new FileInfo(directoryPath).IsDirectory());
            Assert.True(new DirectoryInfo(directoryPath).IsDirectory());
            Assert.False(new FileInfo(filePath).IsDirectory());
            Assert.False(new DirectoryInfo(filePath).IsDirectory());
        }

        [Fact]
        public void IsFile()
        {
            var filePath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Environment.CurrentDirectory;

			Assert.NotNull(filePath);

            Assert.True(new FileInfo(filePath).IsFile());
            Assert.True(new DirectoryInfo(filePath).IsFile());
            Assert.False(new FileInfo(directoryPath).IsFile());
            Assert.False(new DirectoryInfo(directoryPath).IsFile());
        }
    }
}
