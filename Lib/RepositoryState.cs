using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
    [Complete]
    public sealed class RepositoryState
    {
        public readonly static RepositoryState Safe = new RepositoryState(true, true, true, "Normal");
        public readonly static RepositoryState Merging = new RepositoryState(false, false, false, "Conflicts");
        public readonly static RepositoryState Rebasing = new RepositoryState(false, false, true, "Rebase/Apply mailbox");
        public readonly static RepositoryState RebasingMerge = new RepositoryState(false, false, true, "Rebase w/merge");
        public readonly static RepositoryState RebasingInteractive = new RepositoryState(false, false, true, "Rebase interactive");
        public readonly static RepositoryState Bisecting = new RepositoryState(true, false, false, "Bisecting");

        public bool CanCheckout { get; private set; }
        public bool CanResetHead { get; private set; }
        public bool CanCommit { get; private set; }
        public string Description { get; private set; }

        private RepositoryState(bool checkout, bool resetHead, bool commit, string description)
        {
            this.CanCheckout = checkout;
            this.CanResetHead = resetHead;
            this.CanCommit = commit;
            this.Description = description;
        }
    }
}
