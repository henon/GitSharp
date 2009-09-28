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
using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Transport;
using NDesk.Options;

namespace GitSharp.CLI
{

    [Command(common=true, usage = "Clone a repository into a new directory")]
    public class Clone : AbstractFetchCommand
    {
        private static Boolean isHelp = false;              //Complete
        private static Boolean isQuiet = false;
        private static Boolean isVerbose = false;
        private static Boolean isNoCheckout = false;        //Complete
        private static Boolean isCreateBareRepo = false;    //In progress
        private static Boolean isCreateMirrorRepo = false;  //More info needed
        private static Boolean isNoHardLinks = false;       //Unimplemented
        private static Boolean isShared = false;            //Unimplemented
        private static String templateRepo = "";            //More info needed
        private static String referenceRepo = "";           //More info needed
        private static String optionOrigin = "";            //Complete
        private static String uploadPack = "";              //More info needed
        private static Int32 depth = 0;                     //More info needed

        public override void Run(string[] args)
        {

            options = new CmdParserOptionSet()
            {
                { "h|help", "Display this help information. To see online help, use: git help <command>", v=>OfflineHelp()},
                { "q|quiet", "Be quiet", v=>IsQuiet()},
                { "v|verbose", "Be verbose", v=>IsVerbose()},
                { "n|no-checkout", "Don't create a checkout", v=> {isNoCheckout = true;}},
                { "bare", "Create a bare repository", v=> {isCreateBareRepo = true;}},
                { "naked", "Create a bare repository", v=> {isCreateBareRepo = true;}},
                { "mirror", "Create a mirror repository (implies bare)", v=> {isCreateMirrorRepo = true;}},
                { "l|local", "To clone from a local repository", v=>IsCloneLocal()},
                { "no-hardlinks", "(No-op) Do not use hard links, always copy", v=>IsNoHardLinks()},
                { "s|shared", "(No-op) Setup as shared repository", v=>IsShared() },
                { "template=", "{Path} the template repository",(string v) => templateRepo = v },
                { "reference=", "Reference {repo}sitory",(string v) => referenceRepo = v },
                { "o|origin=", "Use <{branch}> instead of 'origin' to track upstream",(string v) => optionOrigin = v },
                { "u|upload-pack=", "{Path} to git-upload-pack on the remote",(string v) => uploadPack = v },
                { "depth=", "Create a shallow clone of that {depth}",(int v) => depth = v },
            };

            try
            {
                List<String> arguments = ParseOptions(args);
                if (arguments.Count > 0)
                {
                    
                    if (isCreateMirrorRepo)
                        isCreateBareRepo = true;

                    if (isCreateBareRepo)
                    {
                        if (optionOrigin.Length > 0)
                            throw die("--bare and --origin " + optionOrigin + " options are incompatible.");
                        
                        isNoCheckout = true;
                    }
                    
                    if (optionOrigin.Length <= 0)
                        optionOrigin = "origin";

                    //Clone the specified repository
                    DoClone(arguments[0]);
                }
                else if (args.Length <= 0)
                {
                    throw die("fatal: You must specify a repository to clone.");
                }
                else
                {
                    OfflineHelp();
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void IsNoHardLinks()
        {
            isNoHardLinks = true;
            if (isNoHardLinks)
                throw die("The git clone --no-hardlinks option has not been implemented yet.");
        }

        private void IsShared()
        {
            isShared = true;
            if (isShared)
                throw die("The git clone --shared option has not been implemented yet.");
        }

        private void IsQuiet()
        {
            isQuiet = true;
            if (isQuiet)
                throw die("The git clone --quiet option has not been implemented yet.");
        }

        private void IsVerbose()
        {
            isVerbose = true;
            if (isVerbose)
                throw die("The git clone --verbose option has not been implemented yet.");
        }

        private void IsCloneLocal()
        {
            streamOut.WriteLine("The git clone --local command is essentially a no-op option.");
            streamOut.WriteLine("The git clone --local hardlinking support has not been implemented yet.");
        }

        private void DoClone(String repository)
        {
            URIish source = new URIish(repository);

            // guess a name
            string p = source.Path;
            while (p.EndsWith("/"))
                p = p.Substring(0, p.Length - 1);
            int s = p.LastIndexOf('/');
            if (s < 0)
                throw die("Cannot guess local name from " + source);
            string localName = p.Substring(s + 1);

            if (!isCreateBareRepo)
            {
                if (localName.EndsWith(".git"))
                    localName = localName.Substring(0, localName.Length - 4);

                if (gitdir == null)
                {
                    gitdir = Path.Combine(localName, ".git");
                }
            }
            else
            {
                gitdir = localName;
            }

            db = new Repository(new DirectoryInfo(gitdir));
            db.Create(isCreateBareRepo);
            db.Config.setBoolean("core", null, "bare", isCreateBareRepo);
            db.Config.save();

            streamOut.WriteLine("Initialized empty Git repository in " + (new DirectoryInfo(gitdir)).FullName);
            streamOut.Flush();
            if (!isCreateBareRepo)
            {
                saveRemote(source);
                FetchResult r = runFetch();
                Ref branch = guessHEAD(r);

                if (!isNoCheckout)
                    doCheckout(branch);
            }
            else
            {
                //Add description directory
                streamOut.WriteLine("Description directory still needs to be implemented.");
                //Add hooks directory
                streamOut.WriteLine("Hooks directory still needs to be implemented.");
                //Add info directory
                streamOut.WriteLine("Info directory still needs to be implemented.");
                //Add packed_refs directory
                streamOut.WriteLine("Packed_refs directory still needs to be implemented.");
            }
            
        }

        private void saveRemote(URIish uri)
        {
            RemoteConfig rc = new RemoteConfig(db.Config, optionOrigin);
            rc.AddURI(uri);
            rc.AddFetchRefSpec(new RefSpec().SetForce(true).SetSourceDestination(Constants.R_HEADS + "*",
                                                                                 Constants.R_REMOTES + optionOrigin + "/*"));
            rc.Update(db.Config);
            db.Config.save();
        }

        private FetchResult runFetch()
        {
            Transport.Transport tn = Transport.Transport.Open(db, optionOrigin);
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

        private static void OfflineHelp()
        {
            if (!isHelp)
            {
                isHelp = true;
                Console.WriteLine("usage: git clone [options] [--] <repo> [<dir>]");
                Console.WriteLine();
                options.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();
            }
        }
    }

}