/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using GitSharp.Transport;

namespace GitSharp.CLI
{

    public abstract class AbstractFetchCommand : TextBuiltin
    {
        protected bool verbose;

        protected void showFetchResult(Transport.Transport tn, FetchResult r)
        {
            bool shownURI = false;
            foreach (TrackingRefUpdate u in r.TrackingRefUpdates)
            {
                if (!verbose && u.Result == RefUpdate.RefUpdateResult.NoChange)
                    continue;

                char type = shortTypeOf(u.Result);
                string longType = longTypeOf(u);
                string src = AbbreviateRef(u.RemoteName, false);
                string dst = AbbreviateRef(u.LocalName, true);

                if (!shownURI)
                {
                    streamOut.Write("From ");
                    streamOut.WriteLine(tn.Uri);
                    shownURI = true;
                }

                streamOut.WriteLine(" " + type + " " + longType + " " + src + " -> " + dst);
            }
        }

        private string longTypeOf(TrackingRefUpdate u)
        {
            RefUpdate.RefUpdateResult r = u.Result;
            if (r == RefUpdate.RefUpdateResult.LockFailure)
                return "[lock fail]";
            if (r == RefUpdate.RefUpdateResult.IOFailure)
                return "[i/o error]";
            if (r == RefUpdate.RefUpdateResult.Rejected)
                return "[rejected]";
            if (ObjectId.ZeroId.Equals(u.NewObjectId))
                return "[deleted]";

            if (r == RefUpdate.RefUpdateResult.New)
            {
                if (u.RemoteName.StartsWith(Constants.R_HEADS))
                    return "[new branch]";
                if (u.LocalName.StartsWith(Constants.R_TAGS))
                    return "[new tag]";
                return "[new]";
            }

            if (r == RefUpdate.RefUpdateResult.Forced)
            {
                string aOld = u.OldObjectId.Abbreviate(db).name();
                string aNew = u.NewObjectId.Abbreviate(db).name();
                return aOld + "..." + aNew;
            }

            if (r == RefUpdate.RefUpdateResult.FastForward)
            {
                string aOld = u.OldObjectId.Abbreviate(db).name();
                string aNew = u.NewObjectId.Abbreviate(db).name();
                return aOld + ".." + aNew;
            }

            if (r == RefUpdate.RefUpdateResult.NoChange)
                return "[up to date]";

            return "[" + r + "]";
        }

        private static char shortTypeOf(RefUpdate.RefUpdateResult r)
        {
            switch (r)
            {
                case RefUpdate.RefUpdateResult.LockFailure:
                case RefUpdate.RefUpdateResult.IOFailure:
                case RefUpdate.RefUpdateResult.Rejected:
                    return '!';

                case RefUpdate.RefUpdateResult.New:
                    return '*';

                case RefUpdate.RefUpdateResult.Forced:
                    return '+';

                case RefUpdate.RefUpdateResult.NoChange:
                    return '=';

                default:
                    return ' ';
            }
        }
    }

}