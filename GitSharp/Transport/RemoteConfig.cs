/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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

using System.Collections.Generic;

namespace GitSharp.Transport
{
    /**
 * A remembered remote repository, including URLs and RefSpecs.
 * <p>
 * A remote configuration remembers one or more URLs for a frequently accessed
 * remote repository as well as zero or more fetch and push specifications
 * describing how refs should be transferred between this repository and the
 * remote repository.
 */
    public class RemoteConfig
    {
        private const string SECTION = "remote";
        private const string KEY_URL = "url";
        private const string KEY_FETCH = "fetch";
        private const string KEY_PUSH = "push";
        private const string KEY_UPLOADPACK = "uploadpack";
        private const string KEY_RECEIVEPACK = "receivepack";
        private const string KEY_TAGOPT = "tagopt";
        private const string KEY_MIRROR = "mirror";
        private const bool DEFAULT_MIRROR = false;

        /** Default value for {@link #getUploadPack()} if not specified. */
        public const string DEFAULT_UPLOAD_PACK = "git-upload-pack";

        /** Default value for {@link #getReceivePack()} if not specified. */
        public const string DEFAULT_RECEIVE_PACK = "git-receive-pack";

        /**
         * Parse all remote blocks in an existing configuration file, looking for
         * remotes configuration.
         *
         * @param rc
         *            the existing configuration to get the remote settings from.
         *            The configuration must already be loaded into memory.
         * @return all remotes configurations existing in provided repository
         *         configuration. Returned configurations are ordered
         *         lexicographically by names.
         * @throws URISyntaxException
         *             one of the URIs within the remote's configuration is invalid.
         */
        public static List<RemoteConfig> getAllRemoteConfigs(RepositoryConfig rc)
        {
            List<string> names = new List<string>(rc.GetSubsections(SECTION));
            names.Sort();

            List<RemoteConfig> result = new List<RemoteConfig>(names.Count);
            foreach (string name in names)
            {
                result.Add(new RemoteConfig(rc, name));
            }
            return result;
        }

        public string Name { get; private set; }
        public List<URIish> URIs { get; private set; }
        public List<RefSpec> Fetch { get; private set; }
        public List<RefSpec> Push { get; private set; }
        public string UploadPack { get; private set; }
        public string ReceivePack { get; private set; }
        public TagOpt TagOpt { get; private set; }
        public bool Mirror { get; set; }

        public RemoteConfig(RepositoryConfig rc, string remoteName)
        {
            Name = remoteName;

            string[] vlst;
            string val;

            vlst = rc.GetStringList(SECTION, Name, KEY_URL);
            URIs = new List<URIish>(vlst.Length);
            foreach (string s in vlst)
                URIs.Add(new URIish(s));

            vlst = rc.GetStringList(SECTION, Name, KEY_FETCH);
            Fetch = new List<RefSpec>(vlst.Length);
            foreach (string s in vlst)
                Fetch.Add(new RefSpec(s));

            vlst = rc.GetStringList(SECTION, Name, KEY_PUSH);
            Push = new List<RefSpec>(vlst.Length);
            foreach (string s in vlst)
                Push.Add(new RefSpec(s));

            val = rc.GetString(SECTION, Name, KEY_UPLOADPACK) ?? DEFAULT_UPLOAD_PACK;
            UploadPack = val;

            val = rc.GetString(SECTION, Name, KEY_RECEIVEPACK) ?? DEFAULT_RECEIVE_PACK;
            ReceivePack = val;

            val = rc.GetString(SECTION, Name, KEY_TAGOPT);
            TagOpt = TagOpt.fromOption(val);
            Mirror = rc.GetBoolean(SECTION, Name, KEY_MIRROR, DEFAULT_MIRROR);
        }

        public void Update(RepositoryConfig rc)
        {

        }

        private void set(RepositoryConfig rc, string key, string currentValue, string defaultValue)
        {
            if (defaultValue.Equals(currentValue))
                unset(rc, key);
            else
                rc.SetString(SECTION, Name, key, currentValue);
        }

        private void set(RepositoryConfig rc, string key, bool currentValue, bool defaultValue)
        {
            if (defaultValue == currentValue)
                unset(rc, key);
            else
                rc.SetBoolean(SECTION, Name, key, currentValue);
        }

        private void unset(RepositoryConfig rc, string key)
        {
            rc.UnsetString(SECTION, Name, key);
        }

        public bool AddURI(URIish toAdd)
        {
            if (URIs.Contains(toAdd))
                return false;
            URIs.Add(toAdd);
            return true;
        }

        public bool RemoveURI(URIish toRemove)
        {
            return URIs.Remove(toRemove);
        }

        public bool AddFetchRefSpec(RefSpec s)
        {
            if (Fetch.Contains(s))
                return false;
            Fetch.Add(s);
            return true;
        }

        public void SetFetchRefSpecs(List<RefSpec> specs)
        {
            Fetch.Clear();
            Fetch.AddRange(specs);
        }

        public void SetPushRefSpecs(List<RefSpec> specs)
        {
            Push.Clear();
            Push.AddRange(specs);
        }

        public bool RemoveFetchRefSpec(RefSpec s)
        {
            return Fetch.Remove(s);
        }

        public bool AddPushRefSpec(RefSpec s)
        {
            if (Push.Contains(s))
                return false;
            Push.Add(s);
            return true;
        }

        public bool RemovePushRefSpec(RefSpec s)
        {
            return Push.Remove(s);
        }

        public void SetTagOpt(TagOpt option)
        {
            TagOpt = option ?? TagOpt.AUTO_FOLLOW;
        }
    }
}