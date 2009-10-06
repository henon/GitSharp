using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests
{
    [TestFixture]
    public class ExtensionsFixture
    {
        [Test]
        public void IsDirectory()
        {
            var filePath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Environment.CurrentDirectory;

            Assert.IsTrue(new FileInfo(directoryPath).IsDirectory());
            Assert.IsTrue(new DirectoryInfo(directoryPath).IsDirectory());
            Assert.IsFalse(new FileInfo(filePath).IsDirectory());
            Assert.IsFalse(new DirectoryInfo(filePath).IsDirectory());
        }

        [Test]
        public void IsFile()
        {
            var filePath = Assembly.GetExecutingAssembly().Location;
            var directoryPath = Environment.CurrentDirectory;

            Assert.IsTrue(new FileInfo(filePath).IsFile());
            Assert.IsTrue(new DirectoryInfo(filePath).IsFile());
            Assert.IsFalse(new FileInfo(directoryPath).IsFile());
            Assert.IsFalse(new DirectoryInfo(directoryPath).IsFile());
        }

        [Test]
        public void getCurrentTime()
        {
            Assert.AreEqual(new DateTimeOffset(2009, 08, 15, 20, 12, 58, 668, new TimeSpan(-3, -30, 0)), 1250379778668L.MillisToDateTimeOffset((int)new TimeSpan(-3, -30, 0).TotalMinutes));
            Assert.AreEqual(new DateTime(2009, 08, 15, 23, 42, 58, 668), 1250379778668L.MillisToDateTime());
        }
    }
}
