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

using System;
using System.IO;
using System.Text;
using System.Threading;
using GitSharp.Util;

namespace GitSharp
{
	public static class RefLogWriter
	{
		internal static void append(RefUpdate u, String msg)
		{
			ObjectId oldId = u.OldObjectId;
			ObjectId newId = u.NewObjectId;
			Repository db = u.Repository;
			PersonIdent ident = u.RefLogIdent;

			AppendOneRecord(oldId, newId, ident, msg, db, u.Name);
		}

		internal static void append(RefRename refRename, String msg)
		{
			ObjectId id = refRename.ObjectId;
			Repository db = refRename.Repository;
			PersonIdent ident = refRename.RefLogIdent;
			AppendOneRecord(id, id, ident, msg, db, refRename.ToName);
		}

		internal static void renameTo(Repository db, RefUpdate from, RefUpdate to) 
		{
			var logdir = new DirectoryInfo(Path.Combine(db.Directory.FullName, Constants.LOGS));
			var reflogFrom = new FileInfo(Path.Combine(logdir.FullName, from.Name));
			if (!reflogFrom.Exists) return;

			var reflogTo = new FileInfo(Path.Combine(logdir.FullName, to.Name));
			var reflogToDir = reflogTo.Directory;
			var tmp = new FileInfo(Path.Combine(logdir.FullName, "tmp-renamed-log.." + Thread.CurrentThread.ManagedThreadId));
			if (!reflogFrom.RenameTo(tmp.FullName))
			{
				throw new IOException("Cannot rename " + reflogFrom + " to (" + tmp + ")" + reflogTo);
			}

			RefUpdate.DeleteEmptyDir(reflogFrom.Directory, RefUpdate.Count(from.Name, '/'));
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

		private static void AppendOneRecord(ObjectId oldId, ObjectId newId, PersonIdent ident, String msg, Repository db, String refName)
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
			var logdir = new DirectoryInfo(db.Directory + "/" + Constants.LOGS);
			var reflog = new DirectoryInfo(logdir + "/" + refName);
			var refdir = reflog.Parent;

			if (refdir != null)
			{
				refdir.Create();
				if (!refdir.Exists)
				{
					throw new IOException("Cannot create directory " + refdir);
				}
			}

			using (var @out = new FileStream(reflog.FullName, System.IO.FileMode.OpenOrCreate, FileAccess.Write))
			{
				try
				{
					@out.Write(rec, 0, rec.Length);
				}
				finally
				{
					@out.Close();
				}
			}
		}


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
		public static void WriteReflog(Repository repo, ObjectId oldCommit, ObjectId commit, string message, string refName)
		{
			string entry = BuildReflogString(repo, oldCommit, commit, message);

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
			
			var writer = new StreamWriter(reflogfile.OpenWrite());
			writer.WriteLine(entry);
			writer.Close();
		}

		private static string BuildReflogString(Repository repo, ObjectId oldCommit, ObjectId commit, string message)
		{
			var me = new PersonIdent(repo);
			string initial = string.Empty;

			if (oldCommit == null)
			{
				oldCommit = ObjectId.ZeroId;
				initial = " (initial)";
			}
			
			string s = oldCommit + " " + commit + " "
					+ me.ToExternalString() + "\t" + message + initial;

			return s;
		}
	}
}