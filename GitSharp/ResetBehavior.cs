namespace GitSharp
{
    public enum ResetBehavior
    {
        //TODO: Review the comments (they're from official git documentation and should be adapted to GitSharp API).

        /// <summary>
        /// Resets the index but not the working tree (i.e., the changed files are preserved but not marked for commit) and reports what has not been updated.
        /// </summary>
        Mixed,

        /// <summary>
        /// Does not touch the index file nor the working tree at all, but requires them to be in a good order. This leaves all your changed files "Changes to be committed", as git-status would put it.
        /// </summary>
        Soft,
    
        /// <summary>
        /// Matches the working tree and index to that of the tree being switched to. Any changes to tracked files in the working tree since <commit> are lost.
        /// </summary>
        Hard,

        /// <summary>
        /// Resets the index to match the tree recorded by the named commit, and updates the files that are different between the named commit and the current commit in the working tree.
        /// </summary>
        Merge
    }
}