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
using GitSharp.RevWalk;

namespace GitSharp.Transport
{

    public class BundleWriter
    {
        private readonly PackWriter packWriter;
        private readonly Dictionary<string, ObjectId> includeObjects;
        private readonly List<RevCommit> assumeCommits;

        public BundleWriter(Repository repo, ProgressMonitor monitor)
        {
            packWriter = new PackWriter(repo, monitor);
            includeObjects = new Dictionary<string, ObjectId>();
            assumeCommits = new List<RevCommit>();
        }

        public void include(string name, AnyObjectId id)
        {
            if (!Repository.IsValidRefName(name))
                throw new ArgumentException("Invalid ref name: " + name, "name");
            if (includeObjects.ContainsKey(name))
                throw new ApplicationException("Duplicate ref: " + name);
            includeObjects.Add(name, id.ToObjectId());
        }

        public void include(Ref r)
        {
            include(r.Name, r.ObjectId);
        }

        public void assume(RevCommit c)
        {
            if (c != null)
                assumeCommits.Add(c);
        }

        public void writeBundle(Stream os)
        {
            if (!(os is BufferedStream))
                os = new BufferedStream(os);

            List<ObjectId> inc = new List<ObjectId>();
            List<ObjectId> exc = new List<ObjectId>();
            inc.AddRange(includeObjects.Values);
            foreach (RevCommit r in assumeCommits)
                exc.Add(r.getId());
            packWriter.Thin = exc.Count > 0;
            packWriter.preparePack(inc, exc);

            StreamWriter w = new StreamWriter(os, Constants.CHARSET);
            w.Write(BundleFetchConnection.V2_BUNDLE_SIGNATURE);
            w.Write('\n');

            char[] tmp = new char[Constants.OBJECT_ID_LENGTH * 2];
            foreach (RevCommit a in assumeCommits)
            {
                w.Write('-');
                a.CopyTo(tmp, w);
                if (a.getRawBuffer() != null)
                {
                    w.Write(' ');
                    w.Write(a.getShortMessage());
                }
                w.Write('\n');
            }
            
            foreach (string k in includeObjects.Keys)
            {
                ObjectId v = includeObjects[k];
                v.CopyTo(tmp, w);
                w.Write(' ');
                w.Write(k);
                w.Write('\n');
            }

            w.Write('\n');
            w.Flush();
            packWriter.writePack(os);
        }
    }

}