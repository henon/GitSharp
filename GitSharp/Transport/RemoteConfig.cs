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

using System;
using System.Collections.Generic;

namespace GitSharp.Transport
{
	/// <summary>
	/// A remembered remote repository, including URLs and RefSpecs.
	/// <para />
	/// A remote configuration remembers one or more URLs for a frequently accessed
	/// remote repository as well as zero or more fetch and push specifications
	/// describing how refs should be transferred between this repository and the
	/// remote repository.
	/// </summary>
	public class RemoteConfig
	{
		private const string Section = "remote";
		private const string KeyUrl = "url";
		private const string KeyPushurl = "pushurl";
		private const string KeyFetch = "fetch";
		private const string KeyPush = "push";
		private const string KeyUploadpack = "uploadpack";
		private const string KeyReceivepack = "receivepack";
		private const string KeyTagopt = "tagopt";
		private const string KeyMirror = "mirror";
		private const string KeyTimeout = "timeout";
		private const bool DefaultMirror = false;

		/// <summary>
		/// Default value for {@link #getUploadPack()} if not specified.
		/// </summary>
		public const string DEFAULT_UPLOAD_PACK = "git-upload-pack";

		/// <summary>
		/// Default value for {@link #getReceivePack()} if not specified.
		/// </summary>
		public const string DEFAULT_RECEIVE_PACK = "git-receive-pack";

		/// <summary>
		/// Parse all remote blocks in an existing configuration file, looking for
		/// remotes configuration.
		/// </summary>
		/// <param name="rc">
		/// The existing configuration to get the remote settings from.
		/// The configuration must already be loaded into memory.
		/// </param>
		/// <returns>
		/// All remotes configurations existing in provided repository
		/// configuration. Returned configurations are ordered
		/// lexicographically by names.
		/// </returns>
		/// <exception cref="URISyntaxException">
		/// One of the URIs within the remote's configuration is invalid.
		/// </exception>
		public static List<RemoteConfig> GetAllRemoteConfigs(RepositoryConfig rc)
		{
			var names = new List<string>(rc.getSubsections(Section));
			names.Sort();

			var result = new List<RemoteConfig>(names.Count);
			foreach (string name in names)
			{
				result.Add(new RemoteConfig(rc, name));
			}

			return result;
		}

		public string Name { get; private set; }
		public List<URIish> URIs { get; private set; }
		public List<URIish> PushURIs { get; private set; }
		public List<RefSpec> Fetch { get; private set; }
		public List<RefSpec> Push { get; private set; }
		public string UploadPack { get; private set; }
		public string ReceivePack { get; private set; }
		public TagOpt TagOpt { get; private set; }
		public bool Mirror { get; set; }

		public RemoteConfig(Config rc, string remoteName)
		{
			Name = remoteName;

			string[] vlst = rc.getStringList(Section, Name, KeyUrl);
			URIs = new List<URIish>(vlst.Length);
			foreach (string s in vlst)
			{
				URIs.Add(new URIish(s));
			}

			vlst = rc.getStringList(Section, Name, KeyPushurl);
			PushURIs = new List<URIish>(vlst.Length);
			foreach (string s in vlst)
			{
				PushURIs.Add(new URIish(s));
			}

			vlst = rc.getStringList(Section, Name, KeyFetch);
			Fetch = new List<RefSpec>(vlst.Length);
			foreach (string s in vlst)
			{
				Fetch.Add(new RefSpec(s));
			}

			vlst = rc.getStringList(Section, Name, KeyPush);
			Push = new List<RefSpec>(vlst.Length);
			foreach (string s in vlst)
			{
				Push.Add(new RefSpec(s));
			}

			string val = rc.getString(Section, Name, KeyUploadpack) ?? DEFAULT_UPLOAD_PACK;
			UploadPack = val;

			val = rc.getString(Section, Name, KeyReceivepack) ?? DEFAULT_RECEIVE_PACK;
			ReceivePack = val;

			val = rc.getString(Section, Name, KeyTagopt);
			TagOpt = TagOpt.fromOption(val);
			Mirror = rc.getBoolean(Section, Name, KeyMirror, DefaultMirror);

			Timeout = rc.getInt(Section, Name, KeyTimeout, 0);
		}

		public void Update(RepositoryConfig rc)
		{
			var vlst = new List<string>();

			vlst.Clear();
			foreach (URIish u in URIs)
			{
				vlst.Add(u.ToPrivateString());
			}
			rc.setStringList(Section, Name, KeyUrl, vlst);

			vlst.Clear();
			foreach (URIish u in PushURIs)
				vlst.Add(u.ToPrivateString());
			rc.setStringList(Section, Name, KeyPushurl, vlst);

			vlst.Clear();
			foreach (RefSpec u in Fetch)
			{
				vlst.Add(u.ToString());
			}
			rc.setStringList(Section, Name, KeyFetch, vlst);

			vlst.Clear();
			foreach (RefSpec u in Push)
			{
				vlst.Add(u.ToString());
			}
			rc.setStringList(Section, Name, KeyPush, vlst);

			Set(rc, KeyUploadpack, UploadPack, DEFAULT_UPLOAD_PACK);
			Set(rc, KeyReceivepack, ReceivePack, DEFAULT_RECEIVE_PACK);
			Set(rc, KeyTagopt, TagOpt.Option, TagOpt.AUTO_FOLLOW.Option);
			Set(rc, KeyMirror, Mirror, DefaultMirror);
			Set(rc, KeyTimeout, Timeout, 0);
		}

		private void Set(Config rc, string key, string currentValue, IEquatable<string> defaultValue)
		{
			if (defaultValue.Equals(currentValue))
			{
				Unset(rc, key);
			}
			else
			{
				rc.setString(Section, Name, key, currentValue);
			}
		}

		private void Set(Config rc, string key, int currentValue, IEquatable<int> defaultValue)
		{
			if (defaultValue.Equals(currentValue))
			{
				Unset(rc, key);
			}
			else
			{
				rc.setInt(Section, Name, key, currentValue);
			}
		}

		private void Set(Config rc, string key, bool currentValue, bool defaultValue)
		{
			if (defaultValue == currentValue)
			{
				Unset(rc, key);
			}
			else
			{
				rc.setBoolean(Section, Name, key, currentValue);
			}
		}

		private void Unset(Config rc, string key)
		{
			rc.unset(Section, Name, key);
		}

		public bool AddURI(URIish toAdd)
		{
			if (URIs.Contains(toAdd)) return false;

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
			{
				return false;
			}

			Fetch.Add(s);

			return true;
		}

		public bool AddPushURI(URIish toAdd)
		{
			if (PushURIs.Contains(toAdd)) return false;

			PushURIs.Add(toAdd);
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

		public bool RemovePushURI(URIish toRemove)
		{
			return PushURIs.Remove(toRemove);
		}

		public bool RemoveFetchRefSpec(RefSpec s)
		{
			return Fetch.Remove(s);
		}

		public bool AddPushRefSpec(RefSpec s)
		{
			if (Push.Contains(s)) return false;

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

		public int Timeout { get; set; }
	}
}