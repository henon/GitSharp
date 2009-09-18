/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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

using GitSharp.Tests.Util;
using Xunit;

namespace GitSharp.Tests
{
    public class ParseRevTests : RepositoryTestCase
    {
        [StrictFactAttribute]
        public void testObjectId_existing()
        {
            Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0").Name);
        }

        [StrictFactAttribute]
        public void testObjectId_nonexisting()
        {
            Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c1", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c1").Name);
        }

        [StrictFactAttribute]
        public void testObjectId_objectid_implicit_firstparent()
        {
            Assert.Equal("6e1475206e57110fcef4b92320436c1e9872a322", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^").Name);
            Assert.Equal("1203b03dc816ccbb67773f28b3c19318654b0bc8", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^^").Name);
            Assert.Equal("bab66b48f836ed950c99134ef666436fb07a09a0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^^^").Name);
        }

        [StrictFactAttribute]
        public void testObjectId_objectid_self()
        {
            Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^0").Name);
            Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^0^0").Name);
            Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^0^0^0").Name);
        }

        [StrictFactAttribute]
        public void testObjectId_objectid_explicit_firstparent()
        {
            Assert.Equal("6e1475206e57110fcef4b92320436c1e9872a322", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^1").Name);
            Assert.Equal("1203b03dc816ccbb67773f28b3c19318654b0bc8", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^1^1").Name);
            Assert.Equal("bab66b48f836ed950c99134ef666436fb07a09a0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^1^1^1").Name);
        }

        [StrictFactAttribute]
        public void testObjectId_objectid_explicit_otherparents()
        {
            Assert.Equal("6e1475206e57110fcef4b92320436c1e9872a322", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^1").Name);
            Assert.Equal("f73b95671f326616d66b2afb3bdfcdbbce110b44", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^2").Name);
            Assert.Equal("d0114ab8ac326bab30e3a657a0397578c5a1af88", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^3").Name);
            Assert.Equal("d0114ab8ac326bab30e3a657a0397578c5a1af88", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^03").Name);
        }

        [StrictFactAttribute]
        public void testRef_refname()
        {
            Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", db.Resolve("master^0").Name);
            Assert.Equal("6e1475206e57110fcef4b92320436c1e9872a322", db.Resolve("master^").Name);
            Assert.Equal("6e1475206e57110fcef4b92320436c1e9872a322", db.Resolve("refs/heads/master^1").Name);
        }

        [StrictFactAttribute]
        public void testDistance()
        {
            Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0~0").Name);
            Assert.Equal("6e1475206e57110fcef4b92320436c1e9872a322", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0~1").Name);
            Assert.Equal("1203b03dc816ccbb67773f28b3c19318654b0bc8", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0~2").Name);
            Assert.Equal("bab66b48f836ed950c99134ef666436fb07a09a0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0~3").Name);
            Assert.Equal("bab66b48f836ed950c99134ef666436fb07a09a0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0~03").Name);
        }

        [StrictFactAttribute]
        public void testTree()
        {
            Assert.Equal("6020a3b8d5d636e549ccbd0c53e2764684bb3125", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^{tree}").Name);
            Assert.Equal("02ba32d3649e510002c21651936b7077aa75ffa9", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^^{tree}").Name);
        }

        [StrictFactAttribute]
        public void testHEAD()
        {
            Assert.Equal("6020a3b8d5d636e549ccbd0c53e2764684bb3125", db.Resolve("HEAD^{tree}").Name);
        }

        [StrictFactAttribute]
        public void testDerefCommit()
        {
            Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^{}").Name);
            Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^{commit}").Name);
            // double deref
            Assert.Equal("6020a3b8d5d636e549ccbd0c53e2764684bb3125", db.Resolve("49322bb17d3acc9146f98c97d078513228bbf3c0^{commit}^{tree}").Name);
        }

        [StrictFactAttribute]
        public void testDerefTag()
        {
            Assert.Equal("17768080a2318cd89bba4c8b87834401e2095703", db.Resolve("refs/tags/B").Name);
            Assert.Equal("d86a2aada2f5e7ccf6f11880bfb9ab404e8a8864", db.Resolve("refs/tags/B^{commit}").Name);
            Assert.Equal("032c063ce34486359e3ee3d4f9e5c225b9e1a4c2", db.Resolve("refs/tags/B10th").Name);
            Assert.Equal("d86a2aada2f5e7ccf6f11880bfb9ab404e8a8864", db.Resolve("refs/tags/B10th^{commit}").Name);
            Assert.Equal("d86a2aada2f5e7ccf6f11880bfb9ab404e8a8864", db.Resolve("refs/tags/B10th^{}").Name);
            Assert.Equal("d86a2aada2f5e7ccf6f11880bfb9ab404e8a8864", db.Resolve("refs/tags/B10th^0").Name);
            Assert.Equal("d86a2aada2f5e7ccf6f11880bfb9ab404e8a8864", db.Resolve("refs/tags/B10th~0").Name);
            Assert.Equal("0966a434eb1a025db6b71485ab63a3bfbea520b6", db.Resolve("refs/tags/B10th^").Name);
            Assert.Equal("0966a434eb1a025db6b71485ab63a3bfbea520b6", db.Resolve("refs/tags/B10th^1").Name);
            Assert.Equal("0966a434eb1a025db6b71485ab63a3bfbea520b6", db.Resolve("refs/tags/B10th~1").Name);
            Assert.Equal("2c349335b7f797072cf729c4f3bb0914ecb6dec9", db.Resolve("refs/tags/B10th~2").Name);
        }

        [StrictFactAttribute]
        public void testDerefBlob()
        {
            Assert.Equal("fd608fbe625a2b456d9f15c2b1dc41f252057dd7", db.Resolve("spearce-gpg-pub^{}").Name);
            Assert.Equal("fd608fbe625a2b456d9f15c2b1dc41f252057dd7", db.Resolve("spearce-gpg-pub^{blob}").Name);
            Assert.Equal("fd608fbe625a2b456d9f15c2b1dc41f252057dd7", db.Resolve("fd608fbe625a2b456d9f15c2b1dc41f252057dd7^{}").Name);
            Assert.Equal("fd608fbe625a2b456d9f15c2b1dc41f252057dd7", db.Resolve("fd608fbe625a2b456d9f15c2b1dc41f252057dd7^{blob}").Name);
        }

        [StrictFactAttribute]
        public void testDerefTree()
        {
            Assert.Equal("032c063ce34486359e3ee3d4f9e5c225b9e1a4c2", db.Resolve("refs/tags/B10th").Name);
            Assert.Equal("856ec208ae6cadac25a6d74f19b12bb27a24fe24", db.Resolve("032c063ce34486359e3ee3d4f9e5c225b9e1a4c2^{tree}").Name);
            Assert.Equal("856ec208ae6cadac25a6d74f19b12bb27a24fe24", db.Resolve("refs/tags/B10th^{tree}").Name);
        }
    }
}
