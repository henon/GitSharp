/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Globalization;
using System.Text;

namespace Gitty.Lib
{
        public sealed class Constants {
		/// <summary>
		///   Prefix for branch refs
		/// </summary>
		public static readonly string HeadsPrefix = "refs/heads";

		/// <summary>
		///   Prefix for remote refs
		/// </summary>
		public static readonly string RemotesPrefix = "refs/remotes";

		/// <summary>
		///   Special name for the "HEAD" symbolic ref
		/// </summary>
		public static readonly string Head = "HEAD";

		public static readonly string Master = "master";

		/// <summary>
		///    Text string that identifies an object as an annotated tag.
		/// <summary>
		/// <remarks>
		///   Annotated tags store a pointer to any other object, and an additional
		///   message. It is most commonly used to record a stable release of the
		///   project.
		/// </summary>
		public static readonly string TypeTag = "tag";
 
		public static readonly Encoding Encoding = Encoding.UTF8;

		public static readonly string RefsSlash = "refs/";

		public static readonly string TagsPrefix = "refs/tags";
		
		public static readonly string TagsSlash = TagsPrefix + "/";

		public static readonly string HeadsSlash = HeadsPrefix + "/";

		public static readonly string[] RefSearchPaths = { "", RefsSlash, TagsSlash, HeadsSlash, RemotesPrefix + "/" };
       }
}