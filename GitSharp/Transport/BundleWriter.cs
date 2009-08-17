using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitSharp.RevWalk;

namespace GitSharp.Transport
{
    public class BundleWriter
    {
	    private PackWriter _packWriter;

	    private Dictionary<String, ObjectId> _include;

	    private HashSet<RevCommit> _assume;

        /// <summary>
        /// Create a writer for a bundle.
        /// </summary>
        /// <param name="repo">repository where objects are stored.</param>
        /// <param name="monitor">operations progress monitor.</param>
        public BundleWriter(Repository repo, IProgressMonitor monitor) 
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
		    if (!Repository.IsValidRefName(name))
			    throw new ArgumentException("Invalid ref name: " + name);
		    if (_include.ContainsKey(name))
			    throw new InvalidOperationException("Duplicate ref: " + name);

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
			    _assume.Add(c);
	    }

	    /**
	     * Generate and write the bundle to the output stream.
	     * <p>
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

		    HashSet<ObjectId> inc = new HashSet<ObjectId>();
		    HashSet<ObjectId> exc = new HashSet<ObjectId>();

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

            StreamWriter w = new StreamWriter(os);

		    w.Write(Constants.V2_BUNDLE_SIGNATURE);
            w.Write('\n');

		    foreach (RevCommit a in _assume) 
            {
                w.Write('-');
			    a.CopyTo(w.BaseStream);
			    if (a.getRawBuffer() != null)
                {
                    w.Write(' ');
                    w.Write(a.getShortMessage());
			    }
                w.Write('\n');
		    }

            foreach(var entry in _include)
            {
                entry.Value.CopyTo(w.BaseStream);
                w.Write(' ');
                w.Write(entry.Key);
                w.Write('\n');
            }

            w.Write('\n');
		    w.Flush();

		    _packWriter.writePack(os);
	    }
    }
}
