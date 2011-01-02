/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
 * Copyright (C) 2010, Dominique van de Vorle <dvdvorle@gmail.com>
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

using Tamir.Streams;

namespace GitSharp.Core.Transport
{
    public class TransportLocal : Transport, IPackTransport
    {
        private const string PWD = ".";
        protected readonly DirectoryInfo remoteGitDir;

        public TransportLocal(Repository local, URIish uri)
            : base(local, uri)
        {
            string dir = FS.resolve(new DirectoryInfo(PWD), uri.Path).FullName;
            if(Directory.Exists(Path.Combine(dir, Constants.DOT_GIT)))
            {
                dir = Path.Combine(dir, Constants.DOT_GIT);
            }
            
            remoteGitDir = new DirectoryInfo(dir);
        }

        public static bool canHandle(URIish uri)
        {
			if (uri == null)
				throw new ArgumentNullException ("uri");
			
            if (uri.Host != null || uri.Port > 0 || uri.User != null || uri.Pass != null || uri.Path == null)
            {
                return false;
            }

            if ("file".Equals(uri.Scheme) || uri.Scheme == null)
            {
                return FS.resolve(new DirectoryInfo(PWD), uri.Path).Exists;
            }

            return false;
        }

        public override IFetchConnection openFetch()
        {
            if (OptionUploadPack.Equals("git-upload-pack") || OptionUploadPack.Equals("git upload-pack"))
            {
                return new InternalLocalFetchConnection(this);
            }
            return new ForkLocalFetchConnection(this);
        }

        public override IPushConnection openPush()
        {
            if (OptionReceivePack.Equals("git-receive-pack") || OptionReceivePack.Equals("git receive-pack"))
            {
                return new InternalLocalPushConnection(this);
            }
		    return new ForkLocalPushConnection(this);
        }

        public override void close()
        {
            // Resources must be established per-connection.
        }

        protected Process Spawn(string cmd)
	    {
		    try
            {
			    ProcessStartInfo psi = new ProcessStartInfo();

			    if (cmd.StartsWith("git-"))
                {   
                    psi.FileName = "git";
                    psi.Arguments = cmd.Substring(4);
			    }
                else
                {
				    int gitspace = cmd.IndexOf("git ");
				    if (gitspace >= 0) {
    					psi.FileName = cmd.Substring(0, gitspace + 3);
					    psi.Arguments = cmd.Substring(gitspace + 4);
				    }
                    else
                    {
                        psi.FileName = cmd;
				    }
		    	}
                
                if(psi.Arguments.Equals(String.Empty))
                {
                    psi.Arguments = PWD;
                }
                else
                {
                    psi.Arguments += " " + PWD;
                }
                
                var process = new Process() { StartInfo = psi};
                process.Start();
                return process;
	        }
            catch (IOException ex)
            {
	            throw new TransportException(Uri, ex.Message, ex);
            }
		}

        #region Nested types

        private class InternalLocalFetchConnection : BasePackFetchConnection
        {
		    private Thread worker;
            private readonly PipedInputStream in_r;
			private readonly PipedOutputStream in_w;

		    public InternalLocalFetchConnection( TransportLocal transport)
                : base( transport )
            {
                
                Repository dst;
			    try
                {
				    dst = new Repository(transport.remoteGitDir);
			    }
                catch (IOException)
                {
				    throw new TransportException(uri, "Not a Git directory");
			    }

                PipedInputStream out_r;
			    PipedOutputStream out_w;
			    try
                {
				    in_r = new PipedInputStream();
				    in_w = new PipedOutputStream(in_r);

				    out_r = new PipedInputStream();
				    out_w = new PipedOutputStream(out_r);
			    }
                catch (IOException ex)
                {
				    dst.Close();
				    throw new TransportException(uri, "Cannot connect pipes", ex);
			    }

			    worker = new Thread( () => 
                    {
					    try
                        {
                            UploadPack rp = new UploadPack(dst);
						    rp.Upload(out_r, in_w, null);
					    }
                        catch (IOException ex)
                        {
						    // Client side of the pipes should report the problem.
						    ex.printStackTrace();
					    } 
                        catch (Exception ex)
                        {
						    // Clients side will notice we went away, and report.
						    ex.printStackTrace();
					    } 
                        finally
                        {
						    try
                            {
							    out_r.close();
						    }
                            catch (IOException)
                            {
							    // Ignore close failure, we probably crashed above.
						    }

						    try
                            {
							    in_w.close();
						    } catch (IOException)
                            {
							    // Ignore close failure, we probably crashed above.
						    }

						    dst.Close();
					    }

				    });
                worker.Name = "JGit-Upload-Pack";
			    worker.Start();

			    init(in_r, out_w);
			    readAdvertisedRefs();
		    }

          
		    override public void Close()
            {
			    base.Close();

			    if (worker != null)
                {
				    try
                    {
					    worker.Join();
				    }
                    catch ( ThreadInterruptedException)
                    {
					    // Stop waiting and return anyway.
				    }
                    finally
                    {
					    worker = null;
				    }
			    }
		    }
	    }
        
        private class InternalLocalPushConnection : BasePackPushConnection
        {
		    private Thread worker;

		    public InternalLocalPushConnection(TransportLocal transport)
                : base(transport)
            {
                Repository dst;
			    try
                {
				    dst = new Repository(transport.remoteGitDir);
			    }
                catch (IOException)
                {
				    throw new TransportException(uri, "Not a Git directory");
			    }

			    PipedInputStream in_r;
			    PipedOutputStream in_w;

			    PipedInputStream out_r;
			    PipedOutputStream out_w;
			    try
                {
				    in_r = new PipedInputStream();
				    in_w = new PipedOutputStream(in_r);

				    out_r = new PipedInputStream();
				    out_w = new PipedOutputStream(out_r);
			    }
                catch (IOException ex)
                {
				    dst.Close();
				    throw new TransportException(uri, "Cannot connect pipes", ex);
			    }

			    worker = new Thread(() =>
                {
				    try
                    {
			            ReceivePack rp = new ReceivePack(dst);
					    rp.receive(out_r, in_w, null);
					}
                    catch (IOException)
                    {
					    // Client side of the pipes should report the problem.
					}
                    catch (Exception)
                    {
					    // Clients side will notice we went away, and report.
				    }
                    finally
                    {
						try
                        {
						    out_r.close();
					    }
                        catch (IOException)
                        {
							// Ignore close failure, we probably crashed above.
						}
    				    
                        try
                        {
						    in_w.close();
					    }
                        catch (IOException)
                        {
							// Ignore close failure, we probably crashed above.
						}

					    dst.Close();
					}
			    });
			    worker.Name = "JGit-Receive-Pack";
                worker.Start();

			    init(in_r, out_w);
			    readAdvertisedRefs();
		    }

		    public override void  Close()
            {
                base.Close();

			    if (worker != null)
                {
				    try
                    {
					    worker.Join();
				    }
                    catch (ThreadInterruptedException)
                    {
					    // Stop waiting and return anyway.
				    }
                    finally
                    {
					    worker = null;
				    }
			    }
		    }
	    }

	    private class ForkLocalFetchConnection : BasePackFetchConnection 
        {
		    private Process uploadPack;

		    public ForkLocalFetchConnection( TransportLocal transport)
                : base(transport)
            {
			    uploadPack = transport.Spawn(transport.OptionUploadPack);

			    Stream upIn = new BufferedStream(uploadPack.StandardInput.BaseStream);
			    Stream upOut = new BufferedStream(uploadPack.StandardOutput.BaseStream);

			    init(upIn, upOut);
			    readAdvertisedRefs();
		    }

		    public override void Close()
            {
			    base.Close();

			    if (uploadPack != null)
                {
				    try
                    {
					    uploadPack.WaitForExit();
				    }
                    catch (ThreadInterruptedException)
                    {
					    // Stop waiting and return anyway.
				    }
                    finally
                    {
					    uploadPack = null;
				    }
			    }
		    }
	    }

	    private class ForkLocalPushConnection : BasePackPushConnection
        {
		    private Process receivePack;

		    public ForkLocalPushConnection( TransportLocal transport)
                : base(transport)
            {
			    receivePack = transport.Spawn(transport.OptionReceivePack);

			    Stream rpIn = new BufferedStream(receivePack.StandardInput.BaseStream);
			    Stream rpOut = new BufferedStream(receivePack.StandardOutput.BaseStream);

			    init(rpIn, rpOut);
			    readAdvertisedRefs();
		    }

		    public override void Close()
            {
			    base.Close();

			    if (receivePack != null)
                {
				    try
                    {
					    receivePack.WaitForExit();
				    }
                    catch (ThreadInterruptedException)
                    {
					    // Stop waiting and return anyway.
				    }
                    finally
                    {
					    receivePack = null;
				    }
			    }
		    }
	    }
        #endregion
    }
}
