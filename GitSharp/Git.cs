using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using GitSharp.Commands;

namespace GitSharp
{
    /// <summary>
    /// The static class Git provides everything to interact with git itself, such as the command line interface commands, the git configuration or properties that are affecting git globally.
    /// </summary>
    public static class Git
    {

        #region Version


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


        #endregion

        #region Defaults for git commands


        /// <summary>
        /// Get or set the default output stream that all git commands are writing to. Per default this returns a StreamWriter wrapping the standard output stream.
        /// By setting your own Streamwriter one can capture the output of the commands.
        /// </summary>
        public static StreamWriter DefaultOutputStream
        {
            get
            {
                if (_output == null)
                {
                    _output = new StreamWriter(Console.OpenStandardOutput());
                    Console.SetOut(_output);
                }
                return _output;
            }
            set
            {
                _output = value;
            }
        }
        private static StreamWriter _output;

        /// <summary>
        /// Get or set the default git repository for all commands. A command can override this by
        /// setting it's own Repository property.
        /// 
        /// Note: Init and Clone do not respect Repository since they create a Repository as a result of Execute.
        /// </summary>
        public static Repository DefaultRepository { get; set; }

        /// <summary>
        /// Get or set the default git directory for all commands. A command can override this, however, 
        /// by setting its own GitDirectory property.
        /// </summary>
        public static string DefaultGitDirectory { get; set; }


        #endregion

        #region Clone

        /// <summary>
        /// Clone a repository and checkout the working directory.
        /// </summary>
        /// <param name="fromUrl"></param>
        /// <param name="toPath"></param>
        /// <returns></returns>
        public static Repository Clone(string fromUrl, string toPath)
        {
            bool bare = false;
            return Clone(fromUrl, toPath, bare);
        }

        /// <summary>
        /// Clone a repository and checkout the working directory only if bare == false
        /// </summary>
        /// <param name="fromUrl"></param>
        /// <param name="toPath"></param>
        /// <param name="bare"></param>
        /// <returns></returns>
        public static Repository Clone(string fromUrl, string toPath, bool bare)
        {
            CloneCommand cmd = new CloneCommand()
            {
                Source = fromUrl,
                GitDirectory = toPath,
                Bare = bare,
            };
            return Clone(cmd);
        }

        public static Repository Clone(CloneCommand command)
        {
            command.Execute();
            return command.Repository;
        }


        #endregion

        #region Init


        public static void Init(string path)
        {
            Repository.Init(path);
        }

        public static void Init(string path, bool bare)
        {
            Repository.Init(path, bare);
        }

        public static void Init(InitCommand command)
        {
            command.Execute();
        }


        #endregion


    }
}
