using System;
using System.IO;
using System.Net;
using GitSharp.Util;

namespace GitSharp
{

    public class GitSharpSystemReader : ISystemReader
    {
        public static ISystemReader Instance = new GitSharpSystemReader();

        public static void SetInstance(ISystemReader @new)
        {
            Instance = @new;
        }

        public string getHostname()
        {
            return Dns.GetHostName();
        }

        public string getenv(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
        public string getProperty(string key)
        {
            //[java] return  System.getProperty(key);
            throw new NotImplementedException();
        }
        public RepositoryConfig openUserConfig()
        {
            string bd;

            int p = (int)Environment.OSVersion.Platform;
            if (p == (int)PlatformID.Unix || p == 6 /* MacOSX */ || p == 128)
                bd = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            else
                bd = Environment.GetEnvironmentVariable("USERPROFILE");

            return new RepositoryConfig(null, new FileInfo(Path.Combine(bd, ".gitconfig")));
        }
    }

}