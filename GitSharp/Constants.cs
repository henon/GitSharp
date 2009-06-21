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
using System.Globalization;
using System.Linq;
using System.Text;
using System;
using System.IO;
using GitSharp.Util;
using GitSharp.Exceptions;

namespace GitSharp
{
    public static class Constants
    {

        /// <summary>
        ///   Special name for the "HEAD" symbolic ref
        /// </summary>
        public static readonly string Head = "HEAD";

        public static readonly string Master = "master";

        public static class ObjectTypes
        {

            /// <summary>
            ///    Text string that identifies an object as an annotated tag.
            /// <summary>
            /// <remarks>
            ///   Annotated tags store a pointer to any other object, and an additional
            ///   message. It is most commonly used to record a stable release of the
            ///   project.
            /// </remarks>
            public const string Tag = "tag";

            /// <summary>
            ///    Text string that identifies an object as tree.
            /// <summary>
            /// <remarks>
            /// Trees attach object ids (hashes) to names and file
            /// modes. The normal use for a tree is to store a
            /// version of a directory and its contents.
            /// </remarks>
            public const string Tree = "tree";

            /// <summary>
            ///    Text string that identifies an object as a blob
            /// <summary>
            /// <remarks>
            /// Blobs store whole file revisions. They are used
            /// for any user file, as well as for symlinks. Blobs
            /// form the bulk of any project's storage space.
            /// </remarks>
            public const string Blob = "blob";

            /// <summary>
            ///    Text string that identifies an object as a commit.
            /// <summary>
            /// <remarks>
            /// Commits connect trees into a string of project
            /// histories, where each commit is an assertion that
            /// the best way to continue is to use this other tree
            /// (set of files).
            /// </remarks>
            public const string Commit = "commit";

            public static readonly byte[] EncodedCommit = new byte[] { (byte)'c', (byte)'o', (byte)'m', (byte)'m', (byte)'i', (byte)'t' };
            public static readonly byte[] EncodedTree = new byte[] { (byte)'t', (byte)'r', (byte)'e', (byte)'e' };
            public static readonly byte[] EncodedBlob = new byte[] { (byte)'b', (byte)'l', (byte)'o', (byte)'b' };
            public static readonly byte[] EncodedTag = new byte[] { (byte)'t', (byte)'a', (byte)'g' };

        }

        public static readonly Encoding Encoding = Encoding.UTF8;

        public static readonly string Refs = "refs/";
        public static readonly string RefsTags = Refs + "tags/";
        public static readonly string RefsHeads = Refs + "heads/";
        public static readonly string RefsRemotes = Refs + "remotes/";

        public static readonly string[] RefSearchPaths = { "", Refs, RefsTags, RefsHeads, RefsRemotes };

        /** Hash function used natively by Git for all objects. */
        private static string HASH_FUNCTION = "SHA-1";

        /** Length of an object hash. */
        public static int OBJECT_ID_LENGTH = 20;

        /** Special name for the "HEAD" symbolic-ref. */
        public static string HEAD = "HEAD";

        /**
         * Text string that identifies an object as a commit.
         * <p>
         * Commits connect trees into a string of project histories, where each
         * commit is an assertion that the best way to continue is to use this other
         * tree (set of files).
         */
        public static string TYPE_COMMIT = "commit";

        /**
         * Text string that identifies an object as a blob.
         * <p>
         * Blobs store whole file revisions. They are used for any user file, as
         * well as for symlinks. Blobs form the bulk of any project's storage space.
         */
        public static string TYPE_BLOB = "blob";

        /**
         * Text string that identifies an object as a tree.
         * <p>
         * Trees attach object ids (hashes) to names and file modes. The normal use
         * for a tree is to store a version of a directory and its contents.
         */
        public static string TYPE_TREE = "tree";

        /**
         * Text string that identifies an object as an annotated tag.
         * <p>
         * Annotated tags store a pointer to any other object, and an additional
         * message. It is most commonly used to record a stable release of the
         * project.
         */
        public static string TYPE_TAG = "tag";


        private static byte[] ENCODED_TYPE_COMMIT = encodeASCII(TYPE_COMMIT);

        private static byte[] ENCODED_TYPE_BLOB = encodeASCII(TYPE_BLOB);

        private static byte[] ENCODED_TYPE_TREE = encodeASCII(TYPE_TREE);

        private static byte[] ENCODED_TYPE_TAG = encodeASCII(TYPE_TAG);

        /** An unknown or invalid object type code. */
        public const int OBJ_BAD = -1;

        /**
         * In-pack object type: extended types.
         * <p>
         * This header code is reserved for future expansion. It is currently
         * undefined/unsupported.
         */
        public const int OBJ_EXT = 0;

        /**
         * In-pack object type: commit.
         * <p>
         * Indicates the associated object is a commit.
         * <p>
         * <b>This constant is fixed and is defined by the Git packfile format.</b>
         * 
         * @see #TYPE_COMMIT
         */
        public const int OBJ_COMMIT = 1;

        /**
         * In-pack object type: tree.
         * <p>
         * Indicates the associated object is a tree.
         * <p>
         * <b>This constant is fixed and is defined by the Git packfile format.</b>
         * 
         * @see #TYPE_BLOB
         */
        public const int OBJ_TREE = 2;

        /**
         * In-pack object type: blob.
         * <p>
         * Indicates the associated object is a blob.
         * <p>
         * <b>This constant is fixed and is defined by the Git packfile format.</b>
         * 
         * @see #TYPE_BLOB
         */
        public const int OBJ_BLOB = 3;

        /**
         * In-pack object type: annotated tag.
         * <p>
         * Indicates the associated object is an annotated tag.
         * <p>
         * <b>This constant is fixed and is defined by the Git packfile format.</b>
         * 
         * @see #TYPE_TAG
         */
        public const int OBJ_TAG = 4;

        /** In-pack object type: reserved for future use. */
        public const int OBJ_TYPE_5 = 5;

        /**
         * In-pack object type: offset delta
         * <p>
         * Objects stored with this type actually have a different type which must
         * be obtained from their delta base object. Delta objects store only the
         * changes needed to apply to the base object in order to recover the
         * original object.
         * <p>
         * An offset delta uses a negative offset from the start of this object to
         * refer to its delta base. The base object must exist in this packfile
         * (even in the case of a thin pack).
         * <p>
         * <b>This constant is fixed and is defined by the Git packfile format.</b>
         */
        public const int OBJ_OFS_DELTA = 6;

        /**
         * In-pack object type: reference delta
         * <p>
         * Objects stored with this type actually have a different type which must
         * be obtained from their delta base object. Delta objects store only the
         * changes needed to apply to the base object in order to recover the
         * original object.
         * <p>
         * A reference delta uses a full object id (hash) to reference the delta
         * base. The base object is allowed to be omitted from the packfile, but
         * only in the case of a thin pack being transferred over the network.
         * <p>
         * <b>This constant is fixed and is defined by the Git packfile format.</b>
         */
        public const int OBJ_REF_DELTA = 7;

        /**
         * Pack file signature that occurs at file header - identifies file as Git
         * packfile formatted.
         * <p>
         * <b>This constant is fixed and is defined by the Git packfile format.</b>
         */
        public static byte[] PACK_SIGNATURE = { (byte)'P', (byte)'A', (byte)'C', (byte)'K' };

        /** Native character encoding for commit messages, file names... */
        public static string CHARACTER_ENCODING = "UTF-8";

        /** Native character encoding for commit messages, file names... */
        public static Encoding CHARSET = Encoding.GetEncoding(CHARACTER_ENCODING);


        /** Default main branch name */
        public static string MASTER = "master";

        /** Prefix for branch refs */
        public static string R_HEADS = "refs/heads/";

        /** Prefix for remotes refs */
        public static string R_REMOTES = "refs/remotes/";

        /** Prefix for tag refs */
        public static string R_TAGS = "refs/tags/";

        /** Prefix for any ref */
        public static string R_REFS = "refs/";

        /** Logs folder name */
        public static string LOGS = "logs";

        /** Info refs folder */
        public static string INFO_REFS = "info/refs";

        /** Packed refs file */
        public static string PACKED_REFS = "packed-refs";

        /** The environment variable that contains the system user name */
        public static string OS_USER_NAME_KEY = "user.name";

        /** The environment variable that contains the author's name */
        public static string GIT_AUTHOR_NAME_KEY = "GIT_AUTHOR_NAME";

        /** The environment variable that contains the author's email */
        public static string GIT_AUTHOR_EMAIL_KEY = "GIT_AUTHOR_EMAIL";

        /** The environment variable that contains the commiter's name */
        public static string GIT_COMMITTER_NAME_KEY = "GIT_COMMITTER_NAME";

        /** The environment variable that contains the commiter's email */
        public static string GIT_COMMITTER_EMAIL_KEY = "GIT_COMMITTER_EMAIL";

        /** Default value for the user name if no other information is available */
        public static string UNKNOWN_USER_DEFAULT = "unknown-user";

        /** Beginning of the common "Signed-off-by: " commit message line */
        public static string SIGNED_OFF_BY_TAG = "Signed-off-by: ";


        /**
         * Create a new digest function for objects.
         * 
         * @return a new digest object.
         * @throws RuntimeException
         *             this Java virtual machine does not support the required hash
         *             function. Very unlikely given that JGit uses a hash function
         *             that is in the Java reference specification.
         */
        internal static MessageDigest newMessageDigest()
        {
            //try {
            //    return MessageDigest.getInstance(HASH_FUNCTION);
            //} catch (NoSuchAlgorithmException nsae) {
            //    throw new RuntimeException("Required hash function "
            //            + HASH_FUNCTION + " not available.", nsae);
            //}
            return new MessageDigest();
        }


        /**
         * Convert an OBJ_* type constant to a TYPE_* type constant.
         *
         * @param typeCode the type code, from a pack representation.
         * @return the canonical string name of this type.
         */
        public static string typeString(int typeCode)
        {
            switch (typeCode)
            {
                case OBJ_COMMIT:
                    return TYPE_COMMIT;
                case OBJ_TREE:
                    return TYPE_TREE;
                case OBJ_BLOB:
                    return TYPE_BLOB;
                case OBJ_TAG:
                    return TYPE_TAG;
                default:
                    throw new ArgumentException("Bad object type: " + typeCode);
            }
        }

        /**
         * Convert an OBJ_* type constant to an ASCII encoded string constant.
         * <p>
         * The ASCII encoded string is often the canonical representation of
         * the type within a loose object header, or within a tag header.
         *
         * @param typeCode the type code, from a pack representation.
         * @return the canonical ASCII encoded name of this type.
         */
        public static byte[] encodedTypeString(int typeCode)
        {
            switch (typeCode)
            {
                case OBJ_COMMIT:
                    return ENCODED_TYPE_COMMIT;
                case OBJ_TREE:
                    return ENCODED_TYPE_TREE;
                case OBJ_BLOB:
                    return ENCODED_TYPE_BLOB;
                case OBJ_TAG:
                    return ENCODED_TYPE_TAG;
                default:
                    throw new ArgumentException("Bad object type: " + typeCode);
            }
        }

        /**
         * Parse an encoded type string into a type constant.
         * 
         * @param id
         *            object id this type string came from; may be null if that is
         *            not known at the time the parse is occurring.
         * @param typeString
         *            string version of the type code.
         * @param endMark
         *            character immediately following the type string. Usually ' '
         *            (space) or '\n' (line feed).
         * @param offset
         *            position within <code>typeString</code> where the parse
         *            should start. Updated with the new position (just past
         *            <code>endMark</code> when the parse is successful.
         * @return a type code constant (one of {@link #OBJ_BLOB},
         *         {@link #OBJ_COMMIT}, {@link #OBJ_TAG}, {@link #OBJ_TREE}.
         * @throws CorruptObjectException
         *             there is no valid type identified by <code>typeString</code>.
         */
        public static int decodeTypeString(AnyObjectId id, byte[] typeString, byte endMark, MutableInteger offset)
        {
            try
            {
                int position = offset.value;
                switch (typeString[position])
                {
                    case (byte)'b':
                        if (typeString[position + 1] != (byte)'l'
                            || typeString[position + 2] != (byte)'o'
                            || typeString[position + 3] != (byte)'b'
                            || typeString[position + 4] != endMark)
                            throw new CorruptObjectException(id, "invalid type");
                        offset.value = position + 5;
                        return Constants.OBJ_BLOB;

                    case (byte)'c':
                        if (typeString[position + 1] != (byte)'o'
                                || typeString[position + 2] != (byte)'m'
                                || typeString[position + 3] != (byte)'m'
                                || typeString[position + 4] != (byte)'i'
                                || typeString[position + 5] != (byte)'t'
                                || typeString[position + 6] != endMark)
                            throw new CorruptObjectException(id, "invalid type");
                        offset.value = position + 7;
                        return Constants.OBJ_COMMIT;

                    case (byte)'t':
                        switch (typeString[position + 1])
                        {
                            case (byte)'a':
                                if (typeString[position + 2] != (byte)'g'
                                    || typeString[position + 3] != endMark)
                                    throw new CorruptObjectException(id, "invalid type");
                                offset.value = position + 4;
                                return Constants.OBJ_TAG;

                            case (byte)'r':
                                if (typeString[position + 2] != (byte)'e'
                                        || typeString[position + 3] != (byte)'e'
                                        || typeString[position + 4] != endMark)
                                    throw new CorruptObjectException(id, "invalid type");
                                offset.value = position + 5;
                                return Constants.OBJ_TREE;

                            default:
                                throw new CorruptObjectException(id, "invalid type");
                        }

                    default:
                        throw new CorruptObjectException(id, "invalid type");
                }
            }
            catch (IndexOutOfRangeException bad)
            {
                throw new CorruptObjectException(id, "invalid type");
            }
        }

        /**
         * Convert an integer into its decimal representation.
         * 
         * @param s
         *            the integer to convert.
         * @return a decimal representation of the input integer. The returned array
         *         is the smallest array that will hold the value.
         */
        public static byte[] encodeASCII(long s)
        {
            return encodeASCII(Convert.ToString(s));
        }


        /**
         * Convert a string to US-ASCII encoding.
         * 
         * @param s
         *            the string to convert. Must not contain any characters over
         *            127 (outside of 7-bit ASCII).
         * @return a byte array of the same length as the input string, holding the
         *         same characters, in the same order.
         * @throws ArgumentException
         *             the input string contains one or more characters outside of
         *             the 7-bit ASCII character space.
         */
        public static byte[] encodeASCII(string s)
        {
            byte[] r = new byte[s.Length];
            for (int k = r.Length - 1; k >= 0; k--)
            {
                char c = s[k];
                if (c > 127)
                    throw new ArgumentException("Not ASCII string: " + s);
                r[k] = (byte)c;
            }
            return r;
        }


        /**
         * Convert a string to a byte array in the standard character encoding.
         *
         * @param str
         *            the string to convert. May contain any Unicode characters.
         * @return a byte array representing the requested string, encoded using the
         *         default character encoding (UTF-8).
         * @see #CHARACTER_ENCODING
         */
        public static byte[] encode(string str)
        {
            //ByteBuffer bb = Constants.CHARSET.encode(str);
            //int len = bb.limit();
            //if (bb.hasArray() && bb.arrayOffset() == 0)
            //{
            //    byte[] arr = bb.array();
            //    if (arr.length == len)
            //        return arr;
            //}

            //byte[] arr = new byte[len];
            //bb.get(arr);
            //return arr;

            return  CHARSET.GetBytes(str);
        }


    }
}