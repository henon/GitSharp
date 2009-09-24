/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.IO;

namespace GitSharp.Util
{
    public abstract class SystemReader {
        private static SystemReader INSTANCE = new InternalSystemReader();
	
        /** @return the live instance to read system properties. */
        public static SystemReader getInstance() {
            return INSTANCE;
        }

        /**
	     * @param newReader
	     *            the new instance to use when accessing properties.
	     */
        public static void setInstance(SystemReader newReader) {
            INSTANCE = newReader;
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
            //TODO : [nulltoken] Why volatile ? 
            private volatile string hostname;

            public override string getenv(string variable) {
                throw new NotImplementedException();
                //return System.getenv(variable);
            }

            public override string getProperty(string key) {
                throw new NotImplementedException();
                //return System.getProperty(key);
            }

            public override FileBasedConfig openUserConfig() {
                DirectoryInfo home = FS.userHome();
                return new FileBasedConfig(new FileInfo(Path.Combine(home.FullName, ".gitconfig")));
            }

            public override string getHostname() {
                throw new NotImplementedException();
                //if (hostname == null) {
                //    try {
                //        InetAddress localMachine = InetAddress.getLocalHost();
                //        hostname = localMachine.getCanonicalHostName();
                //    } catch (UnknownHostException e) {
                //        // we do nothing
                //        hostname = "localhost";
                //    }
                //    assert hostname != null;
                //}
                //return hostname;
            }
        }
    }
}