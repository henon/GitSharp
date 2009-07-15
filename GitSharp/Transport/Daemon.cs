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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using GitSharp.Transport;

namespace GitSharp.Transport
{

    // [caytchen] note these two were actually done anonymously in the original jgit
    class UploadPackService : DaemonService
    {
        public UploadPackService() : base("upload-pack", "uploadpack")
        {
            Enabled = true;
        }

        public override void Execute(DaemonClient dc, Repository db)
        {
            UploadPack rp = new UploadPack(db);
            Stream stream = dc.Stream;
            rp.Upload(stream, null);
        }
    }

    class ReceivePackService : DaemonService
    {
        public ReceivePackService() : base("receive-pack", "receivepack")
        {
            Enabled = false;
        }

        public override void Execute(DaemonClient dc, Repository db)
        {
            EndPoint peer = dc.Peer;
            string host = Dns.GetHostEntry((peer as IPEndPoint).Address).HostName ??
                          (peer as IPEndPoint).Address.ToString();
            ReceivePack rp = new ReceivePack(db);
            Stream stream = dc.Stream;
            string name = "anonymous";
            string email = name + "@" + host;
            rp.SetRefLogIdent(new PersonIdent(name, email));
            rp.Receive(stream, null);
        }
    }

    public class Daemon
    {
        public const int DEFAULT_PORT = 9418;
        private const int BACKLOG = 5;

        public IPEndPoint MyAddress { get; private set; }
        public DaemonService[] Services { get; private set; }
        public Dictionary<string, Thread> Processors { get; private set; }
        public bool ExportAll { get; set; }
        public Dictionary<string, Repository> Exports { get; private set; }
        public List<DirectoryInfo> ExportBase { get; private set; }
        public bool Run { get; private set; }

        private Thread acceptThread;

        public Daemon() : this(null)
        {
        }

        public Daemon(IPEndPoint addr)
        {
            MyAddress = addr;
            Exports = new Dictionary<string, Repository>();
            ExportBase = new List<DirectoryInfo>();
            Processors = new Dictionary<string, Thread>();
            Services = new DaemonService[] {new UploadPackService(), new ReceivePackService()};
        }

        public DaemonService GetService(string name)
        {
            if (!name.StartsWith("git-"))
                name = "git-" + name;
            foreach (DaemonService s in Services)
            {
                if (s.Command.Equals(name))
                    return s;
            }
            return null;
        }

        public void ExportRepository(string name, Repository db)
        {
            Exports.Add(name, db);
        }

        public void ExportDirectory(FileInfo dir)
        {
            ExportBase.Add(dir);
        }

        public void Start()
        {
            if (acceptThread != null)
                throw new InvalidOperationException("Daemon already running");

            Socket listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSock.Bind(MyAddress ?? new IPEndPoint(IPAddress.Any, 0));
            listenSock.Listen(BACKLOG);
            MyAddress = (IPEndPoint) listenSock.LocalEndPoint;

            Run = true;
            acceptThread = new Thread(new ThreadStart(delegate
                                                          {
                                                              while (Run)
                                                              {
                                                                  try
                                                                  {
                                                                      startClient(listenSock.Accept());
                                                                  }
                                                                  catch (ThreadInterruptedException)
                                                                  {

                                                                  }
                                                                  catch (SocketException)
                                                                  {
                                                                      break;
                                                                  }
                                                              }

                                                              try
                                                              {
                                                                  listenSock.Close();
                                                              }
                                                              catch (SocketException)
                                                              {

                                                              }
                                                              finally
                                                              {
                                                                  acceptThread = null;
                                                              }
                                                          }));
            acceptThread.Start();
            Processors.Add("Git-Daemon-Accept", acceptThread);
        }

        private void startClient(Socket s)
        {
            DaemonClient dc = new DaemonClient(this);
            dc.Peer = s.RemoteEndPoint;

            // [caytchen] TODO: insanse anonymous methods were ported 1:1 from jgit, do properly sometime
            Thread t = new Thread(
                new ThreadStart(delegate
                                    {
                                        NetworkStream
                                            stream =
                                                new NetworkStream
                                                    (s);
                                        try
                                        {
                                            dc.Execute(
                                                new BufferedStream
                                                    (stream));
                                        }
                                        catch (
                                            IOException)
                                        {

                                        }
                                        catch (
                                            SocketException
                                            )
                                        {

                                        }
                                        finally
                                        {
                                            try
                                            {
                                                stream.
                                                    Close
                                                    ();
                                                s.Close();
                                            }
                                            catch (
                                                IOException
                                                )
                                            {

                                            }
                                            catch (
                                                SocketException
                                                )
                                            {

                                            }
                                        }
                                    }));

            t.Start();
            Processors.Add("Git-Daemon-Client " + s.RemoteEndPoint, t);
        }

        public DaemonService MatchService(string cmd)
        {
            foreach (DaemonService d in Services)
            {
                if (d.Handles(cmd))
                    return d;
            }
            return null;
        }

        public void Stop()
        {
            if (acceptThread != null)
            {
                Run = false;
                // [caytchen] behaviour probably doesn't match
                //acceptThread.Interrupt();
            }
        }

        public Repository OpenRepository(string name)
        {
            name = name.Replace('\\', '/');
            if (!name.StartsWith("/"))
                return null;

            if (name.StartsWith("//"))
                return null;

            if (name.Contains("/../"))
                return null;

            name = name.Substring(1);
            Repository db;
            db = Exports[name];
            if (db != null) return db;
            db = Exports[name + ".git"];
            if (db != null) return db;

            DirectoryInfo[] search;
            search = ExportBase.ToArray();
            foreach (DirectoryInfo f in search)
            {
                string p = f.ToString();
                if (!p.EndsWith("/")) p = p + '/';

                db = openRepository(new DirectoryInfo(p + name));
                if (db != null) return db;

                db = openRepository(new DirectoryInfo(p + name + ".git"));
                if (db != null) return db;

                db = openRepository(new DirectoryInfo(p + name + "/.git"));
                if (db != null) return db;
            }
            return null;
        }

        private Repository openRepository(DirectoryInfo f)
        {
            if (Directory.Exists(f.ToString()) && canExport(f))
            {
                try
                {
                    return new Repository(f);
                }
                catch (IOException)
                {
                    
                }
            }
            return null;
        }

        private bool canExport(DirectoryInfo d)
        {
            if (ExportAll) return true;
            string p = d.ToString();
            if (!p.EndsWith("/")) p = p + '/';
            return File.Exists(p + "git-daemon-export-ok");
        }
    }
}