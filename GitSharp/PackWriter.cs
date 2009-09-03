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
using System.Diagnostics;
using System.IO;
using GitSharp.Exceptions;
using GitSharp.RevWalk;
using GitSharp.Transport;
using GitSharp.Util;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp
{
    public class PackWriter
    {
        public const string COUNTING_OBJECTS_PROGRESS = "Counting objects";
        public const string SEARCHING_REUSE_PROGRESS = "Compressing objects";
        public const string WRITING_OBJECTS_PROGRESS = "Writing objects";
        public const bool DEFAULT_REUSE_DELTAS = true;
        public const bool DEFAULT_REUSE_OBJECTS = true;
        public const bool DEFAULT_DELTA_BASE_AS_OFFSET = false;
        public const int DEFAULT_MAX_DELTA_DEPTH = 50;
        private const int PACK_VERSION_GENERATED = 2;

        class ObjectToPack : PackedObjectInfo
        {
        	private PackedObjectLoader reuseLoader;
            private int flags;

            public ObjectToPack(AnyObjectId src, int type) : base(src)
            {
                flags |= type << 1;
            }

        	public ObjectId DeltaBaseId { get; set; }

        	public ObjectToPack DeltaBase
            {
                get
                {
                    if (DeltaBaseId is ObjectToPack) return (ObjectToPack) DeltaBaseId;
                    return null;
                }
            }

            public void clearDeltaBase()
            {
                DeltaBaseId = null;
            }

            public bool IsDeltaRepresentation
            {
                get
                {
                    return DeltaBaseId != null;
                }
            }

            public bool IsWritten
            {
                get
                {
                    return Offset != 0;
                }
            }

            public PackedObjectLoader useLoader()
            {
                PackedObjectLoader r = reuseLoader;
                reuseLoader = null;
                return r;
            }

            public bool HasReuseLoader
            {
                get
                {
                    return reuseLoader != null;
                }
            }

            public void setReuseLoader(PackedObjectLoader reuseLoader)
            {
                this.reuseLoader = reuseLoader;
            }

            public void disposeLoader()
            {
                reuseLoader = null;
            }

            public int Type
            {
                get
                {
                    return (flags >> 1) & 0x7; 
                }
            }

            public int DeltaDepth
            {
                get
                {
                    return (int) (((uint) flags) >> 4);
                }
            }
            
			/*
            public void updateDeltaDepth()
            {
                int d;
                if (DeltaBaseId is ObjectToPack)
                    d = ((ObjectToPack)DeltaBaseId).DeltaDepth + 1;
                else d = DeltaBaseId != null ? 1 : 0;
                flags = (d << 4) | flags & 0x15;
            }
			*/

            public bool WantWrite
            {
                get
                {
                    return (flags & 1) == 1;
                }
            }

            public void markWantWrite()
            {
                flags |= 1;
            }
        }

        private readonly List<ObjectToPack>[] objectsLists = createObjectsLists();

        private static List<ObjectToPack>[] createObjectsLists()
        {
            List<ObjectToPack>[] ret = new List<ObjectToPack>[Constants.OBJ_TAG + 1];
            ret[0] = new List<ObjectToPack>();
            ret[Constants.OBJ_COMMIT] = new List<ObjectToPack>();
            ret[Constants.OBJ_TREE] = new List<ObjectToPack>();
            ret[Constants.OBJ_BLOB] = new List<ObjectToPack>();
            ret[Constants.OBJ_TAG] = new List<ObjectToPack>();
            return ret;
        }

        private readonly ObjectIdSubclassMap<ObjectToPack> objectsMap = new ObjectIdSubclassMap<ObjectToPack>();
        private readonly ObjectIdSubclassMap<ObjectId> edgeObjects = new ObjectIdSubclassMap<ObjectId>();
        private readonly Repository db;
        private PackOutputStream pos;
        private readonly Deflater deflater;
        private readonly IProgressMonitor initMonitor;
        private readonly IProgressMonitor writeMonitor;
        private readonly byte[] buf = new byte[16384]; // 16 KB
        private readonly WindowCursor windowCursor = new WindowCursor();
        private List<ObjectToPack> sortedByName;
        private byte[] packcsum;
    	private int outputVersion;

    	public PackWriter(Repository repo, IProgressMonitor monitor)
            : this(repo, monitor, monitor)
        {
        }

        public PackWriter(Repository repo, IProgressMonitor imonitor, IProgressMonitor wmonitor)
        {
        	IgnoreMissingUninteresting = true;
        	MaxDeltaDepth = DEFAULT_MAX_DELTA_DEPTH;
        	DeltaBaseAsOffset = DEFAULT_DELTA_BASE_AS_OFFSET;
        	ReuseObjects = DEFAULT_REUSE_OBJECTS;
        	ReuseDeltas = DEFAULT_REUSE_DELTAS;
        	db = repo;
            initMonitor = imonitor;
            writeMonitor = wmonitor;
            deflater = new Deflater(db.Config.getCore().getCompression());
            outputVersion = repo.Config.getCore().getPackIndexVersion();
        }

    	public bool ReuseDeltas { get; set; }
    	public bool ReuseObjects { get; set; }
    	public bool DeltaBaseAsOffset { get; set; }
    	public int MaxDeltaDepth { get; set; }
    	public bool Thin { get; set; }
    	public bool IgnoreMissingUninteresting { get; set; }

    	public void setIndexVersion(int version)
        {
            outputVersion = version;
        }

        public int getObjectsNumber()
        {
            return objectsMap.size();
        }

        public void preparePack(IEnumerable<RevObject> objectsSource)
        {
			foreach(RevObject obj in objectsSource)
			{
				addObject(obj);
			}
        }

        public void preparePack<T>(IEnumerable<T> interestingObjects, IEnumerable<T> uninterestingObjects) 
			where T : ObjectId
        {
            ObjectWalk walker = setUpWalker(interestingObjects, uninterestingObjects);
            findObjectsToPack(walker);
        }

        public bool willInclude(AnyObjectId id)
        {
            return objectsMap.get(id) != null;
        }

        public ObjectId computeName()
        {
            MessageDigest md = Constants.newMessageDigest();
            foreach (ObjectToPack otp in sortByName())
            {
                otp.copyRawTo(buf, 0);
                md.Update(buf, 0, Constants.OBJECT_ID_LENGTH);
            }
            return ObjectId.FromRaw(md.Digest());
        }

        public void writeIndex(Stream indexStream)
        {
            List<ObjectToPack> list = sortByName();

			PackIndexWriter iw = outputVersion <= 0 ? 
				PackIndexWriter.CreateOldestPossible(indexStream, list) : 
				PackIndexWriter.CreateVersion(indexStream, outputVersion);

            iw.Write(list, packcsum);
        }

        private List<ObjectToPack> sortByName()
        {
            if (sortedByName == null)
            {
                sortedByName = new List<ObjectToPack>(objectsMap.size());

                foreach (List<ObjectToPack> list in objectsLists)
                {
                    foreach (ObjectToPack otp in list)
                    {
                        sortedByName.Add(otp);
                    }
                }

                sortedByName.Sort();
            }

            return sortedByName;
        }

        public void writePack(Stream packStream)
        {
            if (ReuseDeltas || ReuseObjects)
            {
            	searchForReuse();
            }

            if (!(packStream is BufferedStream))
            {
            	packStream = new BufferedStream(packStream);
            }

            pos = new PackOutputStream(packStream);

            writeMonitor.BeginTask(WRITING_OBJECTS_PROGRESS, getObjectsNumber());
            writeHeader();
            writeObjects();
            writeChecksum();

            pos.Flush();
            windowCursor.release();
            writeMonitor.EndTask();
        }

        private void searchForReuse()
        {
            initMonitor.BeginTask(SEARCHING_REUSE_PROGRESS, getObjectsNumber());
            List<PackedObjectLoader> reuseLoaders = new List<PackedObjectLoader>();
            foreach (List<ObjectToPack> list in objectsLists)
            {
                foreach (ObjectToPack otp in list)
                {
                    if (initMonitor.IsCancelled)
                        throw new IOException("Packing cancelled during objects writing.");
                    reuseLoaders.Clear();
                    searchForReuse(reuseLoaders, otp);
                    initMonitor.Update(1);
                }
            }

            initMonitor.EndTask();
        }

        private void searchForReuse(List<PackedObjectLoader> reuseLoaders, ObjectToPack otp)
        {
            db.openObjectInAllPacks(otp, reuseLoaders, windowCursor);

            if (ReuseDeltas)
            {
            	selectDeltaReuseForObject(otp, reuseLoaders);
            }

            if (ReuseObjects && !otp.HasReuseLoader)
            {
            	SelectObjectReuseForObject(otp, reuseLoaders);
            }
        }

        private void selectDeltaReuseForObject(ObjectToPack otp, IEnumerable<PackedObjectLoader> loaders)
        {
            PackedObjectLoader bestLoader = null;
            ObjectId bestBase = null;

            foreach (PackedObjectLoader loader in loaders)
            {
                ObjectId idBase = loader.getDeltaBase();
                if (idBase == null) continue;
                ObjectToPack otpBase = objectsMap.get(idBase);

                if ((otpBase != null || (Thin && edgeObjects.get(idBase) != null)) && isBetterDeltaReuseLoader(bestLoader, loader))
                {
                    bestLoader = loader;
                    bestBase = (otpBase ?? idBase);
                }
            }

        	if (bestLoader == null) return;

        	otp.setReuseLoader(bestLoader);
        	otp.DeltaBaseId = bestBase;
        }

        private static bool isBetterDeltaReuseLoader(PackedObjectLoader currentLoader, PackedObjectLoader loader)
        {
            if (currentLoader == null) return true;

            if (loader.getRawSize() < currentLoader.getRawSize()) return true;

            return loader.getRawSize() == currentLoader.getRawSize() && 
				loader.supportsFastCopyRawData() &&
                !currentLoader.supportsFastCopyRawData();
        }

        private static void SelectObjectReuseForObject(ObjectToPack otp, IEnumerable<PackedObjectLoader> loaders)
        {
            foreach (PackedObjectLoader loader in loaders)
            {
            	if (!(loader is WholePackedObjectLoader)) continue;

            	otp.setReuseLoader(loader);
            	return;
            }
        }

        private void writeHeader()
        {
            Constants.PACK_SIGNATURE.CopyTo(buf, 0);
            NB.encodeInt32(buf, 4, PACK_VERSION_GENERATED);
            NB.encodeInt32(buf, 8, getObjectsNumber());
            pos.Write(buf, 0, 12);
        }

        private void writeObjects()
        {
            foreach (List<ObjectToPack> list in objectsLists)
            {
                foreach (ObjectToPack otp in list)
                {
                    if (writeMonitor.IsCancelled)
                    {
                    	throw new IOException("Packing cancelled during objects writing");
                    }

                    if (!otp.IsWritten)
                    {
                    	writeObject(otp);
                    }
                }
            }
        }

        private void writeObject(ObjectToPack otp)
        {
            otp.markWantWrite();
            if (otp.IsDeltaRepresentation)
            {
                ObjectToPack deltaBase = otp.DeltaBase;
                Debug.Assert(deltaBase != null || Thin);
                if (deltaBase != null && !deltaBase.IsWritten)
                {
                    if (deltaBase.WantWrite)
                    {
                        otp.clearDeltaBase();
                        otp.disposeLoader();
                    }
                    else
                    {
                        writeObject(deltaBase);
                    }
                }
            }

            Debug.Assert(!otp.IsWritten);

            pos.resetCRC32();
            otp.Offset = pos.Length;

            PackedObjectLoader reuse = open(otp);
            if (reuse != null)
            {
                try
                {
                    if (otp.IsDeltaRepresentation)
                    {
                        writeDeltaObjectReuse(otp, reuse);
                    }
                    else
                    {
                        writeObjectHeader(otp.Type, reuse.getSize());
                        reuse.copyRawData(pos, buf, windowCursor);
                    }
                }
                finally
                {
                    reuse.endCopyRawData();
                }
            }
            else if (otp.IsDeltaRepresentation)
            {
                throw new IOException("creating deltas is not implemented");
            }
            else
            {
                writeWholeObjectDeflate(otp);
            }
            otp.CRC = pos.getCRC32();
            writeMonitor.Update(1);
        }

        private PackedObjectLoader open(ObjectToPack otp)
        {
            while(true)
            {
                PackedObjectLoader reuse = otp.useLoader();
                if (reuse == null)
                    return null;

                try
                {
                    reuse.beginCopyRawData();
                    return reuse;
                }
                catch (IOException)
                {
                    otp.clearDeltaBase();
                    searchForReuse(new List<PackedObjectLoader>(), otp);
                    continue;
                }
            }
        }

        private void writeWholeObjectDeflate(ObjectToPack otp)
        {
            ObjectLoader loader = db.openObject(windowCursor, otp);
            byte[] data = loader.getCachedBytes();
            writeObjectHeader(otp.Type, data.Length);
            deflater.Reset();
            deflater.SetInput(data, 0, data.Length);
            deflater.Finish();
            do
            {
                int n = deflater.Deflate(buf, 0, buf.Length);
                if (n > 0)
                    pos.Write(buf, 0, n);
            } while (!deflater.IsFinished);
        }

        private void writeDeltaObjectReuse(ObjectToPack otp, PackedObjectLoader reuse)
        {
            if (DeltaBaseAsOffset && otp.DeltaBase != null)
            {
                writeObjectHeader(Constants.OBJ_OFS_DELTA, reuse.getRawSize());

                ObjectToPack deltaBase = otp.DeltaBase;
                long offsetDiff = otp.Offset - deltaBase.Offset;
                int local_pos = buf.Length - 1;
                buf[local_pos] = (byte) (offsetDiff & 0x7F);
                while ((offsetDiff >>= 7) > 0)
                {
                    buf[--local_pos] = (byte) (0x80 | (--offsetDiff & 0x7F));
                }

                this.pos.Write(buf, local_pos, buf.Length - local_pos);
            }
            else
            {
                writeObjectHeader(Constants.OBJ_REF_DELTA, reuse.getRawSize());
                otp.DeltaBaseId.copyRawTo(buf, 0);
                this.pos.Write(buf, 0, Constants.OBJECT_ID_LENGTH);
            }
            reuse.copyRawData(this.pos, buf, windowCursor);
        }

        private void writeObjectHeader(int objectType, long dataLength)
        {
            long nextLength = (long) (((ulong) dataLength) >> 4);
            int size = 0;
            buf[size++] = (byte) ((nextLength > 0 ? 0x80 : 0x00) | (objectType << 4) | (dataLength & 0x0F));
            dataLength = nextLength;
            while (dataLength > 0)
            {
                nextLength = (long) (((ulong) nextLength) >> 7);
                buf[size++] = (byte) ((nextLength > 0 ? 0x80 : 0x00) | (dataLength & 0x7F));
                dataLength = nextLength;
            }
            pos.Write(buf, 0, size);
        }

        private void writeChecksum()
        {
            packcsum = pos.getDigest();
            pos.Write(packcsum, 0, packcsum.Length);
        }

        private ObjectWalk setUpWalker<T>(IEnumerable<T> interestingObjects, IEnumerable<T> uninterestingObjects) where T : ObjectId
        {
            ObjectWalk walker = new ObjectWalk(db);
            walker.sort(RevSort.Strategy.TOPO);
            walker.sort(RevSort.Strategy.COMMIT_TIME_DESC, true);
            if (Thin)
                walker.sort(RevSort.Strategy.BOUNDARY, true);

            foreach (T id in interestingObjects)
            {
                RevObject o = walker.parseAny(id);
                walker.markStart(o);
            }

            if (uninterestingObjects != null)
            {
                foreach (T id in uninterestingObjects)
                {
                    RevObject o;
                    try
                    {
                        o = walker.parseAny(id);
                    }
                    catch (MissingObjectException x)
                    {
                        if (IgnoreMissingUninteresting)
                            continue;
                        throw x;
                    }
                    walker.markUninteresting(o);
                }
            }
            return walker;
        }

        private void findObjectsToPack(ObjectWalk walker)
        {
            // [caytchen] TODO: IProgressMonitor.UNKNOWN constant!
            initMonitor.BeginTask(COUNTING_OBJECTS_PROGRESS, -1);
            RevObject o;

            while ((o = walker.next()) != null)
            {
                addObject(o);
                o.dispose();
                initMonitor.Update(1);
            }
            while ((o = walker.nextObject()) != null)
            {
                addObject(o);
                o.dispose();
                initMonitor.Update(1);
            }
            initMonitor.EndTask();
        }

        public void addObject(RevObject robject)
        {
            if (robject.has(RevFlag.UNINTERESTING))
            {
                edgeObjects.add(robject);
                Thin = true;
                return;
            }

            ObjectToPack otp = new ObjectToPack(robject, robject.getType());
            try
            {
                objectsLists[robject.getType()].Add(otp);
            }
            catch (IndexOutOfRangeException)
            {
                throw new IncorrectObjectTypeException(robject, "COMMIT nor TREE nor BLOB nor TAG");
            }
            objectsMap.add(otp);
        }
    }
}