using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Core;
using GitSharp.Core.Transport;
using NUnit.Framework;

namespace GitSharp.Tests.Transport
{
    [TestFixture]
    public class BaseConnectionTests
    {
        [Test]
        public void ShouldReturnNullForAnInvalidRef()
        {
            var connection = new StubConnection();

            Assert.IsNull(connection.GetRef("invalid ref"));
        }

        [Test]
        public void ShouldReturnValueForAValidRef()
        {
            var connection = new StubConnection();
            var r = new Core.Ref(null, "ref", ObjectId.ZeroId);

            connection.RefsMap.Add("ref", r);

            Assert.AreEqual(r, connection.GetRef("ref"));
        }

        private class StubConnection : BaseConnection, IPushConnection
        {
            public override void Close()
            {}

            public void Push(ProgressMonitor monitor, IDictionary<string, RemoteRefUpdate> refUpdates)
            {}
        }
    }
}
