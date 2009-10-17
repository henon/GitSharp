/*
 * Copyright (C) 2007, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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
using System.IO;
using System.Linq;
using GitSharp.Core;
using NUnit.Framework;
using System.Diagnostics;
using GitSharp.Core.Util;

namespace GitSharp.Tests
{

    /**
     * Base class for most unit tests.
     *
     * Sets up a predefined test repository and has support for creating additional
     * repositories and destroying them when the tests are finished.
     *
     * A system property <em>jgit.junit.usemmap</em> defines whether memory mapping
     * is used. Memory mapping has an effect on the file system, in that memory
     * mapped files in java cannot be deleted as long as they mapped arrays have not
     * been reclaimed by the garbage collector. The programmer cannot control this
     * with precision, though hinting using <em><see cref="java.lang.System#gc}</em>
     * often helps.
     */
    public abstract class RepositoryTestCase
    {
        protected static DirectoryInfo trashParent = new DirectoryInfo("trash");
        protected DirectoryInfo trash;
        protected DirectoryInfo trash_git;
        
        protected static PersonIdent jauthor = new PersonIdent("J. Author", "jauthor@example.com");
        protected static PersonIdent jcommitter = new PersonIdent("J. Committer", "jcommitter@example.com");

        protected bool packedGitMMAP;

        protected Repository db;
        private int _testcount;
        private readonly List<Repository> _repositoriesToClose = new List<Repository>();
        private readonly List<string> _directoriesToRemove = new List<string>();

        /// <summary>
        /// Configure Git before setting up test repositories.
        /// </summary>
        protected void Configure()  // [henon] reading performance can be implemented later
        {
            //var c = new WindowCacheConfig
            //                          {
            //                              PackedGitLimit = 128 * WindowCacheConfig.Kb,
            //                              PackedGitWindowSize = 8 * WindowCacheConfig.Kb,
            //                              PackedGitMMAP = "true".equals(System.getProperty("jgit.junit.usemmap")),
            //                              DeltaBaseCacheLimit = 8 * WindowCacheConfig.Kb
            //                          };
            //WindowCache.reconfigure(c);
        }

        #region --> Recursive deletion utility

        /// <summary>
        /// Utility method to delete a directory recursively. It is
        /// also used internally. If a file or directory cannot be removed
        /// it throws an AssertionFailure.
        /// </summary>
        /// <param name="dir"></param>
        protected void recursiveDelete(FileSystemInfo fs)
        {
            recursiveDelete(fs, false, GetType().Name + "." + ToString(), true);
        }

        /// <summary>
        /// Utility method to delete a directory recursively. It is
        /// also used internally. If a file or directory cannot be removed
        /// it throws an AssertionFailure.
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="silent"></param>
        /// <param name="name"></param>
        /// <param name="failOnError"></param>
        /// <returns></returns>
        protected static bool recursiveDelete(FileSystemInfo fs, bool silent, string name, bool failOnError)
        {
            Debug.Assert(!(silent && failOnError));

            if (fs.IsFile())
            {
                fs.DeleteFile();
                return silent;
            }

            var dir = new DirectoryInfo(fs.FullName);
            if (!dir.Exists) return silent;

            try
            {
                FileSystemInfo[] ls = dir.GetFileSystemInfos();

                foreach (FileSystemInfo e in ls)
                {
                    silent = recursiveDelete(e, silent, name, failOnError);
                }

                dir.Delete();
            }
            catch (IOException e)
            {
                ReportDeleteFailure(name, failOnError, fs, e.Message);
            }

            return silent;
        }

        private static void ReportDeleteFailure(string name, bool failOnError, FileSystemInfo fsi, string message)
        {
            string severity = failOnError ? "Error" : "Warning";
            string msg = severity + ": Failed to delete " + fsi;

            if (name != null)
            {
                msg += " in " + name;
            }

            msg += Environment.NewLine;
            msg += message;

            if (failOnError)
            {
                Assert.Fail(msg);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        #endregion

        /// <summary>
        /// mock user's global configuration used instead ~/.gitconfig.
        /// This configuration can be modified by the tests without any
        /// effect for ~/.gitconfig.
        /// </summary>
        protected RepositoryConfig userGitConfig;

        protected FileInfo writeTrashFile(string name, string data)
        {
            FileInfo tf;
			
			tf = new FileInfo(Path.Combine(trash.FullName, name));
            var tfp = tf.Directory;

            if (!tfp.Exists && !tfp.Mkdirs())
            {
                throw new IOException("Could not create directory " + tfp.FullName);
            }

			File.WriteAllText(tf.FullName, data, Constants.CHARSET);

            return tf;
        }

        protected static void checkFile(FileInfo f, string checkData)
        {
            var readData = File.ReadAllText(f.FullName, Charset.forName("ISO-8859-1"));

            if (checkData.Length != readData.Length)
            {
                throw new IOException("Internal error reading file data from " + f);
            }

            Assert.AreEqual(checkData, readData);
        }

        #region Test setup / teardown

        [SetUp]
        public virtual void setUp()
        {
            Configure();

            trash = new DirectoryInfo(trashParent + "/trash" + DateTime.Now.Ticks + "." + (_testcount++));
            
            _directoriesToRemove.Add(trash.FullName);

            trash_git = new DirectoryInfo(Path.GetFullPath(trash + "/.git"));

            var mockSystemReader = new MockSystemReader();
            mockSystemReader.userGitConfig = new FileBasedConfig(
                new FileInfo(Path.Combine(trash_git.FullName, "usergitconfig")));
            SystemReader.setInstance(mockSystemReader);

            db = new Repository(trash_git);
            db.Create();

            string[] packs = {
				"pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f",
				"pack-df2982f284bbabb6bdb59ee3fcc6eb0983e20371",
				"pack-9fb5b411fe6dfa89cc2e6b89d2bd8e5de02b5745",
				"pack-546ff360fe3488adb20860ce3436a2d6373d2796",
				"pack-cbdeda40019ae0e6e789088ea0f51f164f489d14",
				"pack-e6d07037cbcf13376308a0a995d1fa48f8f76aaa",
				"pack-3280af9c07ee18a87705ef50b0cc4cd20266cf12"
		    };

            var packDir = new DirectoryInfo(db.ObjectsDirectory + "/pack");

            foreach (var packname in packs)
            {
                new FileInfo("Resources/" + Core.Transport.IndexPack.GetPackFileName(packname)).CopyTo(packDir + "/" + Core.Transport.IndexPack.GetPackFileName(packname), true);
                new FileInfo("Resources/" + Core.Transport.IndexPack.GetIndexFileName(packname)).CopyTo(packDir + "/" + Core.Transport.IndexPack.GetIndexFileName(packname), true);
            }

            new FileInfo("Resources/packed-refs").CopyTo(trash_git.FullName + "/packed-refs", true);
        }

        [TearDown]
        public virtual void tearDown()
        {
            RepositoryCache.clear();
            db.Close();
            foreach (var r in _repositoriesToClose)
            {
                r.Close();
            }

            // Since memory mapping is controlled by the GC we need to
            // tell it this is a good time to clean up and unlock
            // memory mapped files.
            if (packedGitMMAP)
            {
                GC.Collect();
            }

            _repositoriesToClose.Clear();
        }

        #endregion

        protected Repository createNewEmptyRepo()
        {
            return createNewEmptyRepo(false);
        }

        /// <summary>
        /// Helper for creating extra empty repos
        /// </summary>
        /// <returns>
        /// A new empty git repository for testing purposes
        /// </returns>
        protected Repository createNewEmptyRepo(bool bare)  
        {
            var newTestRepoPath = Path.GetFullPath(trashParent + "/new" + DateTime.Now.Ticks + "." + (_testcount++) );
            var newTestRepoPathSuffix  = (bare ? "" : "/") + ".git";
            var newTestRepo = new DirectoryInfo(newTestRepoPath + newTestRepoPathSuffix);

            Assert.IsFalse(newTestRepo.Exists);
            var newRepo = new Repository(newTestRepo);
            newRepo.Create();
            
            _repositoriesToClose.Add(newRepo);

            if (!bare)
            {
                _directoriesToRemove.Add(newTestRepoPath);
            }
            else
            {
                _directoriesToRemove.Add(newTestRepoPath + newTestRepoPathSuffix);
            }

            return newRepo;
        }

        protected void setupReflog(String logName, byte[] data)
        {
            var logfile = new FileInfo(Path.Combine(db.Directory.FullName, logName));
            
            if (!logfile.Directory.Mkdirs() && !logfile.Directory.IsDirectory())
            {
                throw new IOException(
                    "oops, cannot create the directory for the test reflog file"
                    + logfile);
            }

            File.WriteAllBytes(logfile.FullName, data);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            _directoriesToRemove.Any((directoryPath => recursiveDelete(new DirectoryInfo(directoryPath), false, null, false)));
        }
    }
}