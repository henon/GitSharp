/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using System;
using System.Collections.Generic;
using System.IO;
using GitSharp.Core.RevWalk;

namespace GitSharp.Core.Transport
{
    public class BundleWriter : IDisposable
    {
	    private readonly PackWriter _packWriter;
	    private readonly Dictionary<String, ObjectId> _include;
	    private readonly HashSet<RevCommit> _assume;

        /// <summary>
        /// Create a writer for a bundle.
        /// </summary>
        /// <param name="repo">repository where objects are stored.</param>
        /// <param name="monitor">operations progress monitor.</param>
        public BundleWriter(Repository repo, ProgressMonitor monitor) 
        {
		    _packWriter = new PackWriter(repo, monitor);
            _include = new Dictionary<String, ObjectId>();
		    _assume = new HashSet<RevCommit>();
	    }

        /// <summary>
        /// Include an object (and everything reachable from it) in the bundle.
        /// </summary>
        /// <param name="name">
        /// name the recipient can discover this object as from the
        /// bundle's list of advertised refs . The name must be a valid
        /// ref format and must not have already been included in this
        /// bundle writer.
        /// </param>
        /// <param name="id">
        /// object to pack. Multiple refs may point to the same object.
        /// </param>
	    public void include(String name, AnyObjectId id) 
        {
			if (id == null)
				throw new ArgumentNullException ("id");
		    if (!Repository.IsValidRefName(name))
		    {
		    	throw new ArgumentException("Invalid ref name: " + name);
		    }

		    if (_include.ContainsKey(name))
		    {
		    	throw new InvalidOperationException("Duplicate ref: " + name);
		    }

            _include[name] = id.ToObjectId();
	    }

        /// <summary>
        /// Include a single ref (a name/object pair) in the bundle.
        /// This is a utility function for:
        /// <code>include(r.getName(), r.getObjectId())</code>.
        /// </summary>
        /// <param name="r">the ref to include.</param>
	    public void include(Ref r) 
        {
			if (r == null)
				throw new ArgumentNullException ("r");
			
		    include(r.Name, r.ObjectId);
	    }

        /// <summary>
        /// Assume a commit is available on the recipient's side.
        /// 
        /// In order to fetch from a bundle the recipient must have any assumed
        /// commit. Each assumed commit is explicitly recorded in the bundle header
        /// to permit the recipient to validate it has these objects.
        /// </summary>
        /// <param name="c">
        /// the commit to assume being available. This commit should be
        /// parsed and not disposed in order to maximize the amount of
        /// debugging information available in the bundle stream.
        /// </param>
	    public void assume(RevCommit c)
        {
		    if (c != null)
		    {
		    	_assume.Add(c);
		    }
	    }

        private static void writeString(Stream os, string data)
        {
            byte[] val = Constants.CHARSET.GetBytes(data);
            os.Write(val, 0, val.Length);
        }

	    /**
	     * Generate and write the bundle to the output stream.
	     * <para />
	     * This method can only be called once per BundleWriter instance.
	     *
	     * @param os
	     *            the stream the bundle is written to. If the stream is not
	     *            buffered it will be buffered by the writer. Caller is
	     *            responsible for closing the stream.
	     * @throws IOException
	     *             an error occurred reading a local object's data to include in
	     *             the bundle, or writing compressed object data to the output
	     *             stream.
	     */
	    public void writeBundle(Stream os)
        {
		    if (!(os is BufferedStream))
		    {
		        os = new BufferedStream(os);
		    }

		    var inc = new HashSet<ObjectId>();
		    var exc = new HashSet<ObjectId>();

            foreach(ObjectId objectId in _include.Values)
            {
                inc.Add(objectId);
            }
		    
		    foreach(RevCommit r in _assume)
		    {
		        exc.Add(r.getId());
		    }

		    _packWriter.Thin = exc.Count > 0;
		    _packWriter.preparePack(inc, exc);

            //var w = new BinaryWriter(os);
	        //var w = new StreamWriter(os, Constants.CHARSET);
	        var w = os;
 
		    writeString(w, Constants.V2_BUNDLE_SIGNATURE);
            writeString(w, "\n");

	        char[] tmp = new char[Constants.OBJECT_ID_LENGTH*2];
		    foreach (RevCommit a in _assume) 
            {
                writeString(w, "-");
                a.CopyTo(tmp, Constants.CHARSET, w);
			    if (a.RawBuffer != null)
                {
                    writeString(w, " ");
                    writeString(w, a.getShortMessage());
			    }
                writeString(w, "\n");
		    }

            foreach(var entry in _include)
            {
                entry.Value.CopyTo(tmp, Constants.CHARSET, w);
                writeString(w, " ");
                writeString(w, entry.Key);
                writeString(w, "\n");
            }

            writeString(w, "\n");
		    w.Flush();

		    _packWriter.writePack(os);
	    }
		
		public void Dispose ()
		{
			_packWriter.Dispose();
		}
		
    }
}