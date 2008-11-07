using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gitty.Util;

namespace Gitty.Lib
{
    [Complete]
    public class RefLogWriter
    {
        /**
         * Writes reflog entry for ref specified by refName
         * 
         * @param repo
         *            repository to use
         * @param oldCommit
         *            previous commit
         * @param commit
         *            new commit
         * @param message
         *            reflog message
         * @param refName
         *            full ref name         
         */
        public static void WriteReflog(Repository repo, ObjectId oldCommit, ObjectId commit, String message, String refName)
        {
            String entry = BuildReflogString(repo, oldCommit, commit, message);

            DirectoryInfo directory = repo.Directory;

            FileInfo reflogfile = PathUtil.CombineFilePath(directory, "logs/" + refName);
            DirectoryInfo reflogdir = reflogfile.Directory;
            if (!reflogdir.Exists)
            {
                try
                {
                    reflogdir.Create();
                }
                catch (Exception)
                {
                    throw new IOException("Cannot create directory " + reflogdir);
                }
            }
            StreamWriter writer = new StreamWriter(reflogfile.OpenWrite());
            writer.WriteLine(entry);
            writer.Close();
        }

        private static String BuildReflogString(Repository repo, ObjectId oldCommit, ObjectId commit, String message)
        {
            PersonIdent me = new PersonIdent(repo);
            String initial = "";
            if (oldCommit == null)
            {
                oldCommit = ObjectId.ZeroId;
                initial = " (initial)";
            }
            String s = oldCommit.ToString() + " " + commit.ToString() + " "
                    + me.ToExternalString() + "\t" + message + initial;
            return s;
        }

    }

}
