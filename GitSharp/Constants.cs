/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System.Text;
using System;
using GitSharp.Util;
using GitSharp.Exceptions;

namespace GitSharp
{
    public static class Constants
    {
        public const string V2_BUNDLE_SIGNATURE = "# v2 git bundle";

        public const string Master = "master";

        public static class ObjectTypes
        {
            /// <summary>
            /// Text string that identifies an object as an annotated tag.
            /// </summary>
            /// <remarks>
            /// Annotated tags store a pointer to any other object, and an additional
            /// message. It is most commonly used to record a stable release of the
            /// project.
            /// </remarks>
            public const string Tag = "tag";

            /// <summary>
            /// Text string that identifies an object as tree.
			/// </summary>
            /// <remarks>
            /// Trees attach object ids (hashes) to names and file
            /// modes. The normal use for a tree is to store a
            /// version of a directory and its contents.
            /// </remarks>
            public const string Tree = "tree";

            /// <summary>
            /// Text string that identifies an object as a blob
			/// </summary>
            /// <remarks>
            /// Blobs store whole file revisions. They are used
            /// for any user file, as well as for symlinks. Blobs
            /// form the bulk of any project's storage space.
            /// </remarks>
            public const string Blob = "blob";

            /// <summary>
            ///    Text string that identifies an object as a commit.
			/// </summary>
            /// <remarks>
            /// Commits connect trees into a string of project
            /// histories, where each commit is an assertion that
            /// the best way to continue is to use this other tree
            /// (set of files).
            /// </remarks>
            public const string Commit = "commit";

            public static readonly byte[] EncodedCommit = new[] { (byte)'c', (byte)'o', (byte)'m', (byte)'m', (byte)'i', (byte)'t' };
            public static readonly byte[] EncodedTree = new[] { (byte)'t', (byte)'r', (byte)'e', (byte)'e' };
            public static readonly byte[] EncodedBlob = new[] { (byte)'b', (byte)'l', (byte)'o', (byte)'b' };
            public static readonly byte[] EncodedTag = new[] { (byte)'t', (byte)'a', (byte)'g' };
        }

		public const string Refs = "refs/";
		public const string RefsTags = Refs + "tags/";
		public const string RefsHeads = Refs + "heads/";
		public const string RefsRemotes = Refs + "remotes/";

        public static readonly string[] RefSearchPaths = { string.Empty, Refs, RefsTags, RefsHeads, RefsRemotes };

		/*
        /// <summary>
		/// Hash function used natively by Git for all objects.
        /// </summary>
        private const string HASH_FUNCTION = "SHA-1"; // [henon] we don't use it anyway
		*/

        /// <summary>
		/// Length of an object hash.
        /// </summary>
        public const int OBJECT_ID_LENGTH = 20;
		public const int OBJECT_ID_STRING_LENGTH = OBJECT_ID_LENGTH * 2;

        /// <summary>
		/// Special name for the "HEAD" symbolic-ref.
        /// </summary>
        public const string HEAD = "HEAD";

        /// <summary>
        /// Text string that identifies an object as a commit.
		/// <para />
		/// Commits connect trees into a string of project histories, where each
		/// commit is an assertion that the best way to continue is to use this other
		/// tree (set of files).
        /// </summary>
        public const string TYPE_COMMIT = "commit";

        /// <summary>
        /// Text string that identifies an object as a blob.
		/// <para />
		/// Blobs store whole file revisions. They are used for any user file, as
		/// well as for symlinks. Blobs form the bulk of any project's storage space.
		/// </summary>
		public const string TYPE_BLOB = "blob";

        /// <summary>
        /// Text string that identifies an object as a tree.
		/// <para />
		/// Trees attach object ids (hashes) to names and file modes. The normal use
		/// for a tree is to store a version of a directory and its contents.
        /// </summary>
		public const string TYPE_TREE = "tree";

        /// <summary>
        /// Text string that identifies an object as an annotated tag.
		/// <para />
		/// Annotated tags store a pointer to any other object, and an additional
		/// message. It is most commonly used to record a stable release of the
		/// project.
        /// </summary>
        public static string TYPE_TAG = "tag";

        internal static readonly byte[] EncodedTypeCommit = encodeASCII(TYPE_COMMIT);
		internal static readonly byte[] EncodedTypeBlob = encodeASCII(TYPE_BLOB);
		internal static readonly byte[] EncodedTypeTree = encodeASCII(TYPE_TREE);
		internal static readonly byte[] EncodedTypeTag = encodeASCII(TYPE_TAG);

        /// <summary>
        /// Pack file signature that occurs at file header - identifies file as Git
		/// packfile formatted.
		/// <para />
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
        /// </summary>
        public static readonly byte[] PACK_SIGNATURE = { (byte)'P', (byte)'A', (byte)'C', (byte)'K' };
		
        private static readonly Encoding Charset = new UTF8Encoding(false, true);

        /// <summary>
        /// Native character encoding for commit messages, file names...
        /// </summary>
        public static Encoding CHARSET { get { return Charset; }
        }
  
        /// <summary>
		/// Default main branch name
        /// </summary>
		public const string MASTER = "master";

        /// <summary>
		/// Prefix for branch refs
        /// </summary>
		public const string R_HEADS = "refs/heads/";

        /// <summary>
		/// Prefix for remotes refs
        /// </summary>
		public const string R_REMOTES = "refs/remotes/";

        /// <summary>
		/// Prefix for tag refs
        /// </summary>
		public const string R_TAGS = "refs/tags/";

        /// <summary>
		/// Prefix for any ref
        /// </summary>
		public const string R_REFS = "refs/";

        /// <summary>
		/// Logs folder name
        /// </summary>
		public const string LOGS = "logs";

        /// <summary>
		/// Info refs folder
        /// </summary>
		public const string INFO_REFS = "info/refs";

        /// <summary>
		/// Packed refs file
        /// </summary>
		public const string PACKED_REFS = "packed-refs";

        /// <summary>
		/// The environment variable that contains the system user name
        /// </summary>
		public const string OS_USER_NAME_KEY = "user.name";

        /// <summary>
		/// The environment variable that contains the author's name
        /// </summary>
		public const string GIT_AUTHOR_NAME_KEY = "GIT_AUTHOR_NAME";

        /// <summary>
		/// The environment variable that contains the author's email
        /// </summary>
		public const string GIT_AUTHOR_EMAIL_KEY = "GIT_AUTHOR_EMAIL";

        /// <summary>
		/// The environment variable that contains the commiter's name
        /// </summary>
		public const string GIT_COMMITTER_NAME_KEY = "GIT_COMMITTER_NAME";

        /// <summary>
		/// The environment variable that contains the commiter's email
        /// </summary>
		public const string GIT_COMMITTER_EMAIL_KEY = "GIT_COMMITTER_EMAIL";

        /// <summary>
		/// Default value for the user name if no other information is available
        /// </summary>
		public const string UNKNOWN_USER_DEFAULT = "unknown-user";

        /// <summary>
		/// Beginning of the common "Signed-off-by: " commit message line
        /// </summary>
        public const string SIGNED_OFF_BY_TAG = "Signed-off-by: ";


        /// <summary>
        /// Create a new digest function for objects.
        /// </summary>
        /// <returns>A new digest object.</returns>
        public static MessageDigest newMessageDigest()
        {
            //try {
            //    return MessageDigest.getInstance(HASH_FUNCTION);
            //} catch (NoSuchAlgorithmException nsae) {
            //    throw new RuntimeException("Required hash function "
            //            + HASH_FUNCTION + " not available.", nsae);
            //}
            return new MessageDigest();
        }

        /// <summary>
        /// Convert an integer into its decimal representation.
        /// </summary>
        /// <param name="s">the integer to convert.</param>
        /// <returns>
        /// Decimal representation of the input integer. The returned array
		/// is the smallest array that will hold the value.
        /// </returns>
        public static byte[] encodeASCII(long s)
        {
            return encodeASCII(Convert.ToString(s));
        }

		/// <summary>
		/// Convert a string to US-ASCII encoding.       
		/// </summary>
		/// <param name="s">
		/// The string to convert. Must not contain any characters over
		/// 127 (outside of 7-bit ASCII).
		/// </param>
		/// <returns>
		/// A byte array of the same Length as the input string, holding the
		/// same characters, in the same order.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// The input string contains one or more characters outside of
		/// the 7-bit ASCII character space.
		/// </exception>
        public static byte[] encodeASCII(string s)
        {
            var r = new byte[s.Length];
            for (int k = r.Length - 1; k >= 0; k--)
            {
                char c = s[k];
                if (c > 127)
                {
                	throw new ArgumentException("Not ASCII string: " + s);
                }
                r[k] = (byte)c;
            }
            return r;
        }

		/// <summary>
		/// Convert a string to a byte array in UTF-8 character encoding.
		/// </summary>
		/// <param name="str">
		/// The string to convert. May contain any Unicode characters.
		/// </param>
		/// <returns>
		/// A byte array representing the requested string, encoded using the
		/// default character encoding (UTF-8).
		/// </returns>
        public static byte[] encode(string str)
        {
            return CHARSET.GetBytes(str);
        }

		internal static readonly long EpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
		public const long TicksPerSecond = 10000000L;
    	public const int TicksPerMillisecond = 1000000;
    	public const long TicksPerNanosecond = 1000000000L;

    	internal const string RepositoryFormatVersion = "0";
    }
}