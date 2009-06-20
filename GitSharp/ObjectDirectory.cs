/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Linq;
using System.Text;
using GitSharp.Util;
using System.IO;
using GitSharp.Exceptions;

namespace GitSharp
{
    /**
     * Traditional file system based {@link ObjectDatabase}.
     * <p>
     * This is the classical object database representation for a Git repository,
     * where objects are stored loose by hashing them into directories by their
     * {@link ObjectId}, or are stored in compressed containers known as
     * {@link PackFile}s.
     */
    public class ObjectDirectory : ObjectDatabase
    {
        private static PackFile[] NO_PACKS = { };

        private DirectoryInfo objects;

        private DirectoryInfo infoDirectory;

        private DirectoryInfo packDirectory;

        private FileInfo alternatesFile;

        private AtomicReference<PackFile[]> packList;

        private long packDirectoryLastModified;

        /**
         * Initialize a reference to an on-disk object directory.
         *
         * @param dir
         *            the location of the <code>objects</code> directory.
         */
        public ObjectDirectory(DirectoryInfo dir)
        {
            objects = dir;
            infoDirectory = new DirectoryInfo(objects.FullName + "/info");
            packDirectory = new DirectoryInfo(objects.FullName + "/pack");
            alternatesFile = new FileInfo(infoDirectory + "/alternates");

            packList = new AtomicReference<PackFile[]>();
        }

        /**
         * @return the location of the <code>objects</code> directory.
         */
        public DirectoryInfo getDirectory()
        {
            return objects;
        }


        public override bool exists()
        {
            return objects.Exists;
        }


        public override void create()
        {
            objects.Create();
            infoDirectory.Create();
            packDirectory.Create();
        }

        public override void closeSelf()
        {
            PackFile[] packs = packList.get();
            if (packs != null)
            {
                packList.set(null);
                foreach (PackFile p in packs)
                {
                    p.Close();
                }
            }
        }

        /**
         * Compute the location of a loose object file.
         *
         * @param objectId
         *            identity of the loose object to map to the directory.
         * @return location of the object, if it were to exist as a loose object.
         */
        public FileInfo fileFor(AnyObjectId objectId)
        {
            return fileFor(objectId.ToString());
        }

        private FileInfo fileFor(string objectName)
        {
            string d = objectName.Slice(0, 2);
            string f = objectName.Substring(2);
            return new FileInfo(objects.FullName + "/" + d + "/" + f);
        }

        /**
         * Add a single existing pack to the list of available pack files.
         *
         * @param pack
         *            path of the pack file to open.
         * @param idx
         *            path of the corresponding index file.
         * @
         *             index file could not be opened, read, or is not recognized as
         *             a Git pack file index.
         */
        public void openPack(FileInfo pack, FileInfo idx)
        {
            string p = pack.Name;
            string i = idx.Name;

            if (p.Length != 50 || !p.StartsWith("pack-") || !p.EndsWith(".pack"))
                throw new IOException("Not a valid pack " + pack);

            if (i.Length != 49 || !i.StartsWith("pack-") || !i.EndsWith(".idx"))
                throw new IOException("Not a valid pack " + idx);

            if (!p.Slice(0, 45).Equals(i.Slice(0, 45)))
                throw new IOException("Pack " + pack + "does not match index");

            insertPack(new PackFile(idx, pack));
        }


        public override string ToString()
        {
            return "ObjectDirectory[" + getDirectory() + "]";
        }


        public override bool hasObject1(AnyObjectId objectId)
        {
            foreach (PackFile p in packs())
            {
                try
                {
                    if (p.HasObject(objectId))
                    {
                        return true;
                    }
                }
                catch (IOException e)
                {
                    // The hasObject call should have only touched the index,
                    // so any failure here indicates the index is unreadable
                    // by this process, and the pack is likewise not readable.
                    //
                    removePack(p);
                    continue;
                }
            }
            return false;
        }


        public override ObjectLoader openObject1(WindowCursor curs, AnyObjectId objectId)
        {
            PackFile[] pList = packs();
            for (; ; )
            {
            SEARCH:
                foreach (PackFile p in pList)
                {
                    try
                    {
                        PackedObjectLoader ldr = p.Get(curs, objectId);
                        if (ldr != null)
                        {
                            ldr.materialize(curs);
                            return ldr;
                        }
                    }
                    catch (PackMismatchException e)
                    {
                        // Pack was modified; refresh the entire pack list.
                        //
                        pList = scanPacks(pList);
                        goto SEARCH;
                    }
                    catch (IOException e)
                    {
                        // Assume the pack is corrupted.
                        //
                        removePack(p);
                    }
                }
                return null;
            }
        }


        public override void openObjectInAllPacks1(List<PackedObjectLoader> @out, WindowCursor curs, AnyObjectId objectId)
        {
            PackFile[] pList = packs();
            for (; ; )
            {
            SEARCH:
                foreach (PackFile p in pList)
                {
                    try
                    {
                        PackedObjectLoader ldr = p.Get(curs, objectId);
                        if (ldr != null)
                        {
                            @out.Add(ldr);
                        }
                    }
                    catch (PackMismatchException e)
                    {
                        // Pack was modified; refresh the entire pack list.
                        //
                        pList = scanPacks(pList);
                        goto SEARCH;
                    }
                    catch (IOException e)
                    {
                        // Assume the pack is corrupted.
                        //
                        removePack(p);
                    }
                }
                break;
            }
        }


        public override bool hasObject2(string objectName)
        {
            return fileFor(objectName).Exists;
        }


        public override ObjectLoader openObject2(WindowCursor curs,
                 string objectName, AnyObjectId objectId)
        {
            try
            {
                return new UnpackedObjectLoader(fileFor(objectName), objectId);
            }
            catch (FileNotFoundException noFile)
            {
                return null;
            }
        }


        public override bool tryAgain1()
        {
            PackFile[] old = packList.get();
            if (packDirectoryLastModified < packDirectory.LastAccessTime.Ticks)
            {
                scanPacks(old);
                return true;
            }
            return false;
        }

        private void insertPack(PackFile pf)
        {
            PackFile[] o, n;
            do
            {
                o = packs();
                n = new PackFile[1 + o.Length];
                n[0] = pf;
                Array.Copy(o, 0, n, 1, o.Length);
            } while (!packList.compareAndSet(o, n));
        }

        private void removePack(PackFile deadPack)
        {
            PackFile[] o, n;
            do
            {
                o = packList.get();
                if (o == null || !inList(o, deadPack))
                {
                    break;

                }
                else if (o.Length == 1)
                {
                    n = NO_PACKS;

                }
                else
                {
                    n = new PackFile[o.Length - 1];
                    int j = 0;
                    foreach (PackFile p in o)
                    {
                        if (p != deadPack)
                        {
                            n[j++] = p;
                        }
                    }
                }
            } while (!packList.compareAndSet(o, n));
            deadPack.Close();
        }

        private static bool inList(PackFile[] list, PackFile pack)
        {
            foreach (PackFile p in list)
            {
                if (p == pack)
                {
                    return true;
                }
            }
            return false;
        }

        private PackFile[] packs()
        {
            PackFile[] r = packList.get();
            if (r == null)
                r = scanPacks(null);
            return r;
        }

        private PackFile[] scanPacks(PackFile[] original)
        {
            lock (packList)
            {
                PackFile[] o, n;
                do
                {
                    o = packList.get();
                    if (o != original)
                    {
                        // Another thread did the scan for us, while we
                        // were blocked on the monitor above.
                        //
                        return o;
                    }
                    n = scanPacksImpl(o != null ? o : NO_PACKS);
                } while (!packList.compareAndSet(o, n));
                return n;
            }
        }

        private PackFile[] scanPacksImpl(PackFile[] old)
        {
            Dictionary<string, PackFile> forReuse = reuseMap(old);
            string[] idxList = listPackIdx();
            List<PackFile> list = new List<PackFile>(idxList.Length);
            foreach (string indexName in idxList)
            {
                string @base = indexName.Slice(0, indexName.Length - 4);
                string packName = @base + ".pack";

                PackFile oldPack = forReuse[packName];
                    forReuse.Remove(packName);
                if (oldPack != null)
                {
                    list.Add(oldPack);
                    continue;
                }

                var packFile = new FileInfo(packDirectory.FullName + "/" + packName);
                if (!packFile.Exists)
                {
                    // Sometimes C Git's HTTP fetch transport leaves a
                    // .idx file behind and does not download the .pack.
                    // We have to skip over such useless indexes.
                    //
                    continue;
                }

                var idxFile = new FileInfo(packDirectory + "/" + indexName);
                list.Add(new PackFile(idxFile, packFile));
            }

            foreach (PackFile p in forReuse.Values)
            {
                p.Close();
            }

            if (list.Count == 0)
            {
                return NO_PACKS;
            }
            PackFile[] r = list.ToArray();
            Array.Sort(r, PackFile.SORT);
            return r;
        }

        private static Dictionary<string, PackFile> reuseMap(PackFile[] old)
        {
            Dictionary<string, PackFile> forReuse = new Dictionary<string, PackFile>();
            foreach (PackFile p in old)
            {
                if (p.IsInvalid)
                {
                    // The pack instance is corrupted, and cannot be safely used
                    // again. Do not include it in our reuse map.
                    //
                    p.Close();
                    continue;
                }

                PackFile prior = forReuse[p.File.Name] = p;
                if (prior != null)
                {
                    // This should never occur. It should be impossible for us
                    // to have two pack files with the same name, as all of them
                    // came out of the same directory. If it does, we promised to
                    // close any PackFiles we did not reuse, so close the one we
                    // just evicted out of the reuse map.
                    //
                    prior.Close();
                }
            }
            return forReuse;
        }

        private string[] listPackIdx()
        {
            packDirectoryLastModified = packDirectory.LastAccessTime.Ticks;
            throw new NotImplementedException();
#if false
		 string[] idxList = packDirectory.list(new FilenameFilter() {
			public bool accept( DirectoryInfo baseDir,  string n) {
				// Must match "pack-[0-9a-f]{40}.idx" to be an index.
				return n.Length == 49 && n.EndsWith(".idx")
						&& n.StartsWith("pack-");
			}
		});
            return idxList != null ? idxList : "";
#endif
        }


        public override ObjectDatabase[] loadAlternates()
        {
            BufferedReader br = open(alternatesFile);
            List<ObjectDirectory> l = new List<ObjectDirectory>(4);
            try
            {
                string line;
                while ((line = br.ReadLine()) != null)
                {
                    l.Add(new ObjectDirectory(FS.resolve(objects, line)));
                }
            }
            finally
            {
                br.Close();
            }

            if (l.Count == 0)
            {
                return NO_ALTERNATES;
            }
            return l.ToArray();
        }

        private static BufferedReader open(FileInfo f)
        {
            return new BufferedReader(new FileStream(f.FullName, System.IO.FileMode.Open));
        }
    }

}
