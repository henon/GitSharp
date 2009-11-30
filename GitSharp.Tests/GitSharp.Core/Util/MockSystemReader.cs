using System;
using System.Collections.Generic;
using GitSharp.Core;
using GitSharp.Core.Util;
using NUnit.Framework;

namespace GitSharp.Core.Tests.Util
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
            userGitConfig = new FileBasedConfig(null);
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

            return (int)newFoundLandTimeZoneInfo.GetUtcOffset(when.MillisToDateTime()).TotalMinutes;
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