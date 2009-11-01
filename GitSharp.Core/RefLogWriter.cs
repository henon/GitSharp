/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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

using System.IO;
using System.Text;
using System.Threading;

namespace GitSharp.Core
{
	/// <summary>
	/// Utility class to work with reflog files
	/// </summary>
	public static class RefLogWriter
	{
		internal static void append(RefUpdate u, string msg)
		{
			ObjectId oldId = u.OldObjectId;
			ObjectId newId = u.NewObjectId;
			Repository db = u.Repository;
			PersonIdent ident = u.RefLogIdent;

			AppendOneRecord(oldId, newId, ident, msg, db, u.Name);

			if (!u.Name.Equals(u.OriginalName))
			{
				AppendOneRecord(oldId, newId, ident, msg, db, u.OriginalName);
			}
		}

		internal static void append(RefRename refRename, string logName, string msg)
		{
			ObjectId id = refRename.ObjectId;
			Repository db = refRename.Repository;
			PersonIdent ident = refRename.RefLogIdent;
			AppendOneRecord(id, id, ident, msg, db, logName);
		}

		internal static void renameTo(Repository db, RefUpdate from, RefUpdate to)
		{
			var logdir = new DirectoryInfo(Path.Combine(db.Directory.FullName, Constants.LOGS).Replace('/', Path.DirectorySeparatorChar));
			var reflogFrom = new FileInfo(Path.Combine(logdir.FullName, from.Name).Replace('/', Path.DirectorySeparatorChar));
		    DirectoryInfo refLogFromDir = reflogFrom.Directory;
            if (!reflogFrom.Exists) return;

			var reflogTo = new FileInfo(Path.Combine(logdir.FullName, to.Name).Replace('/', Path.DirectorySeparatorChar));
			var reflogToDir = reflogTo.Directory;
			var tmp = new FileInfo(Path.Combine(logdir.FullName, "tmp-renamed-log.." + Thread.CurrentThread.ManagedThreadId));
			if (!reflogFrom.RenameTo(tmp.FullName))
			{
				throw new IOException("Cannot rename " + reflogFrom + " to (" + tmp + ")" + reflogTo);
			}

            RefUpdate.DeleteEmptyDir(refLogFromDir, RefUpdate.Count(from.Name, '/') - 1);
			if (reflogToDir != null && !reflogToDir.Exists)
			{
				try { reflogToDir.Create(); }
				catch(IOException)
				{
					throw new IOException("Cannot create directory " + reflogToDir);
				}
			}

			if (!tmp.RenameTo(reflogTo.FullName))
			{
				throw new IOException("Cannot rename (" + tmp + ")" + reflogFrom + " to " + reflogTo);
			}
		}

        private static void AppendOneRecord(ObjectId oldId, ObjectId newId, PersonIdent ident, string msg, Repository db, string refName)
        {
            ident = ident == null ? new PersonIdent(db) : new PersonIdent(ident);

            var r = new StringBuilder();
            r.Append(ObjectId.ToString(oldId));
            r.Append(' ');
            r.Append(ObjectId.ToString(newId));
            r.Append(' ');
            r.Append(ident.ToExternalString());
            r.Append('\t');
            r.Append(msg);
            r.Append('\n');

            byte[] rec = Constants.encode(r.ToString());
            var logdir = new DirectoryInfo(Path.Combine(db.Directory.FullName, Constants.LOGS));
            var reflog = new DirectoryInfo(Path.Combine(logdir.FullName, refName));

            if (reflog.Exists || db.Config.getCore().isLogAllRefUpdates())
            {
                DirectoryInfo refdir = reflog.Parent;

                if (!refdir.Exists && !refdir.Mkdirs())
                {
                    throw new IOException("Cannot create directory " + refdir);
                }

                using (var @out = new FileStream(reflog.FullName, System.IO.FileMode.Append, FileAccess.Write))
                {
                    @out.Write(rec, 0, rec.Length);
                }
            }
        }

		///	<summary>
		/// Writes reflog entry for ref specified by refName
		///	</summary>
		///	<param name="repo">Repository to use.</param>
		///	<param name="oldCommit">Previous commit.</param>
		///	<param name="commit">New commit.</param>
		///	<param name="message">Reflog message</param>
		///	<param name="refName">Full ref name</param>
		///	<exception cref="IOException"></exception>
		//[Obsolete("Rely upon RefUpdate's automatic logging instead.")]
		public static void WriteReflog(Repository repo, ObjectId oldCommit, ObjectId commit, string message, string refName)
		{
			AppendOneRecord(oldCommit, commit, null, message, repo, refName);
		}
	}
}