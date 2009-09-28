using System;
using System.Collections.Generic;
using GitSharp.Core;

namespace GitSharp.Tests
{
    public class MockSystemReader : SystemReader
    {
        public readonly IDictionary<String, String> values = new Dictionary<String, String>();

        public FileBasedConfig userGitConfig;

        public MockSystemReader()
        {
            init(Constants.OS_USER_NAME_KEY);
            init(Constants.GIT_AUTHOR_NAME_KEY);
            init(Constants.GIT_AUTHOR_EMAIL_KEY);
            init(Constants.GIT_COMMITTER_NAME_KEY);
            init(Constants.GIT_COMMITTER_EMAIL_KEY);
            userGitConfig = new FileBasedConfig(null);
        }

        private void init(string n)
        {
            values.put(n, n);
        }

        public override string getenv(String variable)
        {
            return values.GetValue(variable);
        }

        public override string getProperty(String key)
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
    }
}