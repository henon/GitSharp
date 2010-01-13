namespace GitSharp
{

    /// <summary>
    /// Reset policies for Branch.Reset (see Branch)
    /// </summary>
    public enum ResetBehavior
    {

        /// <summary>
        /// Resets the index but not the working directory (i.e., the changed files are preserved but not marked for commit).
        /// </summary>
        Mixed,

        /// <summary>
        /// Does not touch the index nor the working directory at all, but requires them to be in a good order. This leaves all your changed files "Changes to be committed", as git-status would put it.
        /// </summary>
        Soft,
    
        /// <summary>
        /// Matches the working directory and index to that of the commit being reset to. Any changes to tracked files in the working directory since are lost.
        /// </summary>
        Hard,

        /// <summary>
        /// Resets the index to match the tree recorded by the named commit, and updates the files that are different between the named commit and the current commit in the working directory.
        /// </summary>
        Merge
    }
}