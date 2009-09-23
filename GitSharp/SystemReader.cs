using System;
using System.IO;
using System.Net;
using GitSharp.Util;

namespace GitSharp
{
    public abstract class SystemReader
    {
        private static SystemReader _instance = new InternalSystemReader();

        /** @return the live instance to read system properties. */
        public static SystemReader getInstance()
        {
            return _instance;
        }

        /**
	     * @param newReader
	     *            the new instance to use when accessing properties.
	     */
        public static void setInstance(SystemReader newReader)
        {
            _instance = newReader;
        }

        /**
	     * Gets the hostname of the local host. If no hostname can be found, the
	     * hostname is set to the default value "localhost".
	     *
	     * @return the canonical hostname
	     */
        public abstract string getHostname();

        /**
         * @param variable system variable to read
         * @return value of the system variable
         */
        public abstract string getenv(string variable);
        
        /**
	     * @param key of the system property to read
    	 * @return value of the system property
	     */
        public abstract string getProperty(string key);

        /**
    	 * @return the git configuration found in the user home
    	 */
        public abstract FileBasedConfig openUserConfig();


    private class InternalSystemReader : SystemReader
    {
        public override string getenv(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }
        public override string getProperty(string key)
        {
            //[java] return  System.getProperty(key);
            throw new NotImplementedException();
        }
        public override FileBasedConfig openUserConfig()
        {
            DirectoryInfo home = FS.userHome();
            return new FileBasedConfig(new FileInfo(Path.Combine(home.FullName, ".gitconfig")));

            //string bd;

            //int p = (int)Environment.OSVersion.Platform;
            //if (p == (int)PlatformID.Unix || p == 6 /* MacOSX */ || p == 128)
            //    bd = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            //else
            //    bd = Environment.GetEnvironmentVariable("USERPROFILE");

            //return new RepositoryConfig(null, new FileInfo(Path.Combine(bd, ".gitconfig")));
        }

        public override string getHostname()
        {
            return Dns.GetHostName();
        }
    }

    }
}