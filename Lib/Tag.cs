using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
  public class Tag {
      public class Constants
      {
          public static readonly string TagsPrefix = "refs/tags";
      }

	private Repository objdb;

	

	

	private byte[] raw;

	


	/**
	 * Construct a new, yet unnamed Tag.
	 *
	 * @param db
	 */
	public Tag( Repository db) {
		objdb = db;
	}

	/**
	 * Construct a Tag representing an existing with a known name referencing an known object.
	 * This could be either a simple or annotated tag.
	 *
	 * @param db {@link Repository}
	 * @param id target id.
	 * @param refName tag name or null
	 * @param raw data of an annotated tag.
	 */
	public Tag(Repository db, ObjectId id, String refName, byte[] raw) {
		objdb = db;
		if (raw != null) {
			TagId = id;
			this.Id = ObjectId.FromString(raw, 7);
		} else
			Id = id;
		if (refName != null && refName.StartsWith("refs/tags/"))
			refName = refName.Substring(10);
		TagName = refName;
		this.raw = raw;
	}

	/**
	 * @return tagger of a annotated tag or null
	 */
    private PersonIdent _author;
	public PersonIdent Author {
        get
        {
            decode();
            return _author;
        }
        set
        {
            _author = value;
        }
	}

	/**
	 * @return comment of an annotated tag, or null
	 */
    private String _message;
	public String Message {
        get
        {
            decode();
            return _message;
        }
        set
        {
            _message = value;
        }
	}

	private void decode() {
        throw new NotImplementedException();
		// FIXME: handle I/O errors
        //if (raw != null) {
        //    try {
        //        BufferedReader br = new BufferedReader(new InputStreamReader(
        //                new ByteArrayInputStream(raw)));
        //        String n = br.readLine();
        //        if (n == null || !n.startsWith("object ")) {
        //            throw new CorruptObjectException(tagId, "no object");
        //        }
        //        objId = Id.fromString(n.substring(7));
        //        n = br.readLine();
        //        if (n == null || !n.startsWith("type ")) {
        //            throw new CorruptObjectException(tagId, "no type");
        //        }
        //        type = n.substring("type ".length());
        //        n = br.readLine();

        //        if (n == null || !n.startsWith("tag ")) {
        //            throw new CorruptObjectException(tagId, "no tag name");
        //        }
        //        tag = n.substring("tag ".length());
        //        n = br.readLine();

        //        // We should see a "tagger" header here, but some repos have tags
        //        // without it.
        //        if (n == null)
        //            throw new CorruptObjectException(tagId, "no tagger header");

        //        if (n.length()>0)
        //            if (n.startsWith("tagger "))
        //                tagger = new PersonIdent(n.substring("tagger ".length()));
        //            else
        //                throw new CorruptObjectException(tagId, "no tagger/bad header");

        //        // Message should start with an empty line, but
        //        StringBuffer tempMessage = new StringBuffer();
        //        char[] readBuf = new char[2048];
        //        int readLen;
        //        while ((readLen = br.read(readBuf)) > 0) {
        //            tempMessage.append(readBuf, 0, readLen);
        //        }
        //        message = tempMessage.toString();
        //        if (message.startsWith("\n"))
        //            message = message.substring(1);
        //    } catch (IOException e) {
        //        e.printStackTrace();
        //    } finally {
        //        raw = null;
        //    }
        //}
	}

	
	/**
	 * Store a tag.
	 * If author, message or type is set make the tag an annotated tag.
	 *
	 * @throws IOException
	 */
	public void Create(){ //renamed from Tag
		if (this.TagId != null)
			throw new InvalidOperationException("exists " + this.TagId);
		ObjectId id;
		RefUpdate ru;

		if (_author!=null || _message!=null || _tagType!=null) {
			ObjectId tagid = new ObjectWriter(objdb).WriteTag(this);
			this.TagId = tagid;
			id = tagid;
		} else {
			id = this.Id;
		}

		ru = objdb.UpdateRef(Constants.TagsPrefix  + "/" + this.TagName);
		ru.SetNewObjectId(id);
		ru.SetRefLogMessage("tagged " + this.TagName, false);
		if (ru.ForceUpdate() == RefUpdate.Result.LockFailure)
			throw new GitException("Unable to lock tag " + this.TagName);
	}

	public String toString() {
		return "tag[" + this.TagName + this.TagType + this.Id + " " + this.Author + "]";
	}

    public ObjectId TagId { get; set; }


	/**
	 * @return creator of this tag.
	 */
	public PersonIdent Tagger{
        get
        {
            decode();
            return _author;
        }
        set
        {
            this._author = value;
        }
	}


	/**
	 * @return tag target type
	 */
    private String _tagType;
	public String TagType {
        get
        {
            decode();
            return _tagType;
        }
        set
        {
            this._tagType = value;
        }
	}

	
    public string TagName { get; set; }

	/**
	 * @return the SHA'1 of the object this tag refers to.
	 */

    public ObjectId Id { get; set; }

	/**
	 * Set the id of the object this tag refers to.
	 *
	 * @param objId
	 */
}
}
