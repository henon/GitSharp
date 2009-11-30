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

using System;
using System.IO;
using System.Linq;
using System.Threading;
using GitSharp.Core;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using NUnit.Framework;

namespace GitSharp.Core.Tests
{
    [TestFixture]
    public class ConcurrentRepackTest : RepositoryTestCase
    {
        public override void setUp()
        {
            var windowCacheConfig = new WindowCacheConfig { PackedGitOpenFiles = 1 };
            WindowCache.reconfigure(windowCacheConfig);
            base.setUp();
        }

        public override void tearDown()
        {
            base.tearDown();
            var windowCacheConfig = new WindowCacheConfig();
            WindowCache.reconfigure(windowCacheConfig);
        }

        [Test]
        public void testObjectInNewPack()
        {
            // Create a new object in a new pack, and test that it is present.
            //
            Core.Repository eden = createBareRepository();
            RevObject o1 = WriteBlob(eden, "o1");
            Pack(eden, o1);
            Assert.AreEqual(o1.Name, Parse(o1).Name);
        }

        [Test]
        public void testObjectMovedToNewPack1()
        {
            // Create an object and pack it. Then remove that pack and put the
            // object into a different pack file, with some other object. We
            // still should be able to access the objects.
            //
            Core.Repository eden = createBareRepository();
            RevObject o1 = WriteBlob(eden, "o1");
            FileInfo[] out1 = Pack(eden, o1);
            Assert.AreEqual(o1.Name, Parse(o1).Name);

            RevObject o2 = WriteBlob(eden, "o2");
            Pack(eden, o2, o1);

            // Force close, and then delete, the old pack.
            //
            WhackCache();
            Delete(out1);

            // Now here is the interesting thing. Will git figure the new
            // object exists in the new pack, and not the old one.
            //
            Assert.AreEqual(o2.Name, Parse(o2).Name);
            Assert.AreEqual(o1.Name, Parse(o1).Name);
        }

        [Test]
        public void testObjectMovedWithinPack()
        {
            // Create an object and pack it.
            //
            Core.Repository eden = createBareRepository();
            RevObject o1 = WriteBlob(eden, "o1");
            FileInfo[] out1 = Pack(eden, o1);
            Assert.AreEqual(o1.Name, Parse(o1).Name);

            // Force close the old pack.
            //
            WhackCache();

            // Now overwrite the old pack in place. This method of creating a
            // different pack under the same file name is partially broken. We
            // should also have a different file name because the list of objects
            // within the pack has been modified.
            //
            RevObject o2 = WriteBlob(eden, "o2");
            var pw = new PackWriter(eden, NullProgressMonitor.Instance);
            pw.addObject(o2);
            pw.addObject(o1);
            Write(out1, pw);

            // Try the old name, then the new name. The old name should cause the
            // pack to reload when it opens and the index and pack mismatch.
            //
            Assert.AreEqual(o1.Name, Parse(o1).Name);
            Assert.AreEqual(o2.Name, Parse(o2).Name);
        }

        [Test]
        public void testObjectMovedToNewPack2()
        {
            // Create an object and pack it. Then remove that pack and put the
            // object into a different pack file, with some other object. We
            // still should be able to access the objects.
            //
            Core.Repository eden = createBareRepository();
            RevObject o1 = WriteBlob(eden, "o1");
            FileInfo[] out1 = Pack(eden, o1);
            Assert.AreEqual(o1.Name, Parse(o1).Name);

            ObjectLoader load1 = db.OpenBlob(o1);
            Assert.IsNotNull(load1);

            RevObject o2 = WriteBlob(eden, "o2");
            Pack(eden, o2, o1);

            // Force close, and then delete, the old pack.
            //
            WhackCache();
            Delete(out1);

            // Now here is the interesting thing... can the loader we made
            // earlier still resolve the object, even though its underlying
            // pack is gone, but the object still exists.
            //
            ObjectLoader load2 = db.OpenBlob(o1);
            Assert.IsNotNull(load2);
            Assert.AreNotSame(load1, load2);

            byte[] data2 = load2.CachedBytes;
            byte[] data1 = load1.CachedBytes;
            Assert.IsNotNull(data2);
            Assert.IsNotNull(data1);
            Assert.AreNotSame(data1, data2); // cache should be per-pack, not per object
            Assert.IsTrue(data1.SequenceEqual(data2));
            Assert.AreEqual(load2.Type, load1.Type);
        }

        private static void WhackCache()
        {
            var config = new WindowCacheConfig { PackedGitOpenFiles = 1 };
            WindowCache.reconfigure(config);
        }

        private RevObject Parse(AnyObjectId id)
        {
            return new GitSharp.Core.RevWalk.RevWalk(db).parseAny(id);
        }

        private FileInfo[] Pack(Core.Repository src, params RevObject[] list)
        {
            var pw = new PackWriter(src, NullProgressMonitor.Instance);
            foreach (RevObject o in list)
            {
                pw.addObject(o);
            }

            ObjectId name = pw.computeName();
			FileInfo packFile = FullPackFileName(name);
            FileInfo idxFile = FullIndexFileName(name);
            var files = new[] { packFile, idxFile };
            Write(files, pw);
            return files;
        }

        private static void Write(FileInfo[] files, PackWriter pw)
        {
            FileInfo file = files[0];
            long begin = file.Directory.LastWriteTime.Ticks;

            using (var stream = file.Create())
            {

                    pw.writePack(stream);

            }

            file = files[1];
            using (var stream = file.Create())
            {

                    pw.writeIndex(stream);
   
            }

            Touch(begin, files[0].Directory);
        }

        private static void Delete(FileInfo[] list)
        {
            long begin = list[0].Directory.LastWriteTime.Ticks;
            foreach (var fi in list)
            {
                fi.Delete();
                Assert.IsFalse(File.Exists(fi.FullName), fi + " was not removed");
            }

            Touch(begin, list[0].Directory);
        }

        private static void Touch(long begin, FileSystemInfo dir)
        {
            while (begin >= dir.LastWriteTime.Ticks)
            {
                Thread.Sleep(25);
                dir.LastWriteTime = DateTime.Now;
            }
        }

        private FileInfo FullPackFileName(AnyObjectId name)
        {
            var packdir = Path.Combine(db.ObjectDatabase.getDirectory().FullName, "pack");
            return new FileInfo(Path.Combine(packdir, "pack-" + GitSharp.Core.Transport.IndexPack.GetPackFileName(name.Name)));
        }

        private FileInfo FullIndexFileName(AnyObjectId name)
        {
            var packdir = Path.Combine(db.ObjectDatabase.getDirectory().FullName, "pack");
            return new FileInfo(Path.Combine(packdir, "pack-" + GitSharp.Core.Transport.IndexPack.GetIndexFileName(name.Name)));
        }

        private RevObject WriteBlob(Core.Repository repo, string data)
        {
            var revWalk = new GitSharp.Core.RevWalk.RevWalk(repo);
            byte[] bytes = Constants.encode(data);
            var ow = new ObjectWriter(repo);
            ObjectId id = ow.WriteBlob(bytes);
            try
            {
                Parse(id);
                Assert.Fail("Object " + id.Name + " should not exist in test repository");
            }
            catch (MissingObjectException)
            {
                // Ok
            }

            return revWalk.lookupBlob(id);
        }
    }
}