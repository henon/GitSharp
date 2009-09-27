using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GitSharp.API.Commands
{
    public abstract class BaseCommand
    {
        /// <summary>
        /// Returns the value of the process' environment variable GIT_DIR
        /// </summary>
        protected string GIT_DIR
        {
            get
            {
                return System.Environment.GetEnvironmentVariable("GIT_DIR");
            }
        }

        /// <summary>
        /// This command's output stream. If not explicitly set, the command writes to Git.OutputStream out.
        /// </summary>
        public StreamWriter OutputStream
        {
            get {
                if (_output==null)
                    return Git.OutputStream;
                return _output;
            }
            set
            {
                _output = value;
            }
        }
        StreamWriter _output = null;

        //Repository _repo;

        //public virtual void Execute(Repository repo)
        //{
        //    _repo = repo;
        //}
    }
}
