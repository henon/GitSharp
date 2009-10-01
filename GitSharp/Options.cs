using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Git
{
    /// <summary>
    /// Represents git command line options such as --version.
    /// </summary>
    public static class Options
    {

        /// <summary>
        /// Returns the version of GitSharp.
        /// </summary>
        public static string Version
        {
            get
            {
                var assembly = Assembly.Load("GitSharp");
                var version = assembly.GetName().Version;
                if (version == null)
                    return null;
                return version.ToString();
            }
        }
    }
}
