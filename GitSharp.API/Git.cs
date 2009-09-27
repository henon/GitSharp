using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.API.Commands;
using System.IO;

namespace GitSharp.API
{
    public static class Git
    {
        /// <summary>
        /// Get or set the output stream that all git commands are writing to. Per default this returns a StreamWriter wrapping the standard output stream.
        /// </summary>
        public static StreamWriter OutputStream
        {
            get
            {
                if (_output == null)
                {
                    //Initialize the output stream for all console-based messages.
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

        public static void Init()
        {
            Repository.Init();
        }

        public static void Init(string path)
        {
            Repository.Init(path);
        }

        public static void Init(Init command)
        {
            command.Execute();
        }
    }
}
