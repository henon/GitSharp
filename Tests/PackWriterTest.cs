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
using System.IO;
using System.Linq;
using GitSharp.Exceptions;
using GitSharp.RevWalk;
using GitSharp.Transport;
using NUnit.Framework;

namespace GitSharp.Tests
{
	[TestFixture]
	public class PackWriterTest : RepositoryTestCase
	{
		private static readonly IList<ObjectId> EMPTY_LIST_OBJECT = new List<ObjectId>();
		private static readonly IList<RevObject> EMPTY_LIST_REVS = new List<RevObject>();

		private PackWriter _writer;
		private MemoryStream _os;
		private PackOutputStream _cos;
		private FileInfo _packBase;
		private FileInfo _packFile;
		private FileInfo _indexFile;
		private PackFile _pack;

		#region Setup/Teardown

		[SetUp]
		public override void setUp()
		{
			base.setUp();

			_os = new MemoryStream();
			_cos = new PackOutputStream(_os);
			_packBase = new FileInfo(Path.Combine(trash.FullName, "tmp_pack"));
			_packFile = new FileInfo(Path.Combine(trash.FullName, "tmp_pack._pack"));
			_indexFile = new FileInfo(Path.Combine(trash.FullName, "tmp_pack.idx"));
			_writer = new PackWriter(db, new TextProgressMonitor());
		}

		#endregion

		///	<summary>
		/// Try to pass non-existing object as uninteresting, with ignoring setting.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testIgnoreNonExistingObjects()
		{
			ObjectId nonExisting = ObjectId.FromString("0000000000000000000000000000000000000001");
			CreateVerifyOpenPack(EMPTY_LIST_OBJECT, Enumerable.Repeat(nonExisting, 1), false, true);
			// shouldn't throw anything
		}

		///	<summary>
		/// Create pack basing on only interesting objects, then precisely verify
		///	content. No delta reuse here.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testWritePack1()
		{
			_writer.ReuseDeltas = false;
			WriteVerifyPack1();
		}

		///	<summary>
		/// Compare sizes of packs created using
		///	<seealso cref="testWritePack2DeltasReuseRefs()"/> and
		///	<seealso cref="testWritePack2DeltasReuseOffsets()"/>. 
		/// The pack with delta bases written as offsets should be smaller.
		///	</summary>
		///	<exception cref="Exception"> </exception>
		[Test]
		public void testWritePack2SizeOffsetsVsRefs()
		{
			testWritePack2DeltasReuseRefs();
			long sizePack2DeltasRefs = _cos.Length;
			tearDown();
			setUp();
			testWritePack2DeltasReuseOffsets();
			long sizePack2DeltasOffsets = _cos.Length;

			Assert.IsTrue(sizePack2DeltasRefs > sizePack2DeltasOffsets);
		}

		///	<summary>
		/// Compare sizes of packs created using <seealso cref="testWritePack4()"/> and
		///	<seealso cref="testWritePack4ThinPack()"/>. 
		/// Obviously, the thin pack should be smaller.
		///	</summary>
		///	<exception cref="Exception"> </exception>
		[Test]
		public void testWritePack4SizeThinVsNoThin()
		{
			testWritePack4();
			long sizePack4 = _cos.Length;
			tearDown();
			setUp();
			testWritePack4ThinPack();
			long sizePack4Thin = _cos.Length;

			Assert.IsTrue(sizePack4 > sizePack4Thin);
		}

		[Test]
		public void testWriteIndex()
		{
			_writer.setIndexVersion(2);
			WriteVerifyPack4(false);

			// Validate that IndexPack came up with the right CRC32 value.
			PackIndex idx1 = PackIndex.Open(_indexFile);
			Assert.IsTrue(idx1 is PackIndexV2);
			Assert.AreEqual(0x4743F1E4L, idx1.FindCRC32(ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7")));

			// Validate that an index written by PackWriter is the same.
			FileInfo idx2File = new FileInfo(_indexFile.DirectoryName+ ".2");
			var @is = new FileStream(idx2File.FullName, System.IO.FileMode.CreateNew);
			try
			{
				_writer.writeIndex(@is);
			}
			finally
			{
				@is.Close();
			}
			PackIndex idx2 = PackIndex.Open(idx2File);
			Assert.IsInstanceOfType(typeof (PackIndexV2), idx2);
			Assert.AreEqual(idx1.ObjectCount, idx2.ObjectCount);
			Assert.AreEqual(idx1.Offset64Count, idx2.Offset64Count);

			for (int i = 0; i < idx1.ObjectCount; i++)
			{
				ObjectId id = idx1.GetObjectId(i);
				Assert.AreEqual(id, idx2.GetObjectId(i));
				Assert.AreEqual(idx1.FindOffset(id), idx2.FindOffset(id));
				Assert.AreEqual(idx1.FindCRC32(id), idx2.FindCRC32(id));
			}
		}

		// TODO: testWritePackDeltasCycle()
		// TODO: testWritePackDeltasDepth()

		private void WriteVerifyPack1()
		{
			var interestings = new LinkedList<ObjectId>();
			interestings.AddLast(ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7"));
			CreateVerifyOpenPack(interestings, EMPTY_LIST_OBJECT, false, false);

			var expectedOrder = new[]
			                    	{
			                    		ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7"),
			                    		ObjectId.FromString("c59759f143fb1fe21c197981df75a7ee00290799"),
			                    		ObjectId.FromString("540a36d136cf413e4b064c2b0e0a4db60f77feab"),
			                    		ObjectId.FromString("aabf2ffaec9b497f0950352b3e582d73035c2035"),
			                    		ObjectId.FromString("902d5476fa249b7abc9d84c611577a81381f0327"),
			                    		ObjectId.FromString("4b825dc642cb6eb9a060e54bf8d69288fbee4904"),
			                    		ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"),
			                    		ObjectId.FromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3")
			                    	};

			Assert.AreEqual(expectedOrder.Length, _writer.getObjectsNumber());
			VerifyObjectsOrder(expectedOrder);
			Assert.AreEqual("34be9032ac282b11fa9babdc2b2a93ca996c9c2f", _writer.computeName().Name);
		}

		private void WriteVerifyPack2(bool deltaReuse)
		{
			_writer.ReuseDeltas = deltaReuse;
			var interestings = new LinkedList<ObjectId>();
			interestings.AddLast(ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7"));
			var uninterestings = new LinkedList<ObjectId>();
			uninterestings.AddLast(ObjectId.FromString("540a36d136cf413e4b064c2b0e0a4db60f77feab"));
			CreateVerifyOpenPack(interestings, uninterestings, false, false);

			var expectedOrder = new[]
			                    	{
			                    		ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7"),
			                    		ObjectId.FromString("c59759f143fb1fe21c197981df75a7ee00290799"),
			                    		ObjectId.FromString("aabf2ffaec9b497f0950352b3e582d73035c2035"),
			                    		ObjectId.FromString("902d5476fa249b7abc9d84c611577a81381f0327"),
			                    		ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"),
			                    		ObjectId.FromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3")
			                    	};
			if (deltaReuse)
			{
				// objects order influenced (swapped) by delta-base first rule
				ObjectId temp = expectedOrder[4];
				expectedOrder[4] = expectedOrder[5];
				expectedOrder[5] = temp;
			}

			Assert.AreEqual(expectedOrder.Length, _writer.getObjectsNumber());
			VerifyObjectsOrder(expectedOrder);
			Assert.AreEqual("ed3f96b8327c7c66b0f8f70056129f0769323d86", _writer.computeName().Name);
		}

		private void WriteVerifyPack4(bool thin)
		{
			var interestings = new LinkedList<ObjectId>();
			interestings.AddLast(ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7"));
			var uninterestings = new LinkedList<ObjectId>();
			uninterestings.AddLast(ObjectId.FromString("c59759f143fb1fe21c197981df75a7ee00290799"));
			CreateVerifyOpenPack(interestings, uninterestings, thin, false);

			var writtenObjects = new[]
			                     	{
			                     		ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7"),
			                     		ObjectId.FromString("aabf2ffaec9b497f0950352b3e582d73035c2035"),
			                     		ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259")
			                     	};

			Assert.AreEqual(writtenObjects.Length, _writer.getObjectsNumber());
			ObjectId[] expectedObjects;
			if (thin)
			{
				expectedObjects = new ObjectId[4];
				Array.Copy(writtenObjects, 0, expectedObjects, 0, writtenObjects.Length);
				expectedObjects[3] = ObjectId.FromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3");
			}
			else
			{
				expectedObjects = writtenObjects;
			}

			VerifyObjectsOrder(expectedObjects);
			Assert.AreEqual("cded4b74176b4456afa456768b2b5aafb41c44fc", _writer.computeName().Name);
		}

		private void CreateVerifyOpenPack(IEnumerable<ObjectId> interestings, 
			IEnumerable<ObjectId> uninterestings, bool thin, bool ignoreMissingUninteresting)
		{
			_writer.Thin = thin;
			_writer.IgnoreMissingUninteresting = ignoreMissingUninteresting;
			_writer.preparePack(interestings, uninterestings);
			_writer.writePack(_cos);
			VerifyOpenPack(thin);
		}

		private void CreateVerifyOpenPack(IEnumerable<RevObject> objectSource)
		{
			_writer.preparePack(objectSource);
			_writer.writePack(_cos);
			VerifyOpenPack(false);
		}

		private void VerifyOpenPack(bool thin)
		{
			IndexPack indexer;
			Stream @is;

			if (thin)
			{
				@is = new MemoryStream(_os.ToArray());
				indexer = new IndexPack(db, @is, _packBase);
				try
				{
					indexer.index(new TextProgressMonitor());
					Assert.Fail("indexer should grumble about missing object");
				}
				catch (IOException)
				{
					// expected
				}
			}

			@is = new MemoryStream(_os.ToArray());
			indexer = new IndexPack(db, @is, _packBase);
			indexer.setKeepEmpty(true);
			indexer.setFixThin(thin);
			indexer.setIndexVersion(2);
			indexer.index(new TextProgressMonitor());
			_pack = new PackFile(_indexFile, _packFile);
		}

		private void VerifyObjectsOrder(ObjectId[] objectsOrder)
		{
			var entries = new SortedList<long, PackIndex.MutableEntry>();

			foreach (PackIndex.MutableEntry me in _pack)
			{
				entries.Add(me.Offset, me.CloneEntry());
			}

			int i = 0;
			foreach (PackIndex.MutableEntry me in entries.Values)
			{
				Assert.AreEqual(objectsOrder[i++].ToObjectId(), me.ToObjectId());
			}
		}

		///	 <summary>
		/// Test constructor for exceptions, default settings, initialization.
		/// </summary>
		[Test]
		public void testContructor()
		{
			Assert.AreEqual(false, _writer.DeltaBaseAsOffset);
			Assert.AreEqual(true, _writer.ReuseDeltas);
			Assert.AreEqual(true, _writer.ReuseObjects);
			Assert.AreEqual(0, _writer.getObjectsNumber());
		}

		///	<summary>
		/// Change default settings and verify them.
		/// </summary>
		[Test]
		public void testModifySettings()
		{
			_writer.DeltaBaseAsOffset = true;
			_writer.ReuseDeltas = false;
			_writer.ReuseObjects = false;

			Assert.AreEqual(true, _writer.DeltaBaseAsOffset);
			Assert.AreEqual(false, _writer.ReuseDeltas);
			Assert.AreEqual(false, _writer.ReuseObjects);
		}

		///	<summary>
		/// Try to pass non-existing object as uninteresting, with non-ignoring
		///	setting.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testNotIgnoreNonExistingObjects()
		{
			AssertHelper.Throws<MissingObjectException>(() =>
			                                            	{
																ObjectId nonExisting = ObjectId.FromString("0000000000000000000000000000000000000001");
																CreateVerifyOpenPack(EMPTY_LIST_OBJECT, Enumerable.Repeat(nonExisting, 1), false, false);
			                                            	});
		}

		///	<summary>
		/// Write empty pack by providing empty sets of interesting/uninteresting
		///	objects and check for correct format.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testWriteEmptyPack1()
		{
			CreateVerifyOpenPack(EMPTY_LIST_OBJECT, EMPTY_LIST_OBJECT, false, false);

			Assert.AreEqual(0, _writer.getObjectsNumber());
			Assert.AreEqual(0, _pack.ObjectCount);
			Assert.AreEqual("da39a3ee5e6b4b0d3255bfef95601890afd80709", _writer.computeName().Name);
		}

		/// <summary>
		/// Write empty pack by providing empty iterator of objects to write and
		/// check for correct format.
		/// </summary>
		/// <exception cref="IOException"> </exception>
		[Test]
		public void testWriteEmptyPack2()
		{
			CreateVerifyOpenPack(EMPTY_LIST_REVS);

			Assert.AreEqual(0, _writer.getObjectsNumber());
			Assert.AreEqual(0, _pack.ObjectCount);
		}

		///	<summary>
		/// Test writing pack without object reuse. Pack content/preparation as in
		///	<seealso cref="testWritePack1()"/>.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public virtual void testWritePack1NoObjectReuse()
		{
			_writer.ReuseDeltas = false;
			_writer.ReuseObjects = false;
			WriteVerifyPack1();
		}

		///	<summary>
		/// Create pack basing on both interesting and uninteresting objects, then
		///	precisely verify content. No delta reuse here.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testWritePack2()
		{
			WriteVerifyPack2(false);
		}

		///	<summary>
		/// Test pack writing with delta reuse. Raw-data copy (reuse) is made on a
		///	pack with CRC32 index. Pack configuration as in
		///	<seealso cref="testWritePack2DeltasReuseRefs()"/>.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testWritePack2DeltasCRC32Copy()
		{
			var packDir = new FileInfo(Path.Combine(db.ObjectsDirectory.FullName, "pack"));
			var crc32Pack = new FileInfo(Path.Combine(packDir.DirectoryName, "pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f.pack"));
			var crc32Idx = new FileInfo(Path.Combine(packDir.DirectoryName, "pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f.idx"));
			FileInfo packFile = new FileInfo("Resources/pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f.idxV2");
			packFile.CopyTo(crc32Idx.FullName);
			db.OpenPack(crc32Pack, crc32Idx);

			WriteVerifyPack2(true);
		}

		///	<summary> 
		/// Test pack writing with delta reuse. Delta bases referred as offsets. Pack
		///	configuration as in <seealso cref="testWritePack2DeltasReuseRefs()"/>.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testWritePack2DeltasReuseOffsets()
		{
			_writer.DeltaBaseAsOffset = true;
			WriteVerifyPack2(true);
		}

		///	<summary>
		/// Test pack writing with deltas reuse, delta-base first rule. Pack
		///	content/preparation as in <seealso cref="testWritePack2()"/>.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testWritePack2DeltasReuseRefs()
		{
			WriteVerifyPack2(true);
		}

		///	<summary>
		/// Compare sizes of packs created using <seealso cref="testWritePack2()"/> and
		///	<seealso cref="testWritePack2DeltasReuseRefs()"/>. The pack using deltas should
		///	be smaller.
		///	</summary>
		///	<exception cref="Exception"> </exception>
		[Test]
		public void testWritePack2SizeDeltasVsNoDeltas()
		{
			testWritePack2();
			long sizePack2NoDeltas = _cos.Length;
			tearDown();
			setUp();
			testWritePack2DeltasReuseRefs();
			long sizePack2DeltasRefs = _cos.Length;

			Assert.IsTrue(sizePack2NoDeltas > sizePack2DeltasRefs);
		}

		///	<summary>
		/// Create pack basing on fixed objects list, then precisely verify content.
		///	No delta reuse here.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		///	<exception cref="MissingObjectException">
		///	</exception>
		[Test]
		public void testWritePack3()
		{
			_writer.ReuseDeltas = false;
			var forcedOrder = new[]
			                  	{
			                  		ObjectId.FromString("82c6b885ff600be425b4ea96dee75dca255b69e7"),
			                  		ObjectId.FromString("c59759f143fb1fe21c197981df75a7ee00290799"),
			                  		ObjectId.FromString("aabf2ffaec9b497f0950352b3e582d73035c2035"),
			                  		ObjectId.FromString("902d5476fa249b7abc9d84c611577a81381f0327"),
			                  		ObjectId.FromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"),
			                  		ObjectId.FromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3")
			                  	};
			var parser = new GitSharp.RevWalk.RevWalk(db);
			var forcedOrderRevs = new RevObject[forcedOrder.Length];

			for (int i = 0; i < forcedOrder.Length; i++)
			{
				forcedOrderRevs[i] = parser.parseAny(forcedOrder[i]);
			}

			CreateVerifyOpenPack(forcedOrderRevs.AsEnumerable());

			Assert.AreEqual(forcedOrder.Length, _writer.getObjectsNumber());
			VerifyObjectsOrder(forcedOrder);
			Assert.AreEqual("ed3f96b8327c7c66b0f8f70056129f0769323d86", _writer.computeName().Name);
		}

		///	<summary>
		/// Another pack creation: basing on both interesting and uninteresting
		///	objects. No delta reuse possible here, as this is a specific case when we
		///	write only 1 commit, associated with 1 tree, 1 blob.
		///	</summary>
		///	 <exception cref="IOException"> </exception>
		[Test]
		public void testWritePack4()
		{
			WriteVerifyPack4(false);
		}

		///	<summary>
		/// Test thin pack writing: 1 blob delta base is on objects edge. Pack
		///	configuration as in <seealso cref="testWritePack4()"/>.
		///	</summary>
		///	<exception cref="IOException"> </exception>
		[Test]
		public void testWritePack4ThinPack()
		{
			WriteVerifyPack4(true);
		}
	}
}