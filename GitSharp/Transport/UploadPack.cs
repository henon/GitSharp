/*
 * Copyright (C) 2008, Google Inc.
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
using System.IO;
using System.Text;
using GitSharp.Exceptions;
using GitSharp.RevWalk;
using GitSharp.Util;

namespace GitSharp.Transport
{
	public class UploadPack
	{
		private const string OptionIncludeTag = BasePackFetchConnection.OPTION_INCLUDE_TAG;
		private const string OptionMultiAck = BasePackFetchConnection.OPTION_MULTI_ACK;
		private const string OptionThinPack = BasePackFetchConnection.OPTION_THIN_PACK;
		private const string OptionSideBand = BasePackFetchConnection.OPTION_SIDE_BAND;
		private const string OptionSideBand64K = BasePackFetchConnection.OPTION_SIDE_BAND_64K;
		private const string OptionOfsDelta = BasePackFetchConnection.OPTION_OFS_DELTA;
		private const string OptionNoProgress = BasePackFetchConnection.OPTION_NO_PROGRESS;

		private readonly Repository _db;
		private readonly RevWalk.RevWalk _walk;
		private Stream _stream;
		private PacketLineIn _pckIn;
		private PacketLineOut _pckOut;
		private bool _multiAck;
		private Dictionary<string, Ref> _refs;

		private readonly List<string> _options;
		private readonly List<RevObject> _wantAll;
		private readonly List<RevCommit> _wantCommits;
		private readonly List<RevObject> _commonBase;

		private readonly RevFlag ADVERTISED;
		private readonly RevFlag WANT;
		private readonly RevFlag PEER_HAS;
		private readonly RevFlag COMMON;
		private readonly RevFlagSet SAVE;

		public UploadPack(Repository copyFrom)
		{
			_options = new List<string>();
			_wantAll = new List<RevObject>();
			_wantCommits = new List<RevCommit>();
			_commonBase = new List<RevObject>();

			_db = copyFrom;
			_walk = new RevWalk.RevWalk(_db);

			ADVERTISED = _walk.newFlag("ADVERTISED");
			WANT = _walk.newFlag("WANT");
			PEER_HAS = _walk.newFlag("PEER_HAS");
			COMMON = _walk.newFlag("COMMON");
			_walk.carry(PEER_HAS);

			SAVE = new RevFlagSet { ADVERTISED, WANT, PEER_HAS };
		}

		public Repository Repository
		{
			get { return _db; }
		}

		public RevWalk.RevWalk RevWalk
		{
			get { return _walk; }
		}

		public void Upload(Stream stream, Stream messages)
		{
			_stream = stream;
			_pckIn = new PacketLineIn(stream);
			_pckOut = new PacketLineOut(stream);
			Service();
		}

		private void Service()
		{
			SendAdvertisedRefs();
			RecvWants();
			if (_wantAll.Count == 0)
				return;
			_multiAck = _options.Contains(OptionMultiAck);
			Negotiate();
			SendPack();
		}

		private void SendAdvertisedRefs()
		{
			_refs = _db.getAllRefs();

			var m = new StringBuilder(100);
			var idtmp = new char[2 * Constants.OBJECT_ID_LENGTH];
			IEnumerator<Ref> i = RefComparator.Sort(_refs.Values).GetEnumerator();
			if (i.MoveNext())
			{
				Ref r = i.Current;
				RevObject o = SafeParseAny(r.ObjectId);
				if (o != null)
				{
					Advertise(m, idtmp, o, r.OriginalName);
					m.Append('\0');
					m.Append(' ');
					m.Append(OptionIncludeTag);
					m.Append(' ');
					m.Append(OptionMultiAck);
					m.Append(' ');
					m.Append(OptionOfsDelta);
					m.Append(' ');
					m.Append(OptionSideBand);
					m.Append(' ');
					m.Append(OptionSideBand64K);
					m.Append(' ');
					m.Append(OptionThinPack);
					m.Append(' ');
					m.Append(OptionNoProgress);
					m.Append(' ');
					WriteAdvertisedRef(m);
					if (o is RevTag)
						WriteAdvertisedTag(m, idtmp, o, r.Name);
				}
			}
			while (i.MoveNext())
			{
				Ref r = i.Current;
				RevObject o = SafeParseAny(r.ObjectId);
				if (o != null)
				{
					Advertise(m, idtmp, o, r.OriginalName);
					WriteAdvertisedRef(m);
					if (o is RevTag)
					{
						WriteAdvertisedTag(m, idtmp, o, r.Name);
					}
				}
			}
			_pckOut.End();
		}

		private RevObject SafeParseAny(AnyObjectId id)
		{
			try
			{
				return _walk.parseAny(id);
			}
			catch (IOException)
			{
				return null;
			}
		}

		private void Advertise(StringBuilder m, char[] idtmp, RevObject o, string name)
		{
			o.add(ADVERTISED);
			m.Length = 0;
			o.getId().CopyTo(idtmp, m);
			m.Append(' ');
			m.Append(name);
		}

		private void WriteAdvertisedRef(StringBuilder m)
		{
			m.Append('\n');
			_pckOut.WriteString(m.ToString());
		}

		private void WriteAdvertisedTag(StringBuilder m, char[] idtmp, RevObject tag, string name)
		{
			RevObject o = tag;
			while (o is RevTag)
			{
				try
				{
					_walk.parse(((RevTag)o).getObject());
				}
				catch (IOException)
				{
					return;
				}
				o = ((RevTag)o).getObject();
				o.add(ADVERTISED);
			}
			Advertise(m, idtmp, ((RevTag)tag).getObject(), name + "^{}");
			WriteAdvertisedRef(m);
		}

		private void RecvWants()
		{
			bool isFirst = true;
			for (; ; isFirst = false)
			{
				string line;
				try
				{
					line = _pckIn.ReadString();
				}
				catch (EndOfStreamException)
				{
					if (isFirst) break;
					throw;
				}

				if (line.Length == 0) break;
				if (!line.StartsWith("want ") || line.Length < 45)
				{
					throw new PackProtocolException("expected want; got " + line);
				}

				if (isFirst)
				{
					int sp = line.IndexOf(' ', 45);
					if (sp >= 0)
					{
						foreach (string c in line.Substring(sp + 1).Split(' '))
							_options.Add(c);
						line = line.Slice(0, sp);
					}
				}

				string name = line.Substring(5);
				ObjectId id = ObjectId.FromString(name);
				RevObject o;
				try
				{
					o = _walk.parseAny(id);
				}
				catch (IOException e)
				{
					throw new PackProtocolException(name + " not valid", e);
				}
				if (!o.has(ADVERTISED))
				{
					throw new PackProtocolException(name + " not valid");
				}

				Want(o);
			}
		}

		private void Want(RevObject o)
		{
			if (o.has(WANT)) return;

			o.add(WANT);
			_wantAll.Add(o);

			if (o is RevCommit)
			{
				_wantCommits.Add((RevCommit)o);
			}
			else if (o is RevTag)
			{
				do
				{
					o = ((RevTag)o).getObject();
				} while (o is RevTag);

				if (o is RevCommit)
				{
					Want(o);
				}
			}
		}

		private void Negotiate()
		{
			ObjectId last = ObjectId.ZeroId;
			string lastName = string.Empty;

			while (true)
			{
				string line = _pckIn.ReadString();

				if (line.Length == 0)
				{
					if (_commonBase.Count == 0 || _multiAck)
					{
						_pckOut.WriteString("NAK\n");
					}
					_pckOut.Flush();
				}
				else if (line.StartsWith("have ") && line.Length == 45)
				{
					string name = line.Substring(5);
					ObjectId id = ObjectId.FromString(name);
					if (MatchHave(id))
					{
						if (_multiAck)
						{
							last = id;
							lastName = name;
							_pckOut.WriteString("ACK " + name + " continue\n");
						}
						else if (_commonBase.Count == 1)
						{
							_pckOut.WriteString("ACK " + name + "\n");
						}
					}
					else
					{
						if (_multiAck && OkToGiveUp())
						{
							_pckOut.WriteString("ACK " + name + " continue\n");
						}
					}
				}
				else if (line.Equals("done"))
				{
					if (_commonBase.Count == 0)
					{
						_pckOut.WriteString("NAK\n");
					}
					else if (_multiAck)
					{
						_pckOut.WriteString("ACK " + lastName + "\n");
					}

					break;
				}
				else
				{
					throw new PackProtocolException("expected have; got " + line);
				}
			}
		}

		private bool MatchHave(AnyObjectId id)
		{
			RevObject o;
			try
			{
				o = _walk.parseAny(id);
			}
			catch (IOException)
			{
				return false;
			}

			if (!o.has(PEER_HAS))
			{
				o.add(PEER_HAS);
				if (o is RevCommit)
				{
					((RevCommit)o).carry(PEER_HAS);
				}
				if (!o.has(COMMON))
				{
					o.add(COMMON);
					_commonBase.Add(o);
				}
			}
			return true;
		}

		private bool OkToGiveUp()
		{
			if (_commonBase.Count == 0) return false;

			try
			{
				for (var i = _wantCommits.GetEnumerator(); i.MoveNext(); )
				{
					RevCommit want = i.Current;
					if (WantSatisfied(want))
					{
						_wantCommits.Remove(want);
					}
				}
			}
			catch (IOException e)
			{
				throw new PackProtocolException("internal revision error", e);
			}

			return _wantCommits.Count == 0;
		}

		private bool WantSatisfied(RevCommit want)
		{
			_walk.resetRetain(SAVE);
			_walk.markStart(want);
			for (; ; )
			{
				RevCommit c = _walk.next();
				if (c == null) break;
				if (c.has(PEER_HAS))
				{
					if (!c.has(COMMON))
					{
						c.add(COMMON);
						_commonBase.Add(c);
					}
					return true;
				}
				c.dispose();
			}
			return false;
		}

		private void SendPack()
		{
			bool thin = _options.Contains(OptionThinPack);
			bool progress = !_options.Contains(OptionNoProgress);
			bool sideband = _options.Contains(OptionSideBand) || _options.Contains(OptionSideBand64K);

			IProgressMonitor pm = new NullProgressMonitor();
			Stream packOut = _stream;

			if (sideband)
			{
				int bufsz = SideBandOutputStream.SMALL_BUF;
				if (_options.Contains(OptionSideBand64K))
				{
					bufsz = SideBandOutputStream.MAX_BUF;
				}
				bufsz -= SideBandOutputStream.HDR_SIZE;

				packOut = new BufferedStream(new SideBandOutputStream(SideBandOutputStream.CH_DATA, _pckOut), bufsz);

				if (progress)
					pm = new SideBandProgressMonitor(_pckOut);
			}

			var pw = new PackWriter(_db, pm, new NullProgressMonitor())
						{
							DeltaBaseAsOffset = _options.Contains(OptionOfsDelta),
							Thin = thin
						};
			pw.preparePack(_wantAll, _commonBase);
			if (_options.Contains(OptionIncludeTag))
			{
				foreach (Ref r in _refs.Values)
				{
					RevObject o;
					try
					{
						o = _walk.parseAny(r.ObjectId);
					}
					catch (IOException)
					{
						continue;
					}
					if (o.has(WANT) || !(o is RevTag)) continue;

					var t = (RevTag)o;
					if (!pw.willInclude(t) && pw.willInclude(t.getObject()))
						pw.addObject(t);
				}
			}
			pw.writePack(packOut);

			if (sideband)
			{
				packOut.Flush();
				_pckOut.End();
			}
			else
			{
				_stream.Flush();
			}
		}
	}
}