using System;

namespace GitSharp.Transport
{
    public class SshTransport : TcpTransport
    {
        public SshTransport(Repository local, URIish uri) : base(local, uri)
        {
        }

        public override IFetchConnection openFetch()
        {
            throw new NotImplementedException();
        }

        public override IPushConnection openPush()
        {
            throw new NotImplementedException();
        }

        public override void close()
        {
            throw new NotImplementedException();
        }
    }
}
