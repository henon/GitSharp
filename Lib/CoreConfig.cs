using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Gitty.Lib
{
    [Complete]
    public class CoreConfig
    {
        public class Constants
        {
            public static readonly int DefaultCompression = Deflater.DEFAULT_COMPRESSION;
        }

        public int PackIndexVersion { get; private set; }
        public int Compression { get; private set; }

        public CoreConfig(RepositoryConfig repoConfig)
        {
            this.Compression = repoConfig.GetInt("core", "compression", Constants.DefaultCompression);
            this.PackIndexVersion = repoConfig.GetInt("pack", "indexversion", 0);
        }
    }
}
