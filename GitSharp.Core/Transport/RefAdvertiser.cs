/*
 * Copyright (C) 2008, 2009, Google Inc.
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
using System.Text;
using GitSharp.Core.RevWalk;

namespace GitSharp.Core.Transport
{
	public class RefAdvertiser : IDisposable
	{
		private readonly PacketLineOut _pckOut;
		private readonly RevWalk.RevWalk _walk;
		private readonly RevFlag ADVERTISED;
		private readonly StringBuilder _tmpLine;
		private readonly char[] _tmpId;
		private readonly List<string> _capabilities;
		private bool _derefTags;
		private bool _first;

		public RefAdvertiser(PacketLineOut o, RevWalk.RevWalk protoWalk, RevFlag advertisedFlag)
		{
			_tmpLine = new StringBuilder(100);
            _tmpId = new char[Constants.OBJECT_ID_STRING_LENGTH];
			_capabilities = new List<string>();
			_first = true;

			_pckOut = o;
			_walk = protoWalk;
			ADVERTISED = advertisedFlag;
		}

		public void setDerefTags(bool deref)
		{
			_derefTags = deref;
		}

		public void advertiseCapability(string name)
		{
			_capabilities.Add(name);
		}

		public void send(IEnumerable<Ref> refs)
		{
			foreach (Ref r in RefComparator.Sort(refs))
			{
				RevObject obj = parseAnyOrNull(r.ObjectId);
				if (obj != null)
				{
					advertiseAny(obj, r.OriginalName);
					RevTag rt = (obj as RevTag);
					if (_derefTags && rt != null)
					{
						advertiseTag(rt, r.OriginalName + "^{}");
					}
				}
			}
		}

		public void advertiseHave(AnyObjectId id)
		{
			RevObject obj = parseAnyOrNull(id);
			if (obj != null)
			{
				advertiseAnyOnce(obj, ".have");
			}

			RevTag rt = (obj as RevTag);
			if (rt != null)
			{
				advertiseAnyOnce(rt.getObject(), ".have");
			}
		}

		public void includeAdditionalHaves()
		{
			additionalHaves(_walk.Repository.ObjectDatabase);
		}

		private void additionalHaves(ObjectDatabase db)
		{
			AlternateRepositoryDatabase b = (db as AlternateRepositoryDatabase);
			if (b != null)
			{
				additionalHaves(b.getRepository());
			}

			foreach (ObjectDatabase alt in db.getAlternates())
			{
				additionalHaves(alt);
			}
		}

		private void additionalHaves(Repository alt)
		{
			foreach (Ref r in alt.getAllRefs().Values)
			{
				advertiseHave(r.ObjectId);
			}
		}

		public bool isEmpty()
		{
			return _first;
		}

		private RevObject parseAnyOrNull(AnyObjectId id)
		{
			if (id == null) return null;

			try
			{
				return _walk.parseAny(id);
			}
			catch (IOException)
			{
				return null;
			}
		}

		private void advertiseAnyOnce(RevObject obj, string refName)
		{
			if (!obj.has(ADVERTISED))
			{
				advertiseAny(obj, refName);
			}
		}

		private void advertiseAny(RevObject obj, string refName)
		{
			obj.add(ADVERTISED);
			advertiseId(obj, refName);
		}

		private void advertiseTag(RevTag tag, string refName)
		{
			RevTag o = (tag as RevTag);
			do
			{
				RevTag target = (o.getObject() as RevTag);
				try
				{
					_walk.parseHeaders(target);
				}
				catch (IOException)
				{
					return;
				}
				target.add(ADVERTISED);
				o = target;
			} while (o != null);
			advertiseAny(tag.getObject(), refName);
		}

		private void advertiseId(AnyObjectId id, string refName)
		{
			_tmpLine.Length = 0;
			id.CopyTo(_tmpId, _tmpLine);
			_tmpLine.Append(' ');
			_tmpLine.Append(refName);

			if (_first)
			{
				_first = false;
				if (_capabilities.Count > 0)
				{
					_tmpLine.Append('\0');
					foreach (string capName in _capabilities)
					{
						_tmpLine.Append(' ');
						_tmpLine.Append(capName);
					}
					_tmpLine.Append(' ');
				}
			}

			_tmpLine.Append('\n');
			_pckOut.WriteString(_tmpLine.ToString());
		}
		
		public void Dispose ()
		{
			_walk.Dispose();
			ADVERTISED.Dispose();
		}
		
	}
}