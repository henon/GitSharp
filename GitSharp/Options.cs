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
                Assembly assembly = Assembly.Load("GitSharp");

                Version version = assembly.GetName().Version;
                if (version == null)
                    return null;

                object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    // No AssemblyProduct attribute to parse, no commit hash to extract
                    return version.ToString();
                }

                string commitHash = ExtractCommitHashFrom(((AssemblyProductAttribute) attributes[0]).Product);
                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, commitHash);
            }
        }

        private static string ExtractCommitHashFrom(string product)
        {
            // TODO: Maybe should we switch to a regEx capture ?
            string[] parts = product.Split(new[] {'['});
            return parts[1].TrimEnd(']');
        }
    }
}
