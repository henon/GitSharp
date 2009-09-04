/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.IO;
using GitSharp.Util;
using System.Diagnostics;

namespace GitSharp
{
    [Complete]
    public class LockFile
    {
        private FileInfo _refFile;
        private FileInfo _lockFile;
        private FileStream _os;
        private FileLock _fLck;
        private bool _haveLock;


        public DateTime CommitLastModified { get; private set; }
        public bool NeedStatInformation { get; set; }

        public LockFile(FileInfo file)
        {
            _refFile = file;
            _lockFile = PathUtil.CombineFilePath(_refFile.Directory, _refFile.Name + ".lock");
        }

        public bool Lock()
        {
            _lockFile.Directory.Create();
            if (_lockFile.Exists)
            {
                return false;
            }

            try
            {
                _haveLock = true;
                _os = _lockFile.Create();

                _fLck = FileLock.TryLock(_os, _lockFile);
                if (_fLck == null)
                {
                    // We cannot use unlock() here as this file is not
                    // held by us, but we thought we created it. We must
                    // not delete it, as it belongs to some other process.
                    _haveLock = false;
                    try
                    {
                        _os.Close();
                    }
                    catch (Exception)
                    {
                        // Fail by returning haveLck = false.
                    }
                    _os = null;
                }
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }

            return _haveLock;
        }

        public bool LockForAppend()
        {
            if (!Lock())
            {
                return false;
            }

            CopyCurrentContent();

            return true;
        }


        public void CopyCurrentContent()
        {
            RequireLock();
            try
            {
                FileStream fis = _refFile.OpenRead();
                try
                {
                    byte[] buf = new byte[2048];
                    int r;
                    while ((r = fis.Read(buf, 0, buf.Length)) >= 0)
                        _os.Write(buf, 0, r);
                }
                finally
                {
                    fis.Close();
                }
            }
            catch (FileNotFoundException)
            {
                // Don't worry about a file that doesn't exist yet, it
                // conceptually has no current content to copy.
                //
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }
        }

        public void Unlock()
        {
            if (_os != null)
            {
                if (_fLck != null)
                {
                    try
                    {
                        _fLck.Release();
                    }
                    catch (IOException)
                    {
                        // Huh?
                    }
                    _fLck = null;
                }
                try
                {
                    _os.Close();
                }
                catch (IOException)
                {
                    // Ignore this
                }
                _os = null;
            }

            if (_haveLock)
            {
                _haveLock = false;
                _lockFile.Delete();
            }
        }

        public bool Commit()
        {
            if (_os != null)
            {
                Unlock();
                throw new InvalidOperationException("Lock on " + _refFile + " not closed.");
            }

            SaveStatInformation();
            try
            {
                FileInfo copy = new FileInfo(_lockFile.FullName);
                _lockFile.MoveTo(_refFile.FullName);
                _lockFile = copy;
                return true;
            }
            catch (Exception)
            {
                try
                {
                    if (_refFile.Exists) _refFile.Delete();

                    FileInfo copy = new FileInfo(_lockFile.FullName);
                    _lockFile.MoveTo(_refFile.FullName);
                    _lockFile = copy;
                    return true;
                }
                catch (Exception)
                {

                }
            }

            Unlock();
            return false;
        }

        public void Write(byte[] content)
        {
            RequireLock();
            try
            {
                _os.Write(content, 0, content.Length);
                _os.Flush();
                _fLck.Release();
                _os.Close();
                _os = null;
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }
        }

        public void Write(ObjectId id)
        {
            RequireLock();
            try
            {
                var b = new BinaryWriter(_os);
                id.CopyTo(b);
                b.Write('\n');
                b.Flush();
                _fLck.Release();
                b.Close();
                _os = null;
            }
            catch (Exception)
            {
                Unlock();
                throw;
            }
        }

        private void RequireLock()
        {
            if (_os == null)
            {
                Unlock();
                throw new InvalidOperationException("Lock on " + _refFile + " not held.");
            }
        }

        private void SaveStatInformation()
        {
            if (NeedStatInformation)
            {
                CommitLastModified = _lockFile.LastWriteTime;
            }
        }

        /**
         * Obtain the direct output stream for this lock.
         *
         * The stream may only be accessed once, and only after {@link #lock()} has
         * been successfully invoked and returned true. Callers must close the
         * stream prior to calling {@link #commit()} to commit the change.
         * 
         * @return a stream to write to the new file. The stream is unbuffered.
         */
        public Stream GetOutputStream()
        {
            RequireLock();
            return new LockFileOutputStream(this);
        }

        public class LockFileOutputStream : Stream
        {
            private LockFile m_lock_file;
            public LockFileOutputStream(LockFile lockfile)
            {
                m_lock_file = lockfile;
            }

            public override void Write(byte[] b, int o, int n)
            {
                m_lock_file._os.Write(b, o, n);
            }

            public void write(byte[] b)
            {
                m_lock_file._os.Write(b, 0, b.Length);
            }

            public void write(int b)
            {
                m_lock_file._os.WriteByte((byte)b);
            }

            public override void Flush()
            {
                m_lock_file._os.Flush();
            }

            public override void Close()
            {
                try
                {
                    m_lock_file._os.Flush();
                    m_lock_file._fLck.Release();
                    m_lock_file._os.Close();
                    m_lock_file._os = null;
                }
                catch (Exception)
                {
                    m_lock_file.Unlock();
                    throw;
                }
            }

            public override bool CanRead
            {
                get { throw new NotImplementedException(); }
            }

            public override bool CanSeek
            {
                get { throw new NotImplementedException(); }
            }

            public override bool CanWrite
            {
                get { throw new NotImplementedException(); }
            }

            public override long Length
            {
                get { throw new NotImplementedException(); }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }
        }


        /// <summary>
        /// Wraps a FileStream and tracks its locking status
        /// </summary>
        public class FileLock : IDisposable
        {
            public FileStream FileStream { get; private set; }
            public bool Locked { get; private set; }
            public string File { get; private set; }

            private FileLock(FileStream fs, string file)
            {
                File = file;
                FileStream = fs;
                FileStream.Lock(0, long.MaxValue);
                Locked = true;
            }

            public static FileLock TryLock(FileStream fs, FileInfo file)
            {
                try
                {
                    return new FileLock(fs, file.FullName);
                }
                catch (IOException e)
                {
                    Debug.WriteLine("Could not lock " + file.FullName);
                    Debug.WriteLine(e.Message);
                    return null;
                }
            }

            public void Dispose()
            {
                if (Locked == false)
                {
                    return;
                }
                Release();
            }

            public void Release()
            {
                if (Locked == false)
                {
                    return;
                }
                try
                {
                    FileStream.Unlock(0, long.MaxValue);
#if DEBUG
                    GC.SuppressFinalize(this); // [henon] disarm lock-release checker
#endif
                }
                catch (IOException)
                {
                    // unlocking went wrong
                    Debug.WriteLine(GetType().Name + ": tried to unlock an unlocked filelock " + File);
                    throw;
                }
                Locked = false;
                Dispose();
            }

#if DEBUG
            // [henon] this : a debug mode warning if the filelock has not been disposed properly
            ~FileLock()
            {
                Debug.WriteLine(GetType().Name + " has not been properly disposed: " + File);
            }
#endif
        }

    }
}
