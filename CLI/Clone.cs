/*
 * Copyright (C) 2008, Google Inc.
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

using System.Collections.Generic;
using System.IO;
using GitSharp.Transport;

namespace GitSharp.CLI
{

    [Command(common=true, usage = "Clone a repository into a new directory")]
    public class Clone : AbstractFetchCommand
    {
        private string remoteName = "origin";

        public override bool RequiresRepository()
        {
            return false;
        }

        public override void Run(string[] args)
        {
            if (args.Length == 0) return;
            
            URIish source = new URIish(args[0]);
            
            // guess a name
            string p = source.Path;
            while (p.EndsWith("/"))
                p = p.Substring(0, p.Length - 1);
            int s = p.LastIndexOf('/');
            if (s < 0)
                throw die("Cannot guess local name from " + source);
            string localName = p.Substring(s + 1);
            if (localName.EndsWith(".git"))
                localName = localName.Substring(0, localName.Length - 4);

            if (gitdir == null)
                gitdir = Path.Combine(localName, ".git");

            db = new Repository(new DirectoryInfo(gitdir));
            db.Create();
            db.Config.setBoolean("core", null, "bare", false);
            db.Config.save();

            streamOut.WriteLine("Initialized empty Git repository in " + (new DirectoryInfo(gitdir)).FullName);
            streamOut.Flush();

            saveRemote(source);
            FetchResult r = runFetch();
            Ref branch = guessHEAD(r);
            doCheckout(branch);
        }

        private void saveRemote(URIish uri)
        {
            RemoteConfig rc = new RemoteConfig(db.Config, remoteName);
            rc.AddURI(uri);
            rc.AddFetchRefSpec(new RefSpec().SetForce(true).SetSourceDestination(Constants.R_HEADS + "*",
                                                                                 Constants.R_REMOTES + remoteName + "/*"));
            rc.Update(db.Config);
            db.Config.save();
        }

        private FetchResult runFetch()
        {
            Transport.Transport tn = Transport.Transport.Open(db, remoteName);
            FetchResult r;
            try
            {
                r = tn.fetch(new TextProgressMonitor(streamOut), null);
            }
            finally
            {
                tn.close();
            }
            showFetchResult(tn, r);
            return r;
        }

        private static Ref guessHEAD(FetchResult result)
        {
            Ref idHEAD = result.GetAdvertisedRef(Constants.HEAD);
            List<Ref> availableRefs = new List<Ref>();
            Ref head = null;
            foreach (Ref r in result.AdvertisedRefs.Values)
            {
                string n = r.Name;
                if (!n.StartsWith(Constants.R_HEADS))
                    continue;
                availableRefs.Add(r);
                if (idHEAD == null || head != null)
                    continue;
                if (r.ObjectId.Equals(idHEAD.ObjectId))
                    head = r;
            }
            availableRefs.Sort(RefComparator.INSTANCE);
            if (idHEAD != null && head == null)
                head = idHEAD;
            return head;
        }

        private void doCheckout(Ref branch)
        {
            if (branch == null)
                throw die("Cannot checkout; no HEAD advertised by remote");
            if (!Constants.HEAD.Equals(branch.Name))
                db.WriteSymref(Constants.HEAD, branch.Name);

            GitSharp.Commit commit = db.MapCommit(branch.ObjectId);
            RefUpdate u = db.UpdateRef(Constants.HEAD);
            u.NewObjectId = commit.CommitId;
            u.ForceUpdate();

            GitIndex index = new GitIndex(db);
            Tree tree = commit.TreeEntry;

            WorkDirCheckout co = new WorkDirCheckout(db, db.WorkingDirectory, index, tree);
            co.checkout();
            index.write();
        }
    }

}