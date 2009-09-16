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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using GitSharp.Util;
using Xunit;

namespace GitSharp.Tests.Util
{
	public abstract class RepositoryTestCase : XunitBaseFact
	{
		/// <summary>
		/// Simulates the reading of system variables and properties.
		/// Unit test can control the returned values by manipulating
		/// <see cref="FakeSystemReader.Values"/>
		/// </summary>
		private static readonly FakeSystemReader SystemReader = new FakeSystemReader();
		private static readonly DirectoryInfo TrashParent = new DirectoryInfo("trash");

		protected static readonly PersonIdent JAuthor = new PersonIdent("J. Author", "jauthor@example.com");
		protected static readonly PersonIdent JCommitter = new PersonIdent("J. Committer", "jcommitter@example.com");

		private readonly List<Repository> _repositoriesToClose;
		private readonly bool _packedGitMmap;
		private int _testcount;

		protected DirectoryInfo trash;
		protected DirectoryInfo trash_git;
		protected Repository db;

		static RepositoryTestCase()
		{
			GitSharpSystemReader.SetInstance(SystemReader);
			Microsoft.Win32.SystemEvents.SessionEnded += (o, args) => // cleanup
                RecursiveDelete(new DirectoryInfo(TrashParent.FullName), false, null, false);
		}

		protected RepositoryTestCase()
		{
			_repositoriesToClose = new List<Repository>();
		}

		/// <summary>
		/// Configure Git before setting up test repositories.
		/// </summary>
		private void Configure()  // [henon] reading performance can be implemented later
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

		#region Test setup / teardown

		protected override void SetUp()
		{
			Configure();

			string name = GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;

			// Cleanup old failed stuff
			RecursiveDelete(new DirectoryInfo(TrashParent.FullName), true, name, false);

			trash = new DirectoryInfo(TrashParent + "/trash" + DateTime.Now.Ticks + "." + (_testcount++));
			trash_git = new DirectoryInfo(trash + "/.git");

			var gitConfigFile = new FileInfo(trash_git + "/usergitconfig").FullName;
			var gitConfig = new RepositoryConfig(null, new FileInfo(gitConfigFile));

			SystemReader.SetUserGitConfig(gitConfig);

			db = new Repository(trash_git);
			db.Create();

			string[] packs = {
			                 	"pack-34be9032ac282b11fa9babdc2b2a93ca996c9c2f",
			                 	"pack-df2982f284bbabb6bdb59ee3fcc6eb0983e20371",
			                 	"pack-9fb5b411fe6dfa89cc2e6b89d2bd8e5de02b5745",
			                 	"pack-546ff360fe3488adb20860ce3436a2d6373d2796",
			                 	"pack-e6d07037cbcf13376308a0a995d1fa48f8f76aaa",
			                 	"pack-3280af9c07ee18a87705ef50b0cc4cd20266cf12"
			                 };

			var packDir = new DirectoryInfo(db.ObjectsDirectory + "/pack");

			foreach (var packname in packs)
			{
				new FileInfo("Resources/" + GitSharp.Transport.IndexPack.GetPackFileName(packname)).CopyTo(packDir + "/" + GitSharp.Transport.IndexPack.GetPackFileName(packname), true);
				new FileInfo("Resources/" + GitSharp.Transport.IndexPack.GetIndexFileName(packname)).CopyTo(packDir + "/" + GitSharp.Transport.IndexPack.GetIndexFileName(packname), true);
			}

			new FileInfo("Resources/packed-refs").CopyTo(trash_git.FullName + "/packed-refs", true);

			// Read fake user configuration values
			SystemReader.Values.Clear();
			SystemReader.Values[Constants.OS_USER_NAME_KEY] = Constants.OS_USER_NAME_KEY;
			SystemReader.Values[Constants.GIT_AUTHOR_NAME_KEY] = Constants.GIT_AUTHOR_NAME_KEY;
			SystemReader.Values[Constants.GIT_AUTHOR_EMAIL_KEY] = Constants.GIT_AUTHOR_EMAIL_KEY;
			SystemReader.Values[Constants.GIT_COMMITTER_NAME_KEY] = Constants.GIT_COMMITTER_NAME_KEY;
			SystemReader.Values[Constants.GIT_COMMITTER_EMAIL_KEY] = Constants.GIT_COMMITTER_EMAIL_KEY;
		}

		protected override void TearDown()
		{
			db.Close();
			foreach (var r in _repositoriesToClose)
			{
				r.Close();
			}

			// Since memory mapping is controlled by the GC we need to
			// tell it this is a good time to clean up and unlock
			// memory mapped files.
			if (_packedGitMmap)
			{
				GC.Collect();
			}

			string name = GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name;
			RecursiveDelete(new DirectoryInfo(trash.FullName), false, name, true);
			foreach (var r in _repositoriesToClose)
			{
				RecursiveDelete(new DirectoryInfo(r.WorkingDirectory.FullName), false, name, true);
			}

			_repositoriesToClose.Clear();
		}

		#endregion

		#region --> Recursive deletion utility

		/// <summary>
		/// Utility method to delete a directory recursively. It is
		/// also used internally. If a file or directory cannot be removed
		/// it throws an AssertionFailure.
		/// </summary>
		/// <param name="fs"></param>
		protected void recursiveDelete(FileSystemInfo fs)
		{
			RecursiveDelete(fs, false, GetType().Name + "." + ToString(), true);
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
		private static bool RecursiveDelete(FileSystemInfo fs, bool silent, string name, bool failOnError)
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
					silent = RecursiveDelete(e, silent, name, failOnError);
				}

				dir.Delete();
			}
			catch (IOException e)
			{
				//ReportDeleteFailure(name, failOnError, fs);
				Console.WriteLine(name + ": " + e.Message);
			}

			return silent;
		}
 
		private static void ReportDeleteFailure(string name, bool failOnError, FileSystemInfo fsi)
		{
			string severity = failOnError ? "Error" : "Warning";
			string msg = severity + ": Failed to delete " + fsi;
 
			if (name != null)
			{
				msg += " in " + name;
			}
 
			if (failOnError)
			{
				Assert.False(true, msg);
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
			var tf = new FileInfo(Path.Combine(trash.FullName, name).Replace('/', Path.DirectorySeparatorChar));
			var tfp = tf.Directory;

			Assert.NotNull(tfp);

			if (!tfp.Exists && !tfp.Mkdirs())
			{
				if (!tfp.Exists)
				{
					throw new IOException("Could not create directory " + tfp.FullName);
				}
			}

			File.WriteAllText(tf.FullName, data, Constants.CHARSET);

			return tf;
		}

		protected static void CheckFile(FileInfo f, string checkData)
		{
			var readData = File.ReadAllText(f.FullName, Encoding.GetEncoding("ISO-8859-1"));

			if (checkData.Length != readData.Length)
			{
				throw new IOException("Internal error reading file data from " + f);
			}

			Assert.Equal(checkData, readData);
		}

		/// <summary>
		/// Helper for creating extra empty repos
		/// </summary>
		/// <returns>
		/// A new empty git repository for testing purposes
		/// </returns>
		protected Repository CreateNewEmptyRepo()
		{
			var newTestRepo = new DirectoryInfo(TrashParent + "/new" + DateTime.Now.Ticks + "." + (_testcount++) + "/.git");
			Assert.False(newTestRepo.Exists);
			var newRepo = new Repository(newTestRepo);
			newRepo.Create();
			_repositoriesToClose.Add(newRepo);
			return newRepo;
		}

		protected void SetupReflog(string logName, byte[] data)
		{
			var logfile = new FileInfo(Path.Combine(db.Directory.FullName, logName));
            
			if (!logfile.Directory.Mkdirs() && !logfile.Directory.IsDirectory())
			{
				throw new IOException("oops, cannot create the directory for the test reflog file"
					+ logfile);
			}

			File.WriteAllBytes(logfile.FullName, data);
		}

		#region Nested Types

		private class FakeSystemReader : ISystemReader
		{
			private RepositoryConfig _userGitConfig;

			public FakeSystemReader()
			{
				Values = new Dictionary<string, string>();
			}

			public Dictionary<string, string> Values { get; private set; }

			#region Implementation of ISystemReader

			public string getHostname()
			{
				return Dns.GetHostName();
			}

			public string getenv(string variable)
			{
				return Values[variable];
			}

			public string getProperty(string key)
			{
				return Values[key];
			}

			public RepositoryConfig openUserConfig()
			{
				return _userGitConfig;
			}

			#endregion

			public void SetUserGitConfig(RepositoryConfig userGitConfig)
			{
				_userGitConfig = userGitConfig;
			}
		}

		#endregion
	}
}