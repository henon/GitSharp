using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Core.Diff;

namespace GitSharp
{
    /// <summary>
    /// Patch represents a changeset formatted in diff notation. This class can create and apply patches
    /// </summary>
    public class Patch
    {
        public static Patch Create(string text_a, string text_b, PatchOptions options )
        {
            var differ = new DiffFormatter();
            differ.setContext(options.NumberOfContextLines);
            //FileHeader
            //differ.format();        
            throw new NotImplementedException();
        }

        public class PatchOptions
        {
            public int NumberOfContextLines { get; set; }
        }
    }
}
