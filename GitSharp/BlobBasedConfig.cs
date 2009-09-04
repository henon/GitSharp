/*
 * Copyright (C) 2009, JetBrains s.r.o.
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
using GitSharp.Util;

namespace GitSharp
{

    public class BlobBasedConfig : Config
    {
        public BlobBasedConfig(Config @base, byte[] blob)
            : base(@base)
        {
            fromText(RawParseUtils.decode(blob));
        }

        public BlobBasedConfig(Config @base, Repository r, ObjectId objectid)
            : base(@base)
        {
            ObjectLoader loader = r.OpenBlob(objectid);
            if (loader == null)
                throw new IOException("Blob not found: " + objectid);
            fromText(RawParseUtils.decode(loader.getBytes()));
        }

        public BlobBasedConfig(Config @base, Commit commit, string path)
            : base(@base)
        {
            ObjectId treeId = commit.TreeId;
            Repository r = commit.Repository;
            TreeWalk.TreeWalk tree = TreeWalk.TreeWalk.forPath(r, path, treeId);
            if (tree == null)
                throw new FileNotFoundException("Entry not found by path: " + path);
            ObjectId blobId = tree.getObjectId(0);
            ObjectLoader loader = tree.getRepository().OpenBlob(blobId);
            if (loader == null)
                throw new IOException("Blob not found: " + blobId + " for path: " + path);
            fromText(RawParseUtils.decode(loader.getBytes()));
        }
    }

}