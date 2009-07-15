/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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

using GitSharp.RevWalk;

namespace GitSharp.Transport
{
    // [caytchen] TODO: finish (BasePackConnection!)
    public class BasePackFetchConnection : BasePackConnection, IFetchConnection
    {
        private const int MAX_HAVES = 256;
        protected const int MIN_CLIENT_BUFFER = 2*32*46 + 8;
        public const string OPTION_INCLUDE_TAG = "include-tag";
        public const string OPTION_MULTI_ACK = "multi_ack"; // [caytchen] TODO: this correct?
        public const string OPTION_THIN_PACK = "thin-pack";
        public const string OPTION_SIDE_BAND = "side-band";
        public const string OPTION_SIDE_BAND_64K = "side-band-64k";
        public const string OPTION_OFS_DELTA = "ofs-delta";
        public const string OPTION_SHALLOW = "shallow";
        public const string OPTION_NO_PROGRESS = "no-progress";

        private RevWalk.RevWalk walk;
        private RevCommitList<RevCommit> reachableCommits;
        public readonly RevFlag REACHABLE;
        public readonly RevFlag COMMON;
        public readonly RevFlag ADVERTISED;
        private bool multiAck;
        private bool thinPack;
        private bool sideband;
        private bool includeTags;
        private bool allowOfsDelta;
        private string lockMessage;
        private PackLock packLock;
    }

}