/*
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

namespace GitSharp.Core
{
	/// <summary>
	/// Pairing of a name and the <seealso cref="ObjectId"/> it currently has.
	/// <para />
	/// A ref in Git is (more or less) a variable that holds a single object
	/// identifier. The object identifier can be any valid Git object (blob, tree,
	/// commit, annotated tag, ...).
	/// <para />
	/// The ref name has the attributes of the ref that was asked for as well as
	/// the ref it was resolved to for symbolic refs plus the object id it points
	/// to and (for tags) the peeled target object id, i.e. the tag resolved
	/// recursively until a non-tag object is referenced. 
	/// </summary>
	public class Ref
	{
		/// <summary>
		/// Location where a <see cref="Ref"/> is Stored.
		/// </summary>
		public sealed class Storage
		{		
			/// <summary>
			/// The ref does not exist yet, updating it may create it.
			/// <para />
			/// Creation is likely to choose <see cref="Loose"/> storage.
			/// </summary>
			public static readonly Storage New = new Storage("New", true, false);

			/// <summary>
			/// The ref is Stored in a file by itself.
			/// <para />
			/// Updating this ref affects only this ref.
			/// </summary>
			public static readonly Storage Loose = new Storage("Loose", true, false);

			/// <summary>
			/// The ref is Stored in the <code>packed-refs</code> file, with others.
			/// <para />
			/// Updating this ref requires rewriting the file, with perhaps many
			/// other refs being included at the same time.
			/// </summary>
			public static readonly Storage Packed = new Storage("Packed", false, true);

			/// <summary>
			/// The ref is both <see cref="Loose"/> and <see cref="Packed"/>.
			/// <para />
			/// Updating this ref requires only updating the loose file, but deletion
			/// requires updating both the loose file and the packed refs file.
			/// </summary>
			public static readonly Storage LoosePacked = new Storage("LoosePacked", true, true);

			/// <summary>
			/// The ref came from a network advertisement and storage is unknown.
			/// <para />
			/// This ref cannot be updated without Git-aware support on the remote
			/// side, as Git-aware code consolidate the remote refs and reported them
			/// to this process.
			/// </summary>
			public static readonly Storage Network = new Storage("Network", false, false);

			public bool IsLoose { get; private set; }
			public bool IsPacked { get; private set; }
			public string Name { get; private set; }

			private Storage(string name, bool loose, bool packed)
			{
				Name = name;
				IsLoose = loose;
				IsPacked = packed;
			}
		}

		/// <summary>
		/// Create a new ref pairing.
		/// </summary>
		/// <param name="storage">method used to store this ref.</param>
		/// <param name="origName">The name used to resolve this ref</param>
		/// <param name="refName">name of this ref.</param>
		/// <param name="id">
		/// Current value of the ref. May be null to indicate a ref that
		/// does not exist yet.
		/// </param>
		public Ref(Storage storage, string origName, string refName, ObjectId id)
			: this(storage, origName, refName, id, null, false)
		{
		}

		/// <summary>
		/// Create a new ref pairing.
		/// </summary>
		/// <param name="storage">method used to store this ref.</param>
		/// <param name="refName">name of this ref.</param>
		/// <param name="id">
		/// Current value of the ref. May be null to indicate a ref that
		/// does not exist yet.
		/// </param>
		public Ref(Storage storage, string refName, ObjectId id)
			: this(storage, refName, refName, id, null, false)
		{
		}

		/// <summary>
		/// Create a new ref pairing.
		/// </summary>
		/// <param name="storage">method used to store this ref.</param>
		/// <param name="refName">name of this ref.</param>
		/// <param name="id">
		/// Current value of the ref. May be null to indicate a ref that
		/// does not exist yet.
		/// </param>
		/// <param name="peeledObjectId">
		/// Peeled value of the ref's tag. May be null if this is not a
		/// tag or not yet peeled (in which case the next parameter should be null)
		/// </param>
		/// <param name="peeled">
		/// true if <paramref name="peeledObjectId"/> represents a the peeled value of the object
		/// </param>
		public Ref(Storage storage, string refName, ObjectId id, ObjectId peeledObjectId, bool peeled)
			: this(storage, refName, refName, id, peeledObjectId, peeled)
		{
		}

		/// <summary>
		/// Create a new ref pairing.
		/// </summary>
		/// <param name="storage">method used to store this ref.</param>
		/// <param name="origName">The name used to resolve this ref</param>
		/// <param name="refName">name of this ref.</param>
		/// <param name="id">
		/// Current value of the ref. May be null to indicate a ref that
		/// does not exist yet.
		/// </param>
		/// <param name="peeledObjectId">
		/// Peeled value of the ref's tag. May be null if this is not a
		/// tag or not yet peeled (in which case the next parameter should be null)
		/// </param>
		/// <param name="peeled">
		/// true if <paramref name="peeledObjectId"/> represents a the peeled value of the object
		/// </param>
		public Ref(Storage storage, string origName, string refName, ObjectId id, ObjectId peeledObjectId, bool peeled)
		{
			StorageFormat = storage;
			OriginalName = origName;
			Name = refName;
			ObjectId = id;
			PeeledObjectId = peeledObjectId;
			Peeled = peeled;
		}

		/// <summary>
		/// What this ref is called within the repository.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The originally resolved name
		/// </summary>
		public string OriginalName { get; private set; }

		/// <summary>
		/// How was this ref obtained?
		/// <para>
		/// The current storage model of a <see cref="Ref"/> may influence how the ref must be
		/// updated or deleted from the repository.
		/// </para>
		/// </summary>
		public Storage StorageFormat { get; private set; }

		/// <summary>
		/// Cached value of this ref.
		/// </summary>
		public ObjectId ObjectId { get; private set; }

		/// <summary>
		/// Cached value of <see cref="Ref"/> (the ref peeled to commit).
		/// <para>
		/// if this ref is an annotated tag the id of the commit (or tree or
		/// blob) that the annotated tag refers to; null if this ref does not
		/// refer to an annotated tag.
		/// </para>
		/// </summary>
		public ObjectId PeeledObjectId { get; private set; }

		/// <summary>
		/// Whether this <see cref="Ref"/> represents a peeled tag.
		/// </summary>
		public bool Peeled { get; private set; }

		public override string ToString()
		{
			return "Ref[" + (OriginalName == Name ? string.Empty : "(" + OriginalName + ")") + Name + "=" + ObjectId + "]";
		}
	}
}