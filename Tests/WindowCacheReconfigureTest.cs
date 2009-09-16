/*
 * Copyright (C) 2009, Google Inc.
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
using GitSharp.Tests.Util;
using Xunit;

namespace GitSharp.Tests
{
    public class WindowCacheReconfigureTest : RepositoryTestCase
    {
        [Fact]
        public void testConfigureCache_PackedGitLimit_0()
        {
            var cfg = new WindowCacheConfig { PackedGitLimit = 0 };
            Assert.Throws<ArgumentException>(() => WindowCache.reconfigure(cfg));
        }

        [Fact]
        public void testConfigureCache_PackedGitWindowSize_0()
        {
            try
            {
                var cfg = new WindowCacheConfig { PackedGitWindowSize = 0 };
                WindowCache.reconfigure(cfg);
                Assert.False(true, "incorrectly permitted PackedGitWindowSize = 0");
            }
            catch (ArgumentException e)
            {
                Assert.Equal("Invalid window size", e.Message);
            }
        }

        [Fact]
        public void testConfigureCache_PackedGitWindowSize_512()
        {
            try
            {
                var cfg = new WindowCacheConfig { PackedGitWindowSize = 512 };
                WindowCache.reconfigure(cfg);
                Assert.False(true, "incorrectly permitted PackedGitWindowSize = 512");
            }
            catch (ArgumentException e)
            {
                Assert.Equal("Invalid window size", e.Message);
            }
        }

        [Fact]
        public void testConfigureCache_PackedGitWindowSize_4097()
        {
            try
            {
                var cfg = new WindowCacheConfig { PackedGitWindowSize = 4097 };
                WindowCache.reconfigure(cfg);
                Assert.False(true, "incorrectly permitted PackedGitWindowSize = 4097");
            }
            catch (ArgumentException e)
            {
                Assert.Equal("Window size must be power of 2", e.Message);
            }
        }

        [Fact]
        public void testConfigureCache_PackedGitOpenFiles_0()
        {
            try
            {
                var cfg = new WindowCacheConfig { PackedGitOpenFiles = 0 };
                WindowCache.reconfigure(cfg);
                Assert.False(true, "incorrectly permitted PackedGitOpenFiles = 0");
            }
            catch (ArgumentException e)
            {
                Assert.Equal("Open files must be >= 1", e.Message);
            }
        }

        [Fact]
        public void testConfigureCache_PackedGitWindowSizeAbovePackedGitLimit()
        {
            try
            {
                var cfg = new WindowCacheConfig { PackedGitLimit = 1024, PackedGitWindowSize = 8192 };
                WindowCache.reconfigure(cfg);
                Assert.False(true, "incorrectly permitted PackedGitWindowSize > PackedGitLimit");
            }
            catch (ArgumentException e)
            {
                Assert.Equal("Window size must be < limit", e.Message);
            }
        }

        [Fact]
        public void testConfigureCache_Limits1()
        {
            // This test is just to force coverage over some lower bounds for
            // the table. We don't want the table to wind up with too small
            // of a size. This is highly dependent upon the table allocation
            // algorithm actually implemented in WindowCache.
            //
            var cfg = new WindowCacheConfig { PackedGitLimit = 6 * 4096 / 5, PackedGitWindowSize = 4096 };
            WindowCache.reconfigure(cfg);
        }
    }
}