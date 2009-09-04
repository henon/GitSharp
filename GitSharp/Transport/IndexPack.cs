/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using GitSharp.Util;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp.Transport
{

    public class IndexPack
    {
        public const string PROGRESS_DOWNLOAD = "Receiving objects";
        public const string PROGRESS_RESOLVE_DELTA = "Resolving deltas";

        public const int BUFFER_SIZE = 8192;

        private static FileInfo createTempFile(string pre, string suf, DirectoryInfo dir)
        {
            Random r = new Random();
            int randsuf = r.Next(100000, 999999);
            string p = Path.Combine(dir.ToString(), pre + randsuf + suf);
            File.Create(p).Close();
            return new FileInfo(p);
        }

        public static IndexPack create(Repository db, Stream stream)
        {
            string suffix = ".pack";
            DirectoryInfo objdir = db.ObjectsDirectory;
            FileInfo tmp = createTempFile("incoming_", suffix, objdir);
            string n = tmp.Name;
            FileInfo basef;

            basef = new FileInfo(Path.Combine(objdir.ToString(), n.Slice(0, n.Length - suffix.Length)));
            IndexPack ip = new IndexPack(db, stream, basef);
            ip.setIndexVersion(db.Config.getCore().getPackIndexVersion());
            return ip;
        }

        private class DeltaChain : ObjectId
        {
            public UnresolvedDelta head;

            public DeltaChain(AnyObjectId id)
                : base(id)
            {
                
            }

            public UnresolvedDelta remove()
            {
                UnresolvedDelta r = head;
                if (r != null)
                    head = null;
                return r;
            }

            public void add(UnresolvedDelta d)
            {
                d.next = head;
                head = d;
            }
        }

        private class UnresolvedDelta
        {
            public long position;
            public int crc;
            public UnresolvedDelta next;

            public UnresolvedDelta(long headerOffset, int crc32)
            {
                position = headerOffset;
                crc = crc32;
            }
        }

        private readonly Repository repo;
        private Inflater inflater;
        private readonly MessageDigest objectDigest;
        private readonly MutableObjectId tempObjectId;
        private Stream stream;
        private byte[] buf;
        private long bBase;
        private int bOffset;
        private int bAvail;
        private ObjectChecker objCheck;
        private bool fixThin;
        private bool keepEmpty;
        private int outputVersion;
        private readonly FileInfo dstPack;
        private readonly FileInfo dstIdx;
        private long objectCount;
        private PackedObjectInfo[] entries;
        private int deltaCount;
        private int entryCount;
        private readonly Crc32 crc = new Crc32();
        private ObjectIdSubclassMap<DeltaChain> baseById;
        private Dictionary<long, UnresolvedDelta> baseByPos;
        private byte[] objectData;
        private MessageDigest packDigest;
        private FileStream packOut;
        private byte[] packcsum;

        private long originalEOF;
        private WindowCursor readCurs;

        public IndexPack(Repository db, Stream src, FileInfo dstBase)
        {
            repo = db;
            stream = src;
            inflater = InflaterCache.Instance.get();
            readCurs = new WindowCursor();
            buf = new byte[BUFFER_SIZE];
            objectData = new byte[BUFFER_SIZE];
            objectDigest = Constants.newMessageDigest();
            tempObjectId = new MutableObjectId();
            packDigest = Constants.newMessageDigest();

            if (dstBase != null)
            {
                DirectoryInfo dir = dstBase.Directory;
                string nam = dstBase.Name;
                dstPack = new FileInfo(Path.Combine(dir.ToString(), nam + ".pack"));
                dstIdx = new FileInfo(Path.Combine(dir.ToString(), nam + ".idx"));
                packOut = dstPack.Create();
            }
            else
            {
                dstPack = null;
                dstIdx = null;
            }
        }

        public void setIndexVersion(int version)
        {
            outputVersion = version; 
        }

        public void setFixThin(bool fix)
        {
            fixThin = fix;
        }

        public void setKeepEmpty(bool empty)
        {
            keepEmpty = empty;
        }

        public void setObjectChecker(ObjectChecker oc)
        {
            objCheck = oc;
        }

        public void setObjectChecking(bool on)
        {
            setObjectChecker(on ? new ObjectChecker() : null);
        }

        public void index(IProgressMonitor progress)
        {
            progress.Start(2);
            try
            {
                try
                {
                    readPackHeader();

                    entries = new PackedObjectInfo[(int)objectCount];
                    baseById = new ObjectIdSubclassMap<DeltaChain>();
                    baseByPos = new Dictionary<long, UnresolvedDelta>();

                    progress.BeginTask(PROGRESS_DOWNLOAD, (int) objectCount);
                    for (int done = 0; done < objectCount; done++)
                    {
                        indexOneObject();
                        progress.Update(1);
                        if (progress.IsCancelled)
                            throw new IOException("Download cancelled");
                    }
                    readPackFooter();
                    endInput();
                    progress.EndTask();

                    if (deltaCount > 0)
                    {
                        if (packOut == null)
                            throw new IOException("need packOut");
                        resolveDeltas(progress);
                        if (entryCount < objectCount)
                        {
                            if (!fixThin)
                            {
                                throw new IOException("pack has " + (objectCount - entryCount) + " unresolved deltas");
                            }
                            fixThinPack(progress);
                        }
                    }

                    if (packOut != null && (keepEmpty || entryCount > 0))
                        packOut.Flush();

                    packDigest = null;
                    baseById = null;
                    baseByPos = null;

                    if (dstIdx != null && (keepEmpty || entryCount > 0))
                        writeIdx();
                }
                finally
                {
                    try
                    {
                        InflaterCache.Instance.release(inflater);
                    }
                    finally
                    {
                        inflater = null;
                    }
                    readCurs = WindowCursor.release(readCurs);

                    progress.EndTask();
                    if (packOut != null)
                        packOut.Close();
                }

                if (keepEmpty || entryCount > 0)
                {
                    if (dstPack != null)
                        dstPack.IsReadOnly = true;
                    if (dstIdx != null)
                        dstIdx.IsReadOnly = true;
                }
            }
            catch (IOException)
            {
                if (dstPack != null) dstPack.Delete();
                if (dstIdx != null) dstIdx.Delete();
                throw;
            }
        }

        private void resolveDeltas(IProgressMonitor progress)
        {
            progress.BeginTask(PROGRESS_RESOLVE_DELTA, deltaCount);
            int last = entryCount;
            for (int i = 0; i < last; i++)
            {
                int before = entryCount;
                resolveDeltas(entries[i]);
                progress.Update(entryCount - before);
                if (progress.IsCancelled)
                    throw new IOException("Download cancelled during indexing");
            }
            progress.EndTask();
        }

        private void resolveDeltas(PackedObjectInfo oe)
        {
            int oldCRC = oe.CRC;
            if (baseById.get(oe) != null || baseByPos.ContainsKey(oe.Offset))
            {
                resolveDeltas(oe.Offset, oldCRC, Constants.OBJ_BAD, null, oe);
            }
        }

        private void resolveDeltas(long pos, int oldCRC, int type, byte[] data, PackedObjectInfo oe)
        {
            crc.Reset();
            position(pos);
            int c = readFromFile();
            int typecode = (c >> 4) & 7;
            long sz = c & 15;
            int shift = 4;
            while ((c & 0x80) != 0)
            {
                c = readFromFile();
                sz += (c & 0x7f) << shift;
                shift += 7;
            }

            switch (typecode)
            {
                case Constants.OBJ_COMMIT:
                case Constants.OBJ_TREE:
                case Constants.OBJ_BLOB:
                case Constants.OBJ_TAG:
                    type = typecode;
                    data = inflateFromFile((int) sz);
                    break;
                case Constants.OBJ_OFS_DELTA:
                    {
                        c = readFromFile() & 0xff;
                        while ((c & 128) != 0)
                            c = readFromFile() & 0xff;
                        data = BinaryDelta.Apply(data, inflateFromFile((int) sz));
                        break;
                    }
                case Constants.OBJ_REF_DELTA:
                    {
                        crc.Update(buf, fillFromFile(20), 20);
                        use(20);
                        data = BinaryDelta.Apply(data, inflateFromFile((int) sz));
                        break;
                    }
                    default:
                    throw new IOException("Unknown object type " + typecode + ".");
            }

            int crc32 = (int) crc.Value;
            if (oldCRC != crc32)
                throw new IOException("Corruption detected re-reading at " + pos);
            if (oe == null)
            {
                objectDigest.Update(Constants.encodedTypeString(type));
                objectDigest.Update((byte)' ');
                objectDigest.Update(Constants.encodeASCII(data.Length));
                objectDigest.Update((byte)0);
                objectDigest.Update(data);
                tempObjectId.FromRaw(objectDigest.Digest(), 0);

                verifySafeObject(tempObjectId, type, data);
                oe = new PackedObjectInfo(pos, crc32, tempObjectId);
                entries[entryCount++] = oe;
            }

            resolveChildDeltas(pos, type, data, oe);
        }

        private UnresolvedDelta removeBaseById(AnyObjectId id)
        {
            DeltaChain d = baseById.get(id);
            return d != null ? d.remove() : null;
        }

        private static UnresolvedDelta reverse(UnresolvedDelta c)
        {
            UnresolvedDelta tail = null;
            while (c != null)
            {
                UnresolvedDelta n = c.next;
                c.next = tail;
                tail = c;
                c = n;
            }
            return tail;
        }

        private void resolveChildDeltas(long pos, int type, byte[] data, PackedObjectInfo oe)
        {
            UnresolvedDelta a = reverse(removeBaseById(oe));
            UnresolvedDelta b = reverse(baseByPos[pos]);
            baseByPos.Remove(pos);
            while (a != null && b != null)
            {
                if (a.position < b.position)
                {
                    resolveDeltas(a.position, a.crc, type, data, null);
                    a = a.next;
                }
                else
                {
                    resolveDeltas(b.position, b.crc, type, data, null);
                    b = b.next;
                }
            }
            resolveChildDeltaChain(type, data, a);
            resolveChildDeltaChain(type, data, b);
        }

        private void resolveChildDeltaChain(int type, byte[] data, UnresolvedDelta a)
        {
            while (a != null)
            {
                resolveDeltas(a.position, a.crc, type, data, null);
                a = a.next;
            }
        }

        private void fixThinPack(IProgressMonitor progress)
        {
            growEntries();

            packDigest.Reset();
            originalEOF = packOut.Length - 20;
            Deflater def = new Deflater(Deflater.DEFAULT_COMPRESSION, false);
            List<DeltaChain> missing = new List<DeltaChain>(64);
            long end = originalEOF;
            
			foreach(DeltaChain baseId in baseById)
            {
                if (baseId.head == null)
                {
                    missing.Add(baseId);
                    continue;
                }

                ObjectLoader ldr = repo.OpenObject(readCurs, baseId);
                if (ldr == null)
                {
                    missing.Add(baseId);
                    continue;
                }

                byte[] data = ldr.getCachedBytes();
                int typeCode = ldr.getType();
                

                crc.Reset();
                packOut.Seek(end, SeekOrigin.Begin);
                writeWhole(def, typeCode, data);
				PackedObjectInfo oe = new PackedObjectInfo(end, (int)crc.Value, baseId);
                entries[entryCount++] = oe;
                end = packOut.Position;

                resolveChildDeltas(oe.Offset, typeCode, data, oe);
                if (progress.IsCancelled)
                {
                	throw new IOException("Download cancelled during indexing");
                }
            }
            def.Finish();

            foreach (DeltaChain baseDC in missing)
            {
                if (baseDC.head != null)
                    throw new MissingObjectException(baseDC, "delta base");
            }

            fixHeaderFooter(packcsum, packDigest.Digest());
        }

        private void writeWhole(Deflater def, int typeCode, byte[] data)
        {
            int sz = data.Length;
            int hdrlen = 0;
            buf[hdrlen++] = (byte) ((typeCode << 4) | sz & 15);
            sz = (int) (((uint) sz) >> 7);
            while (sz > 0)
            {
                buf[hdrlen - 1] |= 0x80;
                buf[hdrlen++] = (byte) (sz & 0x7f);
                sz = (int) (((uint) sz) >> 7);
            }
            packDigest.Update(buf, 0, hdrlen);
            crc.Update(buf, 0, hdrlen);
            packOut.Write(buf, 0, hdrlen);
            def.Reset();
            def.SetInput(data);
            def.Finish();
            while (!def.IsFinished)
            {
                int datlen = def.Deflate(buf);
                packDigest.Update(buf, 0, datlen);
                crc.Update(buf, 0, datlen);
                packOut.Write(buf, 0, datlen);
            }
        }

        private void fixHeaderFooter(byte[] origcsum, byte[] tailcsum)
        {
            MessageDigest origDigest = Constants.newMessageDigest();
            MessageDigest tailDigest = Constants.newMessageDigest();
            long origRemaining = originalEOF;

            packOut.Seek(0, SeekOrigin.Begin);
            bAvail = 0;
            bOffset = 0;
            fillFromFile(12);

            {
                int origCnt = (int) Math.Min(bAvail, origRemaining);
                origDigest.Update(buf, 0, origCnt);
                origRemaining -= origCnt;
                if (origRemaining == 0)
                    tailDigest.Update(buf, origCnt, bAvail - origCnt);
            }

            NB.encodeInt32(buf, 8, entryCount);
            packOut.Seek(0, SeekOrigin.Begin);
            packOut.Write(buf, 0, 12);
            packOut.Seek(bAvail, SeekOrigin.Begin);

            packDigest.Reset();
            packDigest.Update(buf, 0, bAvail);
            for (;;)
            {
                int n = packOut.Read(buf, 0, buf.Length);
                if (n < 0)
                    break;
                if (origRemaining != 0)
                {
                    int origCnt = (int) Math.Min(n, origRemaining);
                    origDigest.Update(buf, 0, origCnt);
                    origRemaining -= origCnt;
                    if (origRemaining == 0)
                        tailDigest.Update(buf, origCnt, n - origCnt);
                }
                else
                    tailDigest.Update(buf, 0, n);

                packDigest.Update(buf, 0, n);
            }

            if (!Enumerable.SequenceEqual(origDigest.Digest(), origcsum) || !Enumerable.SequenceEqual(tailDigest.Digest(), tailcsum))
            {
                throw new IOException("Pack corrupted while writing to filesystem");
            }

            packcsum = packDigest.Digest();
            packOut.Write(packcsum, 0, packcsum.Length);
        }

        private void growEntries()
        {
            PackedObjectInfo[] ne;
            ne = new PackedObjectInfo[(int)objectCount+baseById.size()];
            entries.ArrayCopy(0, ne, 0, entryCount);
            entries = ne;
        }

        private void writeIdx()
        {
            Array.Sort(entries, 0, entryCount);
            List<PackedObjectInfo> list = new List<PackedObjectInfo>(entries);
            if (entryCount < entries.Length)
                list.RemoveRange(entryCount, entries.Length-entryCount);

            FileStream os = dstIdx.Create();
            try
            {
                PackIndexWriter iw;
                if (outputVersion <= 0)
                    iw = PackIndexWriter.CreateOldestPossible(os, list);
                else
                    iw = PackIndexWriter.CreateVersion(os, outputVersion);
                iw.Write(list, packcsum);
                os.Flush();
            }
            finally
            {
                os.Close();
            }
        }

        private void readPackHeader()
        {
            int hdrln = Constants.PACK_SIGNATURE.Length + 4 + 4;
            int p = fillFromInput(hdrln);
            for (int k = 0; k < Constants.PACK_SIGNATURE.Length; k++)
                if (buf[p + k] != Constants.PACK_SIGNATURE[k])
                    throw new IOException("Not a PACK file.");

            long vers = NB.decodeInt32(buf, p + 4);
            if (vers != 2 && vers != 3)
                throw new IOException("Unsupported pack version " + vers + ".");
            objectCount = NB.decodeUInt32(buf, p + 8);
            use(hdrln);
        }

        private void readPackFooter()
        {
            sync();
            byte[] cmpcsum = packDigest.Digest();
            int c = fillFromInput(20);
            packcsum = new byte[20];
            buf.ArrayCopy(c, packcsum, 0, 20);
            use(20);
            if (packOut != null)
                packOut.Write(packcsum, 0, packcsum.Length);

            if (!cmpcsum.ArrayEquals(packcsum))
                throw new CorruptObjectException("Packfile checksum incorrect.");
        }

        private void endInput()
        {
            objectData = null;
        }

        private void indexOneObject()
        {
            long pos = position();
            crc.Reset();
            int c = readFromInput();
            int typeCode = (c >> 4) & 7;
            long sz = c & 15;
            int shift = 4;
            while ((c & 0x80) != 0)
            {
                c = readFromInput();
                sz += (c & 0x7f) << shift;
                shift += 7;
            }

            switch (typeCode)
            {
                case Constants.OBJ_COMMIT:
                case Constants.OBJ_TREE:
                case Constants.OBJ_BLOB:
                case Constants.OBJ_TAG:
                    whole(typeCode, pos, sz);
                    break;
                case Constants.OBJ_OFS_DELTA:
                    {
                        c = readFromInput();
                        long ofs = c & 127;
                        while ((c & 128) != 0)
                        {
                            ofs += 1;
                            c = readFromInput();
                            ofs <<= 7;
                            ofs += (c & 127);
                        }
                        long pbase = pos - ofs;
                        UnresolvedDelta n;
                        skipInflateFromInput(sz);
                        n = new UnresolvedDelta(pos, (int)crc.Value);
                        if (baseByPos.ContainsKey(pbase))
                        {
                            n.next = baseByPos[pbase];
                            baseByPos[pbase] = n;
                        }
                        else
                        {
                            baseByPos.Add(pbase, n);
                        }
                        deltaCount++;
                        break;
                    }
                case Constants.OBJ_REF_DELTA:
                    {
                        c = fillFromInput(20);
                        crc.Update(buf, c, 20);
                        ObjectId baseId = ObjectId.FromRaw(buf, c);
                        use(20);
                        DeltaChain r = baseById.get(baseId);
                        if (r == null)
                        {
                            r = new DeltaChain(baseId);
                            baseById.add(r);
                        }
                        skipInflateFromInput(sz);
                        r.add(new UnresolvedDelta(pos, (int) crc.Value));
                        deltaCount++;
                        break;
                    }
                    default:
                    throw new IOException("Unknown object type " + typeCode + ".");
            }
        }

        private void whole(int type, long pos, long sz)
        {
            byte[] data = inflateFromInput((int)sz);
            objectDigest.Update(Constants.encodedTypeString(type));
            objectDigest.Update((byte) ' ');
            objectDigest.Update(Constants.encodeASCII(sz));
            objectDigest.Update((byte) 0);
            objectDigest.Update(data);
            tempObjectId.FromRaw(objectDigest.Digest(), 0);

            verifySafeObject(tempObjectId, type, data);
            int crc32 = (int) crc.Value;
            entries[entryCount++] = new PackedObjectInfo(pos, crc32, tempObjectId);
        }

        private void verifySafeObject(AnyObjectId id, int type, byte[] data)
        {
            if (objCheck != null)
            {
                try
                {
                    objCheck.check(type, data);
                }
                catch (CorruptObjectException e)
                {
                    throw new IOException("Invalid " + Constants.typeString(type) + " " + id + ": " + e.Message, e);
                }
            }

            ObjectLoader ldr = repo.OpenObject(readCurs, id);
            if (ldr != null)
            {
                byte[] existingData = ldr.getCachedBytes();
                if (ldr.getType() != type || !data.ArrayEquals(existingData))
                {
                    throw new IOException("Collision on " + id);
                }
            }
        }

        private long position()
        {
            return bBase + bOffset;
        }

        private void position(long pos)
        {
            packOut.Seek(pos, SeekOrigin.Begin);
            bBase = pos;
            bOffset = 0;
            bAvail = 0;
        }

        private int readFromInput()
        {
            if (bAvail == 0)
                fillFromInput(1);

            bAvail--;
            int b = buf[bOffset++] & 0xff;
            crc.Update((uint)b);
            return b;
        }

        private int readFromFile()
        {
            if (bAvail == 0)
                fillFromFile(1);

            bAvail--;
            int b = buf[bOffset++] & 0xff;
            crc.Update((uint)b);
            return b;
        }

        private void use(int cnt)
        {
            bOffset += cnt;
            bAvail -= cnt;
        }

        private int fillFromInput(int need)
        {
            while (bAvail < need)
            {
                int next = bOffset + bAvail;
                int free = buf.Length - next;
                if (free + bAvail < need)
                {
                    sync();
                    next = bAvail;
                    free = buf.Length - next;
                }
                next = stream.Read(buf, next, free);
                if (next <= 0)
                    throw new EndOfStreamException("Packfile is truncated,");
                bAvail += next;
            }
            return bOffset;
        }

        private int fillFromFile(int need)
        {
            if (bAvail < need)
            {
                int next = bOffset + bAvail;
                int free = buf.Length - next;
                if (free + bAvail < need)
                {
                    if (bAvail > 0)
                        buf.ArrayCopy(bOffset, buf, 0, bAvail);
                    bOffset = 0;
                    next = bAvail;
                    free = buf.Length - next;
                }
                next = packOut.Read(buf, next, free);
                if (next <= 0)
                    throw new EndOfStreamException("Packfile is truncated.");
                bAvail += next;
            }
            return bOffset;
        }

        private void sync()
        {
            packDigest.Update(buf, 0, bOffset);
            if (packOut != null)
                packOut.Write(buf, 0, bOffset);
            if (bAvail > 0)
                buf.ArrayCopy(bOffset, buf, 0, bAvail);
            bBase += bOffset;
            bOffset = 0;
        }

        private void skipInflateFromInput(long sz)
        {
            Inflater inf = inflater;
            try
            {
                byte[] dst = objectData;
                int n = 0;
                int p = -1;
                while (!inf.IsFinished)
                {
                    if (inf.IsNeedingInput)
                    {
                        if (p >= 0)
                        {
                            crc.Update(buf, p, bAvail);
                            use(bAvail);
                        }
                        p = fillFromInput(1);
                        inf.SetInput(buf, p, bAvail);
                    }

                    int free = dst.Length - n;
                    if (free < 8)
                    {
                        sz -= n;
                        n = 0;
                        free = dst.Length;
                    }
                    n += inf.Inflate(dst, n, free);
                }
                if (n != sz)
                    throw new IOException("wrong decompressed length");
                n = bAvail - inf.RemainingInput;
                if (n > 0)
                {
                   crc.Update(buf, p, n);
                    use(n);
                }
            }
            catch (IOException e)
            {
                throw corrupt(e);
            }
            finally
            {
                inf.Reset();
            }
        }

        private byte[] inflateFromInput(int sz)
        {
            byte[] dst = new byte[(int) sz];
            Inflater inf = inflater;
            try
            {
                int n = 0;
                int p = -1;
                while (!inf.IsFinished)
                {
                    if (inf.IsNeedingInput)
                    {
                        if (p >= 0)
                        {
                            crc.Update(buf, p, bAvail);
                            use(bAvail);
                        }
                        p = fillFromFile(1);
                        inf.SetInput(buf, p, bAvail);
                    }

                    n += inf.Inflate(dst, n, sz - n);
                }
                n = bAvail - inf.RemainingInput;
                if (n > 0)
                {
                    crc.Update(buf, p, n);
                    use(n);
                }
                return dst;
            }
            catch (IOException e)
            {
                throw corrupt(e);
            }
            finally
            {
                inf.Reset();
            }
        }

        private byte[] inflateFromFile(long sz)
        {
            byte[] dst = new byte[(int)sz];
            Inflater inf = inflater;
            try
            {
                int n = 0;
                int p = -1;
                while (!inf.IsFinished)
                {
                    if (inf.IsNeedingInput)
                    {
                        if (p >= 0)
                        {
                            crc.Update(buf, p, bAvail);
                            use(bAvail);
                        }
                        p = fillFromInput(1);
                        inf.SetInput(buf, p, bAvail);
                    }

                    n += inf.Inflate(dst, n, dst.Length - n);
                }
                if (n != sz)
                    throw new IOException("wrong decompressed length");
                n = bAvail - inf.RemainingInput;
                if (n > 0)
                {
                    crc.Update(buf, p, n);
                    use(n);
                }
                return dst;
            }
            catch (IOException e)
            {
                throw corrupt(e);
            }
            finally
            {
                inf.Reset();
            }
        }


        private static CorruptObjectException corrupt(IOException e)
        {
            return new CorruptObjectException("Packfile corruption detected: " + e.Message);
        }

        public void renameAndOpenPack()
        {
            renameAndOpenPack(null);
        }

        public PackLock renameAndOpenPack(string lockMessage)
        {
            if (!keepEmpty && entryCount == 0)
            {
                cleanupTemporaryFiles();
                return null;
            }

            MessageDigest d = Constants.newMessageDigest();
            byte[] oeBytes = new byte[Constants.OBJECT_ID_LENGTH];
            for (int i = 0; i < entryCount; i++)
            {
                PackedObjectInfo oe = entries[i];
                oe.copyRawTo(oeBytes, 0);
                d.Update(oeBytes);
            }

            string name = ObjectId.FromRaw(d.Digest()).Name;
            DirectoryInfo packDir = new DirectoryInfo(Path.Combine(repo.ObjectsDirectory.ToString(), "pack"));
            FileInfo finalPack = new FileInfo(Path.Combine(packDir.ToString(), "pack-" + name + ".pack"));
            FileInfo finalIdx = new FileInfo(Path.Combine(packDir.ToString(), "pack-" + name + ".idx"));
            PackLock keep = new PackLock(finalPack);

            if (!packDir.Exists)
            {
                packDir.Create();
                if (!packDir.Exists)
                {
                    cleanupTemporaryFiles();
                    throw new IOException("Cannot create " + packDir.ToString());
                }
            }

            if (finalPack.Exists)
            {
                cleanupTemporaryFiles();
                return null;
            }

            if (lockMessage != null)
            {
                try
                {
                    if (!keep.Lock(lockMessage))
                        throw new IOException("Cannot lock pack in " + finalPack);
                }
                catch (IOException e)
                {
                    cleanupTemporaryFiles();
                    throw e;
                }
            }

            if (!dstPack.RenameTo(finalPack.ToString()))
            {
                cleanupTemporaryFiles();
                keep.Unlock();
                throw new IOException("Cannot move pack to " + finalPack);
            }

            if (!dstIdx.RenameTo(finalIdx.ToString()))
            {
                cleanupTemporaryFiles();
                keep.Unlock();
                finalPack.Delete();
                //if (finalPack.Exists)
                // [caytchen] TODO: finalPack.deleteOnExit();
                throw new IOException("Cannot move index to " + finalIdx);
            }

            try
            {
                repo.openPack(finalPack, finalIdx);
            }
            catch (IOException err)
            {
                keep.Unlock();
                finalPack.Delete();
                finalIdx.Delete();
                throw err;
            }

            return lockMessage != null ? keep : null;
        }

        private void cleanupTemporaryFiles()
        {
            dstIdx.Delete();
            //if (dstIdx.Exists)
                // [caytchen] TODO: dstIdx.deleteOnExit();
            dstPack.Delete();
            //if (dstPack.Exists)
                // [caytchen] TODO: dstPack.deleteOnExit();
        }

    }

}