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

using GitSharp.Core.Transport;
using NUnit.Framework;
using GitSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests.Transport
{
    [TestFixture]
    public class PushProcessTest : RepositoryTestCase
    {
        private PushProcess process;
        private MockTransport transport;
        private List<RemoteRefUpdate> refUpdates;
        private List<Ref> advertisedRefs;
        public static RemoteRefUpdate.UpdateStatus connectionUpdateStatus;

        private class MockTransport : GitSharp.Core.Transport.Transport
        {
            private readonly List<Ref> advertised;

            public MockTransport(Core.Repository local, URIish uri, List<Ref> advertisedRefs)
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
                Dictionary<string, Ref> refsMap = new Dictionary<string, Ref>();
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
                    Assert.AreEqual(RemoteRefUpdate.UpdateStatus.NOT_ATTEMPTED, rru.Status);
                    rru.Status = PushProcessTest.connectionUpdateStatus;
                }
            }
        }

        public override void setUp()
        {
            base.setUp();
            advertisedRefs = new List<Ref>();
            transport = new MockTransport(db, new URIish(), advertisedRefs);
            refUpdates = new List<RemoteRefUpdate>();
            connectionUpdateStatus = RemoteRefUpdate.UpdateStatus.OK;
        }

        private PushResult testOneUpdateStatus(RemoteRefUpdate rru, Ref advertisedRef, RemoteRefUpdate.UpdateStatus expectedStatus, bool? fastForward)
        {
            refUpdates.Add(rru);
            if (advertisedRef != null)
                advertisedRefs.Add(advertisedRef);
            PushResult result = executePush();
            Assert.AreEqual(expectedStatus, rru.Status);
            if (fastForward.HasValue)
                Assert.AreEqual(fastForward.Value, rru.FastForward);
            return result;
        }

        private PushResult executePush()
        {
            process = new PushProcess(transport, refUpdates);
            return process.execute(new TextProgressMonitor());
        }

        [Test]
        public void testUpdateFastForward()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                                                      "refs/heads/master", false, null, null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.OK, true);
        }

        [Test]
        public void testUpdateNonFastForwardUnknownObject()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                                                      "refs/heads/master", false, null, null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("0000000000000000000000000000000000000001"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_NONFASTFORWARD, null);
        }

        [Test]
        public void testUpdateNonFastForward()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "ac7e7e44c1885efb472ad54a78327d66bfc4ecef",
                                                      "refs/heads/master", false, null, null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_NONFASTFORWARD, null);
        }

        [Test]
        public void testUpdateNonFastForwardForced()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "ac7e7e44c1885efb472ad54a78327d66bfc4ecef",
                                          "refs/heads/master", true, null, null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.OK, false);
        }

        [Test]
        public void testUpdateCreateRef()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "ac7e7e44c1885efb472ad54a78327d66bfc4ecef",
                              "refs/heads/master", false, null, null);
            testOneUpdateStatus(rru, null, RemoteRefUpdate.UpdateStatus.OK, true);
        }
        
        [Test]
        public void testUpdateDelete()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, null, "refs/heads/master", false, null, null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.OK, true);
        }

        [Test]
        public void testUpdateDeleteNonExisting()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, null, "refs/heads/master", false, null, null);
            testOneUpdateStatus(rru, null, RemoteRefUpdate.UpdateStatus.NON_EXISTING, null);
        }

        [Test]
        public void testUpdateUpToDate()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9", "refs/heads/master", false, null, null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.UP_TO_DATE, null);
        }

        [Test]
        public void testUpdateExpectedRemote()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                                                      "refs/heads/master", false, null,
                                                      ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.OK, true);
        }

        [Test]
        public void testUpdateUnexpectedRemote()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                                                      "refs/heads/master", false, null,
                                                      ObjectId.FromString("0000000000000000000000000000000000000001"));
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_REMOTE_CHANGED, null);
        }

        [Test]
        public void testUpdateUnexpectedRemoteVsForce()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                                          "refs/heads/master", true, null,
                                          ObjectId.FromString("0000000000000000000000000000000000000001"));
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_REMOTE_CHANGED, null);
        }

        [Test]
        public void testUpdateRejectedByConnection()
        {
            connectionUpdateStatus = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                                                      "refs/heads/master", false, null, null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON, null);
        }

        [Test]
        public void testUpdateMixedCases()
        {
            RemoteRefUpdate rruOk = new RemoteRefUpdate(db, null, "refs/heads/master", false, null, null);
            Ref refToChange = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            RemoteRefUpdate rruReject = new RemoteRefUpdate(db, null, "refs/heads/nonexisting", false, null, null);
            refUpdates.Add(rruOk);
            refUpdates.Add(rruReject);
            advertisedRefs.Add(refToChange);
            executePush();
            Assert.AreEqual(RemoteRefUpdate.UpdateStatus.OK, rruOk.Status);
            Assert.AreEqual(true, rruOk.FastForward);
            Assert.AreEqual(RemoteRefUpdate.UpdateStatus.NON_EXISTING, rruReject.Status);
        }

        [Test]
        public void testTrackingRefUpdateEnabled()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9", "refs/heads/master", false, "refs/remotes/test/master", null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            refUpdates.Add(rru);
            advertisedRefs.Add(@ref);
            PushResult result = executePush();
            TrackingRefUpdate tru = result.GetTrackingRefUpdate("refs/remotes/test/master");
            Assert.IsNotNull(tru);
            Assert.AreEqual("refs/remotes/test/master", tru.LocalName);
            Assert.AreEqual(RefUpdate.RefUpdateResult.New, tru.Result);
        }

        [Test]
        public void testTrackingRefUpdateDisabled()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9", "refs/heads/master", false, null, null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            refUpdates.Add(rru);
            advertisedRefs.Add(@ref);
            PushResult result = executePush();
            Assert.IsTrue(result.TrackingRefUpdates.Count == 0);
        }

        [Test]
        public void testTrackingRefUpdateOnReject()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "ac7e7e44c1885efb472ad54a78327d66bfc4ecef", "refs/heads/master", false, null, null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("2c349335b7f797072cf729c4f3bb0914ecb6dec9"));
            PushResult result = testOneUpdateStatus(rru, @ref, RemoteRefUpdate.UpdateStatus.REJECTED_NONFASTFORWARD, null);
            Assert.IsTrue(result.TrackingRefUpdates.Count == 0);
        }

        [Test]
        public void testPushResult()
        {
            RemoteRefUpdate rru = new RemoteRefUpdate(db, "2c349335b7f797072cf729c4f3bb0914ecb6dec9",
                                                      "refs/heads/master", false, "refs/remotes/test/master", null);
            Ref @ref = new Ref(Ref.Storage.Loose, "refs/heads/master", ObjectId.FromString("ac7e7e44c1885efb472ad54a78327d66bfc4ecef"));
            refUpdates.Add(rru);
            advertisedRefs.Add(@ref);
            PushResult result = executePush();
            Assert.AreEqual(1, result.TrackingRefUpdates.Count);
            Assert.AreEqual(1, result.AdvertisedRefs.Count);
            Assert.AreEqual(1, result.RemoteUpdates.Count);
            Assert.IsNotNull(result.GetTrackingRefUpdate("refs/remotes/test/master"));
            Assert.IsNotNull(result.GetAdvertisedRef("refs/heads/master"));
            Assert.IsNotNull(result.GetRemoteUpdate("refs/heads/master"));
        }
    }
}
