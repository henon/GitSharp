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

using System.Collections.Generic;
using System.IO;
using GitSharp.Exceptions;
using GitSharp.RevWalk;

namespace GitSharp.Transport
{
    
    public class FetchProcess
    {
        private readonly Transport transport;
        private readonly List<RefSpec> toFetch;
        private readonly Dictionary<ObjectId, Ref> askFor = new Dictionary<ObjectId, Ref>();
        private readonly List<ObjectId> have = new List<ObjectId>();
        private readonly List<TrackingRefUpdate> localUpdates = new List<TrackingRefUpdate>();
        private readonly List<FetchHeadRecord> fetchHeadUpdates = new List<FetchHeadRecord>();
        private readonly List<PackLock> packLocks = new List<PackLock>();

        private IFetchConnection conn;

        public FetchProcess(Transport t, List<RefSpec> f)
        {
            transport = t;
            toFetch = f;
        }

        public void execute(ProgressMonitor monitor, FetchResult result)
        {
            askFor.Clear();
            localUpdates.Clear();
            fetchHeadUpdates.Clear();
            packLocks.Clear();

            try
            {
                executeImp(monitor, result);
            }
            finally
            {
                foreach (PackLock pl in packLocks) pl.Unlock();
            }
        }

        private void executeImp(ProgressMonitor monitor, FetchResult result)
        {
            conn = transport.openFetch();
            try
            {
                result.SetAdvertisedRefs(transport.URI, conn.RefsMap);
                List<Ref> matched = new List<Ref>();
                foreach (RefSpec spec in toFetch)
                {
                    if (spec.Source == null)
                    {
                        throw new TransportException("Source ref not specified for refspec: " + spec);
                    }

                    if (spec.Wildcard)
                        expandWildcard(spec, matched);
                    else
                    {
                        expandSingle(spec, matched);
                    }
                }

                List<Ref> additionalTags = new List<Ref>();
                TagOpt tagopt = transport.TagOpt;
                if (tagopt == TagOpt.AUTO_FOLLOW)
                    additionalTags = expandAutoFollowTags();
                else if (tagopt == TagOpt.FETCH_TAGS)
                    expandFetchTags();

                bool includedTags;
                if (askFor.Count != 0 && !askForIsComplete())
                {
                    fetchObjects(monitor);
                    includedTags = conn.DidFetchIncludeTags;

                    closeConnection();
                }
                else
                {
                    includedTags = false;
                }

                if (tagopt == TagOpt.AUTO_FOLLOW && additionalTags.Count != 0)
                {
                    have.AddRange(askFor.Keys);
                    askFor.Clear();
                    foreach (Ref r in additionalTags)
                    {
                        ObjectId id = r.PeeledObjectId;
                        if (id == null || transport.Local.HasObject(id))
                            wantTag(r);
                    }

                    if (askFor.Count != 0 && (!includedTags || !askForIsComplete()))
                    {
                        reopenConnection();
                        if (askFor.Count != 0)
                            fetchObjects(monitor);
                    }
                }
            }
            finally
            {
                closeConnection();
            }

            RevWalk.RevWalk walk = new RevWalk.RevWalk(transport.Local);
            if (transport.RemoveDeletedRefs)
                deleteStaleTrackingRefs(result, walk);
            foreach (TrackingRefUpdate u in localUpdates)
            {
                try
                {
                    u.Update(walk);
                    result.Add(u);
                }
                catch (IOException err)
                {
                    throw new TransportException("Failue updating tracking ref " + u.LocalName + ": " + err.Message, err);
                }
            }

            if (fetchHeadUpdates.Count != 0)
            {
                try
                {
                    updateFETCH_HEAD(result);
                }
                catch (IOException err)
                {
                    throw new TransportException("Failure updating FETCH_HEAD: " + err.Message, err);
                }
            }
        }

        private void fetchObjects(ProgressMonitor monitor)
        {
            try
            {
                conn.SetPackLockMessage("jgit fetch " + transport.URI);
                conn.Fetch(monitor, new List<Ref>(askFor.Values), have);
            }
            finally
            {
                packLocks.AddRange(conn.PackLocks);
            }
            if (transport.CheckFetchedObjects && !conn.DidFetchTestConnectivity && !askForIsComplete())
            {
                throw new TransportException(transport.URI, "Peer did not supply a complete object graph");
            }
        }

        private void closeConnection()
        {
            if (conn != null)
            {
                conn.Close();
                conn = null;
            }
        }

        private void reopenConnection()
        {
            if (conn != null)
                return;

            conn = transport.openFetch();

            Dictionary<ObjectId, Ref> avail = new Dictionary<ObjectId, Ref>();
            foreach (Ref r in conn.Refs)
                avail.Add(r.ObjectId, r);

            List<Ref> wants = new List<Ref>(askFor.Values);
            askFor.Clear();
            foreach (Ref want in wants)
            {
                Ref newRef = avail[want.ObjectId];
                if (newRef != null)
                {
                    askFor.Add(newRef.ObjectId, newRef);
                }
                else
                {
                    removeFetchHeadRecord(want.ObjectId);
                    removeTrackingRefUpdate(want.ObjectId);
                }
            }
        }

        private void removeTrackingRefUpdate(ObjectId want)
        {
            IEnumerator<TrackingRefUpdate> i = localUpdates.GetEnumerator();
            while (i.MoveNext())
            {
                TrackingRefUpdate u = i.Current;
                if (u.NewObjectId.Equals(want))
                    localUpdates.Remove(u);
            }
        }

        private void removeFetchHeadRecord(ObjectId want)
        {
            IEnumerator<FetchHeadRecord> i = fetchHeadUpdates.GetEnumerator();
            while (i.MoveNext())
            {
                FetchHeadRecord fh = i.Current;
                if (fh.NewValue.Equals(want))
                    fetchHeadUpdates.Remove(fh);
            }
        }

        private void updateFETCH_HEAD(FetchResult result)
        {
            LockFile @lock = new LockFile(new FileInfo(Path.Combine(transport.Local.Directory.ToString(), "FETCH_HEAD")));
            if (@lock.Lock())
            {
                StreamWriter sw = new StreamWriter(@lock.GetOutputStream());
                foreach (FetchHeadRecord h in fetchHeadUpdates)
                {
                    h.Write(sw);
                    sw.Write('\n');
                    result.Add(h);
                }
                sw.Close();
                @lock.Commit();
            }
        }

        private bool askForIsComplete()
        {
            try
            {
                ObjectWalk ow = new ObjectWalk(transport.Local);
                foreach (ObjectId want in askFor.Keys)
                    ow.markStart(ow.parseAny(want));
                foreach (Ref r in transport.Local.Refs.Values)
                    ow.markUninteresting(ow.parseAny(r.ObjectId));
                ow.checkConnectivity();
                return true;
            }
            catch (MissingObjectException)
            {
                return false;
            }
            catch (IOException e)
            {
                throw new TransportException("Unable to check connectivity.", e);
            }
        }

        private void expandWildcard(RefSpec spec, List<Ref> matched)
        {
            foreach (Ref src in conn.Refs)
            {
                if (spec.MatchSource(src))
                {
                    matched.Add(src);
                    want(src, spec.ExpandFromSource(src));
                }
            }
        }

        private void expandSingle(RefSpec spec, List<Ref> matched)
        {
            Ref src = conn.GetRef(spec.Source);
            if (src == null)
            {
                throw new TransportException("Remote does not have " + spec.Source + " available for fetch.");
            }
            matched.Add(src);
            want(src, spec);
        }

        private List<Ref> expandAutoFollowTags()
        {
            List<Ref> additionalTags = new List<Ref>();
            Dictionary<string, Ref> haveRefs = transport.Local.Refs;
            foreach (Ref r in conn.Refs)
            {
                if (!isTag(r))
                    continue;
                if (r.PeeledObjectId == null)
                {
                    additionalTags.Add(r);
                    continue;
                }

                Ref local = haveRefs[r.Name];
                if (local != null)
                {
                    if (!r.ObjectId.Equals(local.ObjectId))
                        wantTag(r);
                }
                else if (askFor.ContainsKey(r.PeeledObjectId) || transport.Local.HasObject(r.PeeledObjectId))
                    wantTag(r);
                else
                    additionalTags.Add(r);
            }
            return additionalTags;
        }

        private void expandFetchTags()
        {
            Dictionary<string, Ref> haveRefs = transport.Local.Refs;
            foreach (Ref r in conn.Refs)
            {
                if (!isTag(r))
                    continue;
                Ref local = haveRefs[r.Name];
                if (local == null || !r.ObjectId.Equals(local.ObjectId))
                    wantTag(r);
            }
        }

        private void wantTag(Ref r)
        {
            want(r, new RefSpec().SetSource(r.Name).SetDestination(r.Name));
        }

        private void want(Ref src, RefSpec spec)
        {
            ObjectId newId = src.ObjectId;
            if (spec.Destination != null)
            {
                try
                {
                    TrackingRefUpdate tru = createUpdate(spec, newId);
                    if (newId.Equals(tru.OldObjectId))
                        return;
                    localUpdates.Add(tru);
                }
                catch (IOException err)
                {
                    throw new TransportException("Cannot resolve local tracking ref " + spec.Destination + " for updating.", err);
                }
            }

            askFor.Add(newId, src);

            FetchHeadRecord fhr = new FetchHeadRecord
                                      {
                                          NewValue = newId,
                                          NotForMerge = (spec.Destination != null),
                                          SourceName = src.Name,
                                          SourceURI = transport.URI
                                      };

            fetchHeadUpdates.Add(fhr);
        }

        private TrackingRefUpdate createUpdate(RefSpec spec, ObjectId newId)
        {
            return new TrackingRefUpdate(transport.Local, spec, newId, "fetch");
        }

        private void deleteStaleTrackingRefs(FetchResult result, RevWalk.RevWalk walk)
        {
            Repository db = transport.Local;
            foreach (Ref r in db.Refs.Values)
            {
                string refname = r.Name;
                foreach (RefSpec spec in toFetch)
                {
                    if (spec.MatchDestination(refname))
                    {
                        RefSpec s = spec.ExpandFromDestination(refname);
                        if (result.GetAdvertisedRef(s.Source) == null)
                        {
                            deleteTrackingRef(result, db, walk, s, r);
                        }
                    }
                }
            }
        }

        private void deleteTrackingRef(FetchResult result, Repository db, RevWalk.RevWalk walk, RefSpec spec, Ref localRef)
        {
            string name = localRef.Name;
            try
            {
                TrackingRefUpdate u = new TrackingRefUpdate(db, name, spec.Source, true, ObjectId.ZeroId, "deleted");
                result.Add(u);
                if (transport.DryRun)
                    return;
                u.Delete(walk);
                switch (u.Result)
                {
                    case RefUpdate.RefUpdateResult.New:
                    case RefUpdate.RefUpdateResult.NoChange:
                    case RefUpdate.RefUpdateResult.FastForward:
                    case RefUpdate.RefUpdateResult.Forced:
                        break;
                    default:
                        throw new TransportException(transport.URI, "Cannot delete stale tracking ref " + name + ": " + u.Result);
                }
            }
            catch (IOException e)
            {
                throw new TransportException(transport.URI, "Cannot delete stale tracking ref " + name, e);
            }
        }

        private static bool isTag(Ref r)
        {
            return isTag(r.Name);
        }

        private static bool isTag(string name)
        {
            return name.StartsWith(Constants.R_TAGS);
        }
    }

}