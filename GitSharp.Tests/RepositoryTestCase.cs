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

using System.IO;
using GitSharp.Core;
using NUnit.Framework;
using GitSharp.Core.Util;

namespace GitSharp.Tests
{

    /**
     * Base class for most unit tests.
     *
     * Sets up a predefined test repository and has support for creating additional
     * repositories and destroying them when the tests are finished.
     */
    public abstract class RepositoryTestCase : LocalDiskRepositoryTestCase
    {
        /** Test repository, initialized for this test case. */
        protected Core.Repository db;


        /// <summary>
        /// mock user's global configuration used instead ~/.gitconfig.
        /// This configuration can be modified by the tests without any
        /// effect for ~/.gitconfig.
        /// </summary>
        protected RepositoryConfig userGitConfig;

        protected FileInfo writeTrashFile(string name, string data)
        {
            var path = new FileInfo(Path.Combine(db.WorkingDirectory.FullName, name));
    		write(path, data);
	    	return path;
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

       	/** Working directory of {@link #db}. */
    	protected DirectoryInfo trash;

        public override void setUp()
        {
            base.setUp();

            db = createWorkRepository();
            trash = db.WorkingDirectory;

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

            new FileInfo("Resources/packed-refs").CopyTo(db.Directory.FullName + "/packed-refs", true);
        }

        protected static void CopyDirectory(string sourceDirectoryPath, string targetDirectoryPath)
        {
            if (!targetDirectoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                targetDirectoryPath += Path.DirectorySeparatorChar;
            }

            if (!Directory.Exists(targetDirectoryPath))
            {
                Directory.CreateDirectory(targetDirectoryPath);
            }

            string[] files = Directory.GetFileSystemEntries(sourceDirectoryPath);

            foreach (string fileSystemElement in files)
            {
                if (Directory.Exists(fileSystemElement))
                {
                    CopyDirectory(fileSystemElement, targetDirectoryPath + Path.GetFileName(fileSystemElement));
                    continue;
                }

                File.Copy(fileSystemElement, targetDirectoryPath + Path.GetFileName(fileSystemElement), true);
            }
        }
    }
}