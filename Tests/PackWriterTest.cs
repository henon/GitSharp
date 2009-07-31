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

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Tests
{
    [TestFixture]
    public class PackWriterTest : RepositoryTestCase
    {
#if false
	private static final List<ObjectId> EMPTY_LIST_OBJECT = Collections
			.<ObjectId> emptyList();

	private static final List<RevObject> EMPTY_LIST_REVS = Collections
			.<RevObject> emptyList();

	private PackWriter writer;

	private ByteArrayOutputStream os;

	private PackOutputStream cos;

	private File packBase;

	private File packFile;

	private File indexFile;

	private PackFile pack;

	public void setUp() throws Exception {
		super.setUp();
		os = new ByteArrayOutputStream();
		cos = new PackOutputStream(os);
		packBase = new File(trash, "tmp_pack");
		packFile = new File(trash, "tmp_pack.pack");
		indexFile = new File(trash, "tmp_pack.idx");
		writer = new PackWriter(db, new TextProgressMonitor());
	}

	/**
	 * Test constructor for exceptions, default settings, initialization.
	 */
	public void testContructor() {
		assertEquals(false, writer.isDeltaBaseAsOffset());
		assertEquals(true, writer.isReuseDeltas());
		assertEquals(true, writer.isReuseObjects());
		assertEquals(0, writer.getObjectsNumber());
	}

	/**
	 * Change default settings and verify them.
	 */
	public void testModifySettings() {
		writer.setDeltaBaseAsOffset(true);
		writer.setReuseDeltas(false);
		writer.setReuseObjects(false);

		assertEquals(true, writer.isDeltaBaseAsOffset());
		assertEquals(false, writer.isReuseDeltas());
		assertEquals(false, writer.isReuseObjects());
	}

	/**
	 * Write empty pack by providing empty sets of interesting/uninteresting
	 * objects and check for correct format.
	 *
	 * @throws IOException
	 */
	public void testWriteEmptyPack1() throws IOException {
		createVerifyOpenPack(EMPTY_LIST_OBJECT, EMPTY_LIST_OBJECT, false, false);

		assertEquals(0, writer.getObjectsNumber());
		assertEquals(0, pack.getObjectCount());
		assertEquals("da39a3ee5e6b4b0d3255bfef95601890afd80709", writer
				.computeName().name());
	}

	/**
	 * Write empty pack by providing empty iterator of objects to write and
	 * check for correct format.
	 *
	 * @throws IOException
	 */
	public void testWriteEmptyPack2() throws IOException {
		createVerifyOpenPack(EMPTY_LIST_REVS.iterator());

		assertEquals(0, writer.getObjectsNumber());
		assertEquals(0, pack.getObjectCount());
	}

	/**
	 * Try to pass non-existing object as uninteresting, with non-ignoring
	 * setting.
	 *
	 * @throws IOException
	 */
	public void testNotIgnoreNonExistingObjects() throws IOException {
		final ObjectId nonExisting = ObjectId
				.fromString("0000000000000000000000000000000000000001");
		try {
			createVerifyOpenPack(EMPTY_LIST_OBJECT, Collections.nCopies(1,
					nonExisting), false, false);
			fail("Should have thrown MissingObjectException");
		} catch (MissingObjectException x) {
			// expected
		}
	}

	/**
	 * Try to pass non-existing object as uninteresting, with ignoring setting.
	 *
	 * @throws IOException
	 */
	public void testIgnoreNonExistingObjects() throws IOException {
		final ObjectId nonExisting = ObjectId
				.fromString("0000000000000000000000000000000000000001");
		createVerifyOpenPack(EMPTY_LIST_OBJECT, Collections.nCopies(1,
				nonExisting), false, true);
		// shouldn't throw anything
	}

	/**
	 * Create pack basing on only interesting objects, then precisely verify
	 * content. No delta reuse here.
	 *
	 * @throws IOException
	 */
	public void testWritePack1() throws IOException {
		writer.setReuseDeltas(false);
		writeVerifyPack1();
	}

	/**
	 * Test writing pack without object reuse. Pack content/preparation as in
	 * {@link #testWritePack1()}.
	 *
	 * @throws IOException
	 */
	public void testWritePack1NoObjectReuse() throws IOException {
		writer.setReuseDeltas(false);
		writer.setReuseObjects(false);
		writeVerifyPack1();
	}

	/**
	 * Create pack basing on both interesting and uninteresting objects, then
	 * precisely verify content. No delta reuse here.
	 *
	 * @throws IOException
	 */
	public void testWritePack2() throws IOException {
		writeVerifyPack2(false);
	}

	/**
	 * Test pack writing with deltas reuse, delta-base first rule. Pack
	 * content/preparation as in {@link #testWritePack2()}.
	 *
	 * @throws IOException
	 */
	public void testWritePack2DeltasReuseRefs() throws IOException {
		writeVerifyPack2(true);
	}

	/**
	 * Test pack writing with delta reuse. Delta bases referred as offsets. Pack
	 * configuration as in {@link #testWritePack2DeltasReuseRefs()}.
	 *
	 * @throws IOException
	 */
	public void testWritePack2DeltasReuseOffsets() throws IOException {
		writer.setDeltaBaseAsOffset(true);
		writeVerifyPack2(true);
	}

	/**
	 * Test pack writing with delta reuse. Raw-data copy (reuse) is made on a
	 * pack with CRC32 index. Pack configuration as in
	 * {@link #testWritePack2DeltasReuseRefs()}.
	 *
	 * @throws IOException
	 */
	public void testWritePack2DeltasCRC32Copy() throws IOException {
		final File packDir = new File(db.getObjectsDirectory(), "pack");
		final File crc32Pack = new File(packDir,
				"pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f.pack");
		final File crc32Idx = new File(packDir,
				"pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f.idx");
		copyFile(JGitTestUtil.getTestResourceFile(
				"pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f.idxV2"),
				crc32Idx);
		db.openPack(crc32Pack, crc32Idx);

		writeVerifyPack2(true);
	}

	/**
	 * Create pack basing on fixed objects list, then precisely verify content.
	 * No delta reuse here.
	 *
	 * @throws IOException
	 * @throws MissingObjectException
	 *
	 */
	public void testWritePack3() throws MissingObjectException, IOException {
		writer.setReuseDeltas(false);
		final ObjectId forcedOrder[] = new ObjectId[] {
				ObjectId.fromString("82c6b885ff600be425b4ea96dee75dca255b69e7"),
				ObjectId.fromString("c59759f143fb1fe21c197981df75a7ee00290799"),
				ObjectId.fromString("aabf2ffaec9b497f0950352b3e582d73035c2035"),
				ObjectId.fromString("902d5476fa249b7abc9d84c611577a81381f0327"),
				ObjectId.fromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"),
				ObjectId.fromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3") };
		final RevWalk parser = new RevWalk(db);
		final RevObject forcedOrderRevs[] = new RevObject[forcedOrder.length];
		for (int i = 0; i < forcedOrder.length; i++)
			forcedOrderRevs[i] = parser.parseAny(forcedOrder[i]);

		createVerifyOpenPack(Arrays.asList(forcedOrderRevs).iterator());

		assertEquals(forcedOrder.length, writer.getObjectsNumber());
		verifyObjectsOrder(forcedOrder);
		assertEquals("ed3f96b8327c7c66b0f8f70056129f0769323d86", writer
				.computeName().name());
	}

	/**
	 * Another pack creation: basing on both interesting and uninteresting
	 * objects. No delta reuse possible here, as this is a specific case when we
	 * write only 1 commit, associated with 1 tree, 1 blob.
	 *
	 * @throws IOException
	 */
	public void testWritePack4() throws IOException {
		writeVerifyPack4(false);
	}

	/**
	 * Test thin pack writing: 1 blob delta base is on objects edge. Pack
	 * configuration as in {@link #testWritePack4()}.
	 *
	 * @throws IOException
	 */
	public void testWritePack4ThinPack() throws IOException {
		writeVerifyPack4(true);
	}

	/**
	 * Compare sizes of packs created using {@link #testWritePack2()} and
	 * {@link #testWritePack2DeltasReuseRefs()}. The pack using deltas should
	 * be smaller.
	 *
	 * @throws Exception
	 */
	public void testWritePack2SizeDeltasVsNoDeltas() throws Exception {
		testWritePack2();
		final long sizePack2NoDeltas = cos.length();
		tearDown();
		setUp();
		testWritePack2DeltasReuseRefs();
		final long sizePack2DeltasRefs = cos.length();

		assertTrue(sizePack2NoDeltas > sizePack2DeltasRefs);
	}

	/**
	 * Compare sizes of packs created using
	 * {@link #testWritePack2DeltasReuseRefs()} and
	 * {@link #testWritePack2DeltasReuseOffsets()}. The pack with delta bases
	 * written as offsets should be smaller.
	 *
	 * @throws Exception
	 */
	public void testWritePack2SizeOffsetsVsRefs() throws Exception {
		testWritePack2DeltasReuseRefs();
		final long sizePack2DeltasRefs = cos.length();
		tearDown();
		setUp();
		testWritePack2DeltasReuseOffsets();
		final long sizePack2DeltasOffsets = cos.length();

		assertTrue(sizePack2DeltasRefs > sizePack2DeltasOffsets);
	}

	/**
	 * Compare sizes of packs created using {@link #testWritePack4()} and
	 * {@link #testWritePack4ThinPack()}. Obviously, the thin pack should be
	 * smaller.
	 *
	 * @throws Exception
	 */
	public void testWritePack4SizeThinVsNoThin() throws Exception {
		testWritePack4();
		final long sizePack4 = cos.length();
		tearDown();
		setUp();
		testWritePack4ThinPack();
		final long sizePack4Thin = cos.length();

		assertTrue(sizePack4 > sizePack4Thin);
	}

	public void testWriteIndex() throws Exception {
		writer.setIndexVersion(2);
		writeVerifyPack4(false);

		// Validate that IndexPack came up with the right CRC32 value.
		final PackIndex idx1 = PackIndex.open(indexFile);
		assertTrue(idx1 instanceof PackIndexV2);
		assertEquals(0x4743F1E4L, idx1.findCRC32(ObjectId
				.fromString("82c6b885ff600be425b4ea96dee75dca255b69e7")));

		// Validate that an index written by PackWriter is the same.
		final File idx2File = new File(indexFile.getAbsolutePath() + ".2");
		final FileOutputStream is = new FileOutputStream(idx2File);
		try {
			writer.writeIndex(is);
		} finally {
			is.close();
		}
		final PackIndex idx2 = PackIndex.open(idx2File);
		assertTrue(idx2 instanceof PackIndexV2);
		assertEquals(idx1.getObjectCount(), idx2.getObjectCount());
		assertEquals(idx1.getOffset64Count(), idx2.getOffset64Count());

		for (int i = 0; i < idx1.getObjectCount(); i++) {
			final ObjectId id = idx1.getObjectId(i);
			assertEquals(id, idx2.getObjectId(i));
			assertEquals(idx1.findOffset(id), idx2.findOffset(id));
			assertEquals(idx1.findCRC32(id), idx2.findCRC32(id));
		}
	}

	// TODO: testWritePackDeltasCycle()
	// TODO: testWritePackDeltasDepth()

	private void writeVerifyPack1() throws IOException {
		final LinkedList<ObjectId> interestings = new LinkedList<ObjectId>();
		interestings.add(ObjectId
				.fromString("82c6b885ff600be425b4ea96dee75dca255b69e7"));
		createVerifyOpenPack(interestings, EMPTY_LIST_OBJECT, false, false);

		final ObjectId expectedOrder[] = new ObjectId[] {
				ObjectId.fromString("82c6b885ff600be425b4ea96dee75dca255b69e7"),
				ObjectId.fromString("c59759f143fb1fe21c197981df75a7ee00290799"),
				ObjectId.fromString("540a36d136cf413e4b064c2b0e0a4db60f77feab"),
				ObjectId.fromString("aabf2ffaec9b497f0950352b3e582d73035c2035"),
				ObjectId.fromString("902d5476fa249b7abc9d84c611577a81381f0327"),
				ObjectId.fromString("4b825dc642cb6eb9a060e54bf8d69288fbee4904"),
				ObjectId.fromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"),
				ObjectId.fromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3") };

		assertEquals(expectedOrder.length, writer.getObjectsNumber());
		verifyObjectsOrder(expectedOrder);
		assertEquals("34be9032ac282b11fa9babdc2b2a93ca996c9c2f", writer
				.computeName().name());
	}

	private void writeVerifyPack2(boolean deltaReuse) throws IOException {
		writer.setReuseDeltas(deltaReuse);
		final LinkedList<ObjectId> interestings = new LinkedList<ObjectId>();
		interestings.add(ObjectId
				.fromString("82c6b885ff600be425b4ea96dee75dca255b69e7"));
		final LinkedList<ObjectId> uninterestings = new LinkedList<ObjectId>();
		uninterestings.add(ObjectId
				.fromString("540a36d136cf413e4b064c2b0e0a4db60f77feab"));
		createVerifyOpenPack(interestings, uninterestings, false, false);

		final ObjectId expectedOrder[] = new ObjectId[] {
				ObjectId.fromString("82c6b885ff600be425b4ea96dee75dca255b69e7"),
				ObjectId.fromString("c59759f143fb1fe21c197981df75a7ee00290799"),
				ObjectId.fromString("aabf2ffaec9b497f0950352b3e582d73035c2035"),
				ObjectId.fromString("902d5476fa249b7abc9d84c611577a81381f0327"),
				ObjectId.fromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259"),
				ObjectId.fromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3") };
		if (deltaReuse) {
			// objects order influenced (swapped) by delta-base first rule
			ObjectId temp = expectedOrder[4];
			expectedOrder[4] = expectedOrder[5];
			expectedOrder[5] = temp;
		}
		assertEquals(expectedOrder.length, writer.getObjectsNumber());
		verifyObjectsOrder(expectedOrder);
		assertEquals("ed3f96b8327c7c66b0f8f70056129f0769323d86", writer
				.computeName().name());
	}

	private void writeVerifyPack4(final boolean thin) throws IOException {
		final LinkedList<ObjectId> interestings = new LinkedList<ObjectId>();
		interestings.add(ObjectId
				.fromString("82c6b885ff600be425b4ea96dee75dca255b69e7"));
		final LinkedList<ObjectId> uninterestings = new LinkedList<ObjectId>();
		uninterestings.add(ObjectId
				.fromString("c59759f143fb1fe21c197981df75a7ee00290799"));
		createVerifyOpenPack(interestings, uninterestings, thin, false);

		final ObjectId writtenObjects[] = new ObjectId[] {
				ObjectId.fromString("82c6b885ff600be425b4ea96dee75dca255b69e7"),
				ObjectId.fromString("aabf2ffaec9b497f0950352b3e582d73035c2035"),
				ObjectId.fromString("5b6e7c66c276e7610d4a73c70ec1a1f7c1003259") };
		assertEquals(writtenObjects.length, writer.getObjectsNumber());
		ObjectId expectedObjects[];
		if (thin) {
			expectedObjects = new ObjectId[4];
			System.arraycopy(writtenObjects, 0, expectedObjects, 0,
					writtenObjects.length);
			expectedObjects[3] = ObjectId
					.fromString("6ff87c4664981e4397625791c8ea3bbb5f2279a3");

		} else {
			expectedObjects = writtenObjects;
		}
		verifyObjectsOrder(expectedObjects);
		assertEquals("cded4b74176b4456afa456768b2b5aafb41c44fc", writer
				.computeName().name());
	}

	private void createVerifyOpenPack(final Collection<ObjectId> interestings,
			final Collection<ObjectId> uninterestings, final boolean thin,
			final boolean ignoreMissingUninteresting)
			throws MissingObjectException, IOException {
		writer.setThin(thin);
		writer.setIgnoreMissingUninteresting(ignoreMissingUninteresting);
		writer.preparePack(interestings, uninterestings);
		writer.writePack(cos);
		verifyOpenPack(thin);
	}

	private void createVerifyOpenPack(final Iterator<RevObject> objectSource)
			throws MissingObjectException, IOException {
		writer.preparePack(objectSource);
		writer.writePack(cos);
		verifyOpenPack(false);
	}

	private void verifyOpenPack(final boolean thin) throws IOException {
		if (thin) {
			final InputStream is = new ByteArrayInputStream(os.toByteArray());
			final IndexPack indexer = new IndexPack(db, is, packBase);
			try {
				indexer.index(new TextProgressMonitor());
				fail("indexer should grumble about missing object");
			} catch (IOException x) {
				// expected
			}
		}
		final InputStream is = new ByteArrayInputStream(os.toByteArray());
		final IndexPack indexer = new IndexPack(db, is, packBase);
		indexer.setKeepEmpty(true);
		indexer.setFixThin(thin);
		indexer.setIndexVersion(2);
		indexer.index(new TextProgressMonitor());
		pack = new PackFile(indexFile, packFile);
	}

	private void verifyObjectsOrder(final ObjectId objectsOrder[]) {
		final List<PackIndex.MutableEntry> entries = new ArrayList<PackIndex.MutableEntry>();

		for (MutableEntry me : pack) {
			entries.add(me.cloneEntry());
		}
		Collections.sort(entries, new Comparator<PackIndex.MutableEntry>() {
			public int compare(MutableEntry o1, MutableEntry o2) {
				return Long.signum(o1.getOffset() - o2.getOffset());
			}
		});

		int i = 0;
		for (MutableEntry me : entries) {
			assertEquals(objectsOrder[i++].toObjectId(), me.toObjectId());
		}
	}
#endif
    }
}
