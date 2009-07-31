/*
 * Copyright (C) 2009, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2009, Google Inc.
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
    public class ConcurrentRepackTest : RepositoryTestCase
    {
#if false
	public void setUp() throws Exception {
		WindowCacheConfig windowCacheConfig = new WindowCacheConfig();
		windowCacheConfig.setPackedGitOpenFiles(1);
		WindowCache.reconfigure(windowCacheConfig);
		super.setUp();
	}

	protected void tearDown() throws Exception {
		super.tearDown();
		WindowCacheConfig windowCacheConfig = new WindowCacheConfig();
		WindowCache.reconfigure(windowCacheConfig);
	}

	public void testObjectInNewPack() throws IncorrectObjectTypeException,
			IOException {
		// Create a new object in a new pack, and test that it is present.
		//
		final Repository eden = createNewEmptyRepo();
		final RevObject o1 = writeBlob(eden, "o1");
		pack(eden, o1);
		assertEquals(o1.name(), parse(o1).name());
	}

	public void testObjectMovedToNewPack1()
			throws IncorrectObjectTypeException, IOException {
		// Create an object and pack it. Then remove that pack and put the
		// object into a different pack file, with some other object. We
		// still should be able to access the objects.
		//
		final Repository eden = createNewEmptyRepo();
		final RevObject o1 = writeBlob(eden, "o1");
		final File[] out1 = pack(eden, o1);
		assertEquals(o1.name(), parse(o1).name());

		final RevObject o2 = writeBlob(eden, "o2");
		pack(eden, o2, o1);

		// Force close, and then delete, the old pack.
		//
		whackCache();
		delete(out1);

		// Now here is the interesting thing. Will git figure the new
		// object exists in the new pack, and not the old one.
		//
		assertEquals(o2.name(), parse(o2).name());
		assertEquals(o1.name(), parse(o1).name());
	}

	public void testObjectMovedWithinPack()
			throws IncorrectObjectTypeException, IOException {
		// Create an object and pack it.
		//
		final Repository eden = createNewEmptyRepo();
		final RevObject o1 = writeBlob(eden, "o1");
		final File[] out1 = pack(eden, o1);
		assertEquals(o1.name(), parse(o1).name());

		// Force close the old pack.
		//
		whackCache();

		// Now overwrite the old pack in place. This method of creating a
		// different pack under the same file name is partially broken. We
		// should also have a different file name because the list of objects
		// within the pack has been modified.
		//
		final RevObject o2 = writeBlob(eden, "o2");
		final PackWriter pw = new PackWriter(eden, NullProgressMonitor.INSTANCE);
		pw.addObject(o2);
		pw.addObject(o1);
		write(out1, pw);

		// Try the old name, then the new name. The old name should cause the
		// pack to reload when it opens and the index and pack mismatch.
		//
		assertEquals(o1.name(), parse(o1).name());
		assertEquals(o2.name(), parse(o2).name());
	}

	public void testObjectMovedToNewPack2()
			throws IncorrectObjectTypeException, IOException {
		// Create an object and pack it. Then remove that pack and put the
		// object into a different pack file, with some other object. We
		// still should be able to access the objects.
		//
		final Repository eden = createNewEmptyRepo();
		final RevObject o1 = writeBlob(eden, "o1");
		final File[] out1 = pack(eden, o1);
		assertEquals(o1.name(), parse(o1).name());

		final ObjectLoader load1 = db.openBlob(o1);
		assertNotNull(load1);

		final RevObject o2 = writeBlob(eden, "o2");
		pack(eden, o2, o1);

		// Force close, and then delete, the old pack.
		//
		whackCache();
		delete(out1);

		// Now here is the interesting thing... can the loader we made
		// earlier still resolve the object, even though its underlying
		// pack is gone, but the object still exists.
		//
		final ObjectLoader load2 = db.openBlob(o1);
		assertNotNull(load2);
		assertNotSame(load1, load2);

		final byte[] data2 = load2.getCachedBytes();
		final byte[] data1 = load1.getCachedBytes();
		assertNotNull(data2);
		assertNotNull(data1);
		assertNotSame(data1, data2); // cache should be per-pack, not per object
		assertTrue(Arrays.equals(data1, data2));
		assertEquals(load2.getType(), load1.getType());
	}

	private static void whackCache() {
		final WindowCacheConfig config = new WindowCacheConfig();
		config.setPackedGitOpenFiles(1);
		WindowCache.reconfigure(config);
	}

	private RevObject parse(final AnyObjectId id)
			throws MissingObjectException, IOException {
		return new RevWalk(db).parseAny(id);
	}

	private File[] pack(final Repository src, final RevObject... list)
			throws IOException {
		final PackWriter pw = new PackWriter(src, NullProgressMonitor.INSTANCE);
		for (final RevObject o : list) {
			pw.addObject(o);
		}

		final ObjectId name = pw.computeName();
		final File packFile = fullPackFileName(name, ".pack");
		final File idxFile = fullPackFileName(name, ".idx");
		final File[] files = new File[] { packFile, idxFile };
		write(files, pw);
		return files;
	}

	private static void write(final File[] files, final PackWriter pw)
			throws IOException {
		final long begin = files[0].getParentFile().lastModified();
		FileOutputStream out;

		out = new FileOutputStream(files[0]);
		try {
			pw.writePack(out);
		} finally {
			out.close();
		}

		out = new FileOutputStream(files[1]);
		try {
			pw.writeIndex(out);
		} finally {
			out.close();
		}

		touch(begin, files[0].getParentFile());
	}

	private static void delete(final File[] list) {
		final long begin = list[0].getParentFile().lastModified();
		for (final File f : list) {
			f.delete();
			assertFalse(f + " was removed", f.exists());
		}
		touch(begin, list[0].getParentFile());
	}

	private static void touch(final long begin, final File dir) {
		while (begin >= dir.lastModified()) {
			try {
				Thread.sleep(25);
			} catch (InterruptedException ie) {
				//
			}
			dir.setLastModified(System.currentTimeMillis());
		}
	}

	private File fullPackFileName(final ObjectId name, final String suffix) {
		final File packdir = new File(db.getObjectsDirectory(), "pack");
		return new File(packdir, "pack-" + name.name() + suffix);
	}

	private RevObject writeBlob(final Repository repo, final String data)
			throws IOException {
		final RevWalk revWalk = new RevWalk(repo);
		final byte[] bytes = data.getBytes(Constants.CHARACTER_ENCODING);
		final ObjectWriter ow = new ObjectWriter(repo);
		final ObjectId id = ow.writeBlob(bytes);
		try {
			parse(id);
			fail("Object " + id.name() + " should not exist in test repository");
		} catch (MissingObjectException e) {
			// Ok
		}
		return revWalk.lookupBlob(id);
	}
#endif
    }
}
