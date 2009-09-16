/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
using GitSharp.Tests.Util;
using GitSharp.Transport;
using Xunit;

namespace GitSharp.Tests.Transport
{
    public class PushProcessTest : RepositoryTestCase
    {
        private PushProcess _process;
        private MockTransport _transport;
        private List<RemoteRefUpdate> _refUpdates;
        private List<Ref> _advertisedRefs;
    	private static RemoteRefUpdate.UpdateStatus _connectionUpdateStatus;

		#region Nested Types

		private class MockTransport : GitSharp.Transport.Transport
        {
            private readonly List<Ref> advertised;

            public MockTransport(Repository local, URIish uri, List<Ref> advertisedRefs)
                : base(local, uri)
            {
                advertised = advertisedRefs;
            }

            public override IFetchConnection openFetch()
            {
                throw new NotSupportedException("mock");
            }

            public override IPushConnection openPush()
            {
                return new MockPushConnection(advertised);
            }

            public override void close()
            {
            }
        }

        private class MockPushConnection : BaseConnection, IPushConnection
        {
            public MockPushConnection(IEnumerable<Ref> advertisedRefs)
            {
                var refsMap = new Dictionary<string, Ref>();
                foreach (Ref r in advertisedRefs)
                    refsMap.Add(r.Name, r);
                available(refsMap);
            }

            public override void Close()
            {
            }

            public void Push(ProgressMonitor monitor, IDictionary<string, RemoteRefUpdate> refsToUpdate)
            {
                foreach (RemoteRefUpdate rru in refsToUpdate.Values)
                {
                    Assert.Equal(RemoteRefUpdate.UpdateStatus.NOT_ATTEMPTED, rru.Status);
                    rru.Status = _connectionUpdateStatus;
                }
            }
		}

		#endregion

		#region Test Setup

		protected override void SetUp()
        {
            base.SetUp();
            _advertisedRefs = new List<Ref>();
            _transport = new MockTransport(db, new URIish(), _advertisedRefs);
            _refUpdates = new List<RemoteRefUpdate>();
            _connectionUpdateStatus = RemoteRefUpdate.UpdateStatus.OK;
		}

		#endregion

		private PushResult TestOneUpdateStatus(RemoteRefUpdate rru, Ref advertisedRef, RemoteRefUpdate.UpdateStatus expectedStatus, bool checkFastForward, bool fastForward)
        {
            _refUpdates.Add(rru);
            
			if (advertisedRef != null)
            {
            	_advertisedRefs.Add(advertisedRef);
            }

            PushResult result = ExecutePush();
            Assert.Equal(expectedStatus, rru.Status);

            if (checkFastForward)
            {
            	Assert.Equal(fastForward, rru.FastForward);
            }

            return result;
        }

        private PushResult ExecutePush()
        {
            _process = new PushProcess(_transport, _refUpdates);
            return _process.execute(new TextProgressMonitor());
        }

        [Fact]
        public void testUpdateFastForward()
        {
            var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                "refs/heads/master", false, null, null);

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.OK, true, true);
        }

        [Fact]
        public void testUpdateNonFastForwardUnknownObject()
        {
            var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                "refs/heads/master", false, null, null);

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("0000000000000000000000000000000000000001"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_NONFASTFORWARD, false, false);
        }

        [Fact]
        public void testUpdateNonFastForward()
        {
            var rru = new RemoteRefUpdate(db, "ac7e7e44c1885efb472ad54a78327d66bfc4ecef",
                "refs/heads/master", false, null, null);

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_NONFASTFORWARD, false, false);
        }

        [Fact]
        public void testUpdateNonFastForwardForced()
        {
            var rru = new RemoteRefUpdate(db, "ac7e7e44c1885efb472ad54a78327d66bfc4ecef",
                "refs/heads/master", true, null, null);

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.OK, true, false);
        }

        [Fact]
        public void testUpdateCreateRef()
        {
            var rru = new RemoteRefUpdate(db, "ac7e7e44c1885efb472ad54a78327d66bfc4ecef",
                "refs/heads/master", false, null, null);

            TestOneUpdateStatus(rru, null, RemoteRefUpdate.UpdateStatus.OK, true, true);
        }
        
        [Fact]
        public void testUpdateDelete()
        {
            var rru = new RemoteRefUpdate(db, null, "refs/heads/master", false, null, null);
            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.OK, true, true);
        }

        [Fact]
        public void testUpdateDeleteNonExisting()
        {
            var rru = new RemoteRefUpdate(db, null, "refs/heads/master", false, null, null);
            TestOneUpdateStatus(rru, null, RemoteRefUpdate.UpdateStatus.NON_EXISTING, false, false);
        }

        [Fact]
        public void testUpdateUpToDate()
        {
            var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9", 
				"refs/heads/master", false, null, null);

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.UP_TO_DATE, false, false);
        }

        [Fact]
        public void testUpdateExpectedRemote()
        {
			var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                "refs/heads/master", false, null, ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.OK, true, true);
        }

        [Fact]
        public void testUpdateUnexpectedRemote()
        {
            var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                "refs/heads/master", false, null, ObjectId.FromString("0000000000000000000000000000000000000001"));

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_REMOTE_CHANGED, false, false);
        }

        [Fact]
        public void testUpdateUnexpectedRemoteVsForce()
        {
            var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                "refs/heads/master", true, null, ObjectId.FromString("0000000000000000000000000000000000000001"));

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_REMOTE_CHANGED, false, false);
        }

        [Fact]
        public void testUpdateRejectedByConnection()
        {
            _connectionUpdateStatus = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
            var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
				"refs/heads/master", false, null, null);

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON, false, false);
        }

        [Fact]
        public void testUpdateMixedCases()
        {
            var rruOk = new RemoteRefUpdate(db, null, "refs/heads/master", false, null, null);
            var refToChange = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            var rruReject = new RemoteRefUpdate(db, null, "refs/heads/nonexisting", false, null, null);
            _refUpdates.Add(rruOk);
            _refUpdates.Add(rruReject);
            _advertisedRefs.Add(refToChange);
            ExecutePush();
            Assert.Equal(RemoteRefUpdate.UpdateStatus.OK, rruOk.Status);
            Assert.Equal(true, rruOk.FastForward);
            Assert.Equal(RemoteRefUpdate.UpdateStatus.NON_EXISTING, rruReject.Status);
        }

        [Fact]
        public void testTrackingRefUpdateEnabled()
        {
            var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9", "refs/heads/master", false, "refs/remotes/test/master", null);
            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            _refUpdates.Add(rru);
            _advertisedRefs.Add(@ref);
            PushResult result = ExecutePush();
            TrackingRefUpdate tru = result.GetTrackingRefUpdate("refs/remotes/test/master");
            Assert.NotEqual(null, tru);
            Assert.Equal("refs/remotes/test/master", tru.LocalName);
            Assert.Equal(RefUpdate.RefUpdateResult.New, tru.Result);
        }

        [Fact]
        public void testTrackingRefUpdateDisabled()
        {
            var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9", "refs/heads/master", false, null, null);
            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            _refUpdates.Add(rru);
            _advertisedRefs.Add(@ref);
            PushResult result = ExecutePush();
            Assert.True(result.TrackingRefUpdates.Count == 0);
        }

        [Fact]
        public void testTrackingRefUpdateOnReject()
        {
            var rru = new RemoteRefUpdate(db, "ac7e7e44c1885efb472ad54a78327d66bfc4ecef", "refs/heads/master", false, null, null);
            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            PushResult result = TestOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_NONFASTFORWARD,
                                                    false, false);
            Assert.True(result.TrackingRefUpdates.Count == 0);
        }

        [Fact]
        public void testPushResult()
        {
            var rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                "refs/heads/master", false, "refs/remotes/test/master", null);

            var @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            _refUpdates.Add(rru);
            _advertisedRefs.Add(@ref);
            PushResult result = ExecutePush();
            Assert.Equal(1, result.TrackingRefUpdates.Count);
            Assert.Equal(1, result.AdvertisedRefs.Count);
            Assert.Equal(1, result.RemoteUpdates.Count);
            Assert.NotEqual(null, result.GetTrackingRefUpdate("refs/remotes/test/master"));
            Assert.NotEqual(null, result.GetAdvertisedRef("refs/heads/master"));
            Assert.NotEqual(null, result.GetRemoteUpdate("refs/heads/master"));
        }
    }
}
