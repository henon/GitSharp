/*
 * Copyright (C) 2009, Yann Simon <yann.simon.fr@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 *
 * - Neither the name of the Eclipse Foundation, Inc. nor the
 * contributors may be used to endorse or promote products derived from this
 * software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */


using System;
using System.Collections.Generic;
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Tests.GitSharp.Core.Util
{
    public class MockSystemReader : SystemReader
    {
        public readonly IDictionary<String, String> values = new Dictionary<String, String>();

        public FileBasedConfig userGitConfig;
        public PlatformType operatingSystem;

        public MockSystemReader()
        {
            init(Constants.OS_USER_NAME_KEY);
            init(Constants.GIT_AUTHOR_NAME_KEY);
            init(Constants.GIT_AUTHOR_EMAIL_KEY);
            init(Constants.GIT_COMMITTER_NAME_KEY);
            init(Constants.GIT_COMMITTER_EMAIL_KEY);
            userGitConfig = new MockFileBasedConfig(null);
            operatingSystem = SystemReader.getInstance().getOperatingSystem();
        }

        private void init(string n)
        {
            setProperty(n, n);
        }

        public void clearProperties()
        {
            values.Clear();
        }

        public void setProperty(string key, string value)
        {
            values.put(key, value);
        }

        public override string getenv(string variable)
        {
            return values.GetValue(variable);
        }

        public override string getProperty(string key)
        {
            return values.GetValue(key);
        }

        public override FileBasedConfig openUserConfig()
        {
            return userGitConfig;
        }

        public override string getHostname()
        {
            return "fake.host.example.com";
        }

        public override long getCurrentTime()
        {
            return 1250379778668L; // Sat Aug 15 20:12:58 GMT-03:30 2009
        }

        public override int getTimezone(long when)
        {
            TimeZoneInfo newFoundLandTimeZoneInfo = null;
            var expectedOffset = new TimeSpan(-3, -30, 0);
            foreach (TimeZoneInfo timeZoneInfo in TimeZoneInfo.GetSystemTimeZones())
            {
                if (timeZoneInfo.BaseUtcOffset != expectedOffset)
                {
                    continue;
                }

                newFoundLandTimeZoneInfo = timeZoneInfo;
                break;
            }

            if (newFoundLandTimeZoneInfo == null)
            {
                Assert.Fail("No -03:30 TimeZone has been found");
            }

            return (int)newFoundLandTimeZoneInfo.GetUtcOffset(when.MillisToUtcDateTime()).TotalMinutes;
        }

        public override FileBasedConfig getConfigFile(ConfigFileType fileType)
        {
            return SystemReader.getInstance().getConfigFile(fileType);
        }

        public override FileBasedConfig getConfigFile(string fileLocation)
        {
            return SystemReader.getInstance().getConfigFile(fileLocation);
        }
        
        public override PlatformType getOperatingSystem()
        {
            return operatingSystem;
        }
    }
}