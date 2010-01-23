/*
 * Copyright (C) 2009, Rolenun <rolenun@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using GitSharp.Platform;

namespace GitSharp.Core.Platform
{
    public static class Platform1
    {
        enum GitPlatformID
        {
          Win32S = PlatformID.Win32S,
          Win32Windows = PlatformID.Win32Windows,
          Win32NT = PlatformID.Win32NT,
          WinCE = PlatformID.WinCE,
          Unix = PlatformID.Unix,
          Xbox,
          MacOSX,
        }
        
	
        public static PlatformObject Load()
        {
           System.OperatingSystem os = Environment.OSVersion;
           GitPlatformID pid = (GitPlatformID)os.Platform;
            PlatformObject obj;
			
            switch (pid)
            {
               case GitPlatformID.Unix:
                    obj = GitSharp.Platform.OSS.Linux.Load();
                    break;
               case GitPlatformID.MacOSX:
                    obj = GitSharp.Platform.Macintosh.Mac.Load();
                    break;
               case GitPlatformID.Win32NT:
               case GitPlatformID.Win32S:
               case GitPlatformID.Win32Windows:
               case GitPlatformID.WinCE:
                    obj = GitSharp.Platform.Windows.Win32.Load();
                    break;
                default:
                    throw new NotImplementedException();
            }
		
            return obj;
        }
		
        public static bool IsHardlinkSupported()
        {
            System.OperatingSystem os = Environment.OSVersion;
            GitPlatformID pid = (GitPlatformID)os.Platform;
            bool isSupported = false;
			
            switch (pid)
            {
               case GitPlatformID.Unix:
                    isSupported = GitSharp.Platform.OSS.Linux.IsHardlinkSupported();
                    break;
               case GitPlatformID.MacOSX:
                    isSupported = GitSharp.Platform.Macintosh.Mac.IsHardlinkSupported();
                    break;
                case GitPlatformID.Win32NT:
                case GitPlatformID.Win32S:
                case GitPlatformID.Win32Windows:
                case GitPlatformID.WinCE:
                    isSupported = GitSharp.Platform.Windows.Win32.IsHardlinkSupported();
                    break;
                default:
                    throw new NotImplementedException();
            }
		
            return isSupported;
        }
	
        public static bool IsSymlinkSupported()
        {
            System.OperatingSystem os = Environment.OSVersion;
            GitPlatformID pid = (GitPlatformID)os.Platform;
            bool isSupported = false;
		
            switch (pid)
            {
               case GitPlatformID.Unix:
                    isSupported = GitSharp.Platform.OSS.Linux.IsSymlinkSupported();
                    break;
               case GitPlatformID.MacOSX:
                    isSupported = GitSharp.Platform.Macintosh.Mac.IsSymlinkSupported();
                    break;
               case GitPlatformID.Win32NT:
               case GitPlatformID.Win32S:
               case GitPlatformID.Win32Windows:
               case GitPlatformID.WinCE:
                    isSupported = GitSharp.Platform.Windows.Win32.IsSymlinkSupported();
                    break;
                default:
                    throw new NotImplementedException();
            }
		
            return isSupported;
        }
	
        public static bool CreateSymlink(string symlinkFilename, string existingFilename, bool isSymlinkDirectory)
        {
            System.OperatingSystem os = Environment.OSVersion;
            GitPlatformID pid = (GitPlatformID)os.Platform;
            bool success = false;
			
            switch (pid)
            {
               case GitPlatformID.Unix:
                    success = GitSharp.Platform.OSS.Linux.CreateSymlink(symlinkFilename, existingFilename, isSymlinkDirectory);
                    break;
               case GitPlatformID.MacOSX:
                    success = GitSharp.Platform.Macintosh.Mac.CreateSymlink(symlinkFilename, existingFilename, isSymlinkDirectory);
                    break;
               case GitPlatformID.Win32NT:
               case GitPlatformID.Win32S:
               case GitPlatformID.Win32Windows:
               case GitPlatformID.WinCE:
                    success = GitSharp.Platform.Windows.Win32.CreateSymlink(symlinkFilename, existingFilename, isSymlinkDirectory);
                    break;
                default:
                    throw new NotImplementedException();
            }
		
            return success;
        }
	
        public static bool CreateHardlink(string hardlinkFilename, string exisitingFilename)
        {
            System.OperatingSystem os = Environment.OSVersion;
            GitPlatformID pid = (GitPlatformID)os.Platform;
            bool success = false;
		
            switch (pid)
            {
               case GitPlatformID.Unix:
                    success = GitSharp.Platform.OSS.Linux.CreateHardlink(hardlinkFilename, exisitingFilename);
                    break;
               case GitPlatformID.MacOSX:
                    success = GitSharp.Platform.Macintosh.Mac.CreateHardlink(hardlinkFilename, exisitingFilename);
                    break;
               case GitPlatformID.Win32NT:
               case GitPlatformID.Win32S:
               case GitPlatformID.Win32Windows:
               case GitPlatformID.WinCE:
                    success = GitSharp.Platform.Windows.Win32.CreateHardlink(hardlinkFilename, exisitingFilename);
                    break;
                default:
                    throw new NotImplementedException();
            }
		
            return success;
        }
    }
}