/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2007, Shawn O. Pearce <spearce@spearce.org>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 *
 * - Neither the name of the Eclipse Foundation, Inc. nor the
 * contributors may be used to endorse or promote products derived from this
 * software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */



/*
 * JUnit TestCase with specialized support for temporary local repository.
 * <p>
 * A temporary directory is created for each test, allowing each test to use a
 * fresh environment. The temporary directory is cleaned up after the test ends.
 * <p>
 * Callers should not use {@link RepositoryCache} from within these tests as it
 * may wedge file descriptors open past the end of the test.
 * <p>
 * A system property {@code jgit.junit.usemmap} defines whether memory mapping
 * is used. Memory mapping has an effect on the file system, in that memory
 * mapped files in Java cannot be deleted as long as the mapped arrays have not
 * been reclaimed by the garbage collector. The programmer cannot control this
 * with precision, so temporary files may hang around longer than desired during
 * a test, or tests may fail altogether if there is insufficient file
 * descriptors or address space for the test process.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GitSharp.Core;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;
using GitSharp.Tests.GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Util
{
    public abstract class LocalDiskRepositoryTestCase  {
#warning setting useMMAP to true makes many tests fail. This has to be investigated.
        private static bool useMMAP = false; 
    
        // Java line below has not been ported
        //useMMAP = "true".equals(System.getProperty("jgit.junit.usemmap"));

        /** A fake (but stable) identity for author fields in the test. */
        protected PersonIdent author;

        /** A fake (but stable) identity for committer fields in the test. */
        protected PersonIdent committer;

        private DirectoryInfo trash = new DirectoryInfo(Path.Combine("target", "trash"));

        private List<Core.Repository> toClose = new List<Core.Repository>();

        private MockSystemReader mockSystemReader;

        [SetUp]
        public virtual void setUp(){
            recursiveDelete(testName() + " (SetUp)", trash, false, true);

            mockSystemReader = new MockSystemReader();
            mockSystemReader.userGitConfig = new FileBasedConfig(new FileInfo(Path.Combine(trash.FullName, "usergitconfig")));
            SystemReader.setInstance(mockSystemReader);

            long now = mockSystemReader.getCurrentTime();
            int tz = mockSystemReader.getTimezone(now);
            author = new PersonIdent("J. Author", "jauthor@example.com");
            author = new PersonIdent(author, now, tz);

            committer = new PersonIdent("J. Committer", "jcommitter@example.com");
            committer = new PersonIdent(committer, now, tz);

            WindowCacheConfig c = new WindowCacheConfig();
            c.PackedGitLimit = (128 * WindowCacheConfig.Kb);
            c.PackedGitWindowSize = (8 * WindowCacheConfig.Kb);
            c.PackedGitMMAP = (useMMAP);
            c.DeltaBaseCacheLimit = (8 * WindowCacheConfig.Kb);
            WindowCache.reconfigure(c);
        }

        [TearDown]
        public virtual void tearDown()  {
            RepositoryCache.clear();
            foreach (Core.Repository r in toClose)
                r.Dispose();
            toClose.Clear();

            // Since memory mapping is controlled by the GC we need to
            // tell it this is a good time to clean up and unlock
            // memory mapped files.
            //
            if (useMMAP)
                System.GC.Collect();

            recursiveDelete(testName() + " (TearDown)", trash, false, true);

        }

        [TestFixtureTearDown]
        public virtual void FixtureTearDown()
        {
            recursiveDelete(testName() + " (FixtureTearDown)", trash, false, true);
        }

        /** Increment the {@link #author} and {@link #committer} times. */
        protected void tick() {
            long delta = (long)TimeSpan.FromMinutes(5).TotalMilliseconds;
            long now = author.When + delta;
            int tz = mockSystemReader.getTimezone(now);

            author = new PersonIdent(author, now, tz);
            committer = new PersonIdent(committer, now, tz);
        }

        /**
	 * Recursively delete a directory, failing the test if the delete fails.
	 *
	 * @param dir
	 *            the recursively directory to delete, if present.
	 */
        protected void recursiveDelete(FileSystemInfo dir) {
            recursiveDelete(testName(), dir, false, true);
        }

        private static bool recursiveDelete(string testName, FileSystemInfo fs, bool silent,  bool failOnError)
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
                    silent = recursiveDelete(testName, e, silent, failOnError);
                }

                dir.Delete();
            }
            catch (IOException e)
            {
                ReportDeleteFailure(testName, failOnError, fs, e.Message);
            }

            return silent;
        }

        private static void ReportDeleteFailure(string name, bool failOnError, FileSystemInfo fsi, string message)
        {
            string severity = failOnError ? "Error" : "Warning";
            string msg = severity + ": Failed to delete " + fsi.FullName;

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

        /**
	 * Creates a new empty bare repository.
	 *
	 * @return the newly created repository, opened for access
	 * @throws IOException
	 *             the repository could not be created in the temporary area
	 */
        protected Core.Repository createBareRepository()  {
            return createRepository(true /* bare */);
        }

        /**
	 * Creates a new empty repository within a new empty working directory.
	 *
	 * @return the newly created repository, opened for access
	 * @throws IOException
	 *             the repository could not be created in the temporary area
	 */
        protected Core.Repository createWorkRepository()  {
            return createRepository(false /* not bare */);
        }

        /**
	 * Creates a new empty repository.
	 *
	 * @param bare
	 *            true to create a bare repository; false to make a repository
	 *            within its working directory
	 * @return the newly created repository, opened for access
	 * @throws IOException
	 *             the repository could not be created in the temporary area
	 */
        private Core.Repository createRepository(bool bare) {
            String uniqueId = GetType().Name + Guid.NewGuid().ToString();
            String gitdirName = "test" + uniqueId + (bare ? "" : "/") + Constants.DOT_GIT;
            DirectoryInfo gitdir = new DirectoryInfo(Path.Combine(trash.FullName, gitdirName));
            Core.Repository db = new Core.Repository(gitdir);

            Assert.IsFalse(gitdir.Exists);
            db.Create();
            toClose.Add(db);
            return db;
        }

        /**
	 * Run a hook script in the repository, returning the exit status.
	 *
	 * @param db
	 *            repository the script should see in GIT_DIR environment
	 * @param hook
	 *            path of the hook script to execute, must be executable file
	 *            type on this platform
	 * @param args
	 *            arguments to pass to the hook script
	 * @return exit status code of the invoked hook
	 * @throws IOException
	 *             the hook could not be executed
	 * @throws InterruptedException
	 *             the caller was interrupted before the hook completed
	 */
        protected int runHook(Core.Repository db, FileInfo hook,
                              params string[] args) {
            String[] argv = new String[1 + args.Length];
            argv[0] = hook.FullName;
            System.Array.Copy(args, 0, argv, 1, args.Length);

            IDictionary<String, String> env = cloneEnv();
            env.put("GIT_DIR", db.Directory.FullName);
            putPersonIdent(env, "AUTHOR", author);
            putPersonIdent(env, "COMMITTER", committer);

            DirectoryInfo cwd = db.WorkingDirectory;

            throw new NotImplementedException("Not ported yet.");
            //Process p = Runtime.getRuntime().exec(argv, toEnvArray(env), cwd);
            //p.getOutputStream().close();
            //p.getErrorStream().close();
            //p.getInputStream().close();
            //return p.waitFor();
                              }

        private static void putPersonIdent(IDictionary<String, String> env,
                                           string type, PersonIdent who) {
            string ident = who.ToExternalString();
            string date = ident.Substring(ident.IndexOf("> ") + 2);
            env.put("GIT_" + type + "_NAME", who.Name);
            env.put("GIT_" + type + "_EMAIL", who.EmailAddress);
            env.put("GIT_" + type + "_DATE", date);
                                           }

        /**
	 * Create a string to a UTF-8 temporary file and return the path.
	 *
	 * @param body
	 *            complete content to write to the file. If the file should end
	 *            with a trailing LF, the string should end with an LF.
	 * @return path of the temporary file created within the trash area.
	 * @throws IOException
	 *             the file could not be written.
	 */
        protected FileInfo write(string body) {

            string filepath = Path.Combine(trash.FullName, "temp" + Guid.NewGuid() + ".txt");

            try
            {
                File.WriteAllText(filepath, body);
            }
            catch (Exception)
            {
                File.Delete(filepath);
                throw;
            }

            return new FileInfo(filepath);
        }

        /**
	 * Write a string as a UTF-8 file.
	 *
	 * @param f
	 *            file to write the string to. Caller is responsible for making
	 *            sure it is in the trash directory or will otherwise be cleaned
	 *            up at the end of the test. If the parent directory does not
	 *            exist, the missing parent directories are automatically
	 *            created.
	 * @param body
	 *            content to write to the file.
	 * @throws IOException
	 *             the file could not be written.
	 */
        protected void write(FileInfo f, string body) {
            f.Directory.Mkdirs();

            using (var s = new StreamWriter(f.FullName, false, Charset.forName("UTF-8")))
            {
                s.Write(body);
            }
        }

        /**
	 * Fully read a UTF-8 file and return as a string.
	 *
	 * @param f
	 *            file to read the content of.
	 * @return UTF-8 decoded content of the file, empty string if the file
	 *         exists but has no content.
	 * @throws IOException
	 *             the file does not exist, or could not be read.
	 */
        protected String read(FileInfo f) {
            using(var s = new StreamReader(f.FullName, Charset.forName("UTF-8")))
            {
                return s.ReadToEnd();
            }
        }

        private static String[] toEnvArray(IDictionary<String, String> env) {
            String[] envp = new String[env.Count];
            int i = 0;
            foreach (KeyValuePair<string, string> e in env) {
                envp[i++] = e.Key + "=" + e.Value;
            }
            return envp;
        }

        private static IDictionary<String, String> cloneEnv() 
        {
            var dic = new Dictionary<string, string>();	    
        
            foreach (DictionaryEntry e in Environment.GetEnvironmentVariables())
            {
                dic.Add(e.Key.ToString(), e.Value.ToString());
            }

            return dic;
        }

        private String testName() {
            return this.GetType().FullName;
            //return getClass().getName() + "." + getName();
        }
    }
}