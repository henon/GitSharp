== Git# --> Git for .NET ==
... a native Windows version of the fast & free open source version control system

Git# is the most advanced C# implementation of git for Windows and the .NET framework. 
It is aimed to be fully compatible to the original git for linux and can be used as stand 
alone command line application (potentially replacing msysGit) or as library for windows 
applications such as gui frontends or plugins for IDEs.

Git# is released under the BSD license. It is derived from the Java version jgit.
Please refer to the LICENSE.txt files for the complete license, and please refer to the 
individual source file header to determine who contributed.

For more info check out the Git# website at http://www.eqqon.com/index.php/GitSharp

== WARNINGS / CAVEATS   ==

- Symbolic links are not supported because Windows does not directly support them.
  Such links could be damaged.

- Only the timestamp of the index is used by git check if  the index
  is dirty.

- CRLF conversion is never performed. You should therefore
  make sure your projects and workspaces are configured to save files
  with Unix (LF) line endings.

== Features ==

    * Read loose and packed commits, trees, blobs, including
      deltafied objects.

    * Read objects from shared repositories

    * Write loose commits, trees, blobs.

    * Copy trees to local directory, or local directory to a tree.

    * Lazily loads objects as necessary.

    * Read and write .git/config files.

    * Create a new repository.

    * Read and write refs, including walking through symrefs. (not ported yet)

    * Read, update and write the Git index. (updating and writing not ported yet)

    * Checkout in dirty working directory if trivial. (not ported yet)

    * Walk the history from a given set of commits looking for commits
      introducing changes in files under a specified path.

    * Object transport  (not ported yet)
      Fetch via ssh, git, http, Amazon S3 and bundles.
      Push via ssh, git and Amazon S3. Git# does not yet deltify
      the pushed packs so they may be a lot larger than C Git packs.

== Missing Features ==

There are a lot of missing features in jgit and thus also in Git#. You need the real Git 
for those. For some operations it may just be the preferred solution also. There
are not just a command line, there is e.g. git-gui that makes committing
partial files simple.

- Merging. 

- Repacking.

- Generate a GIT format patch.

- Apply a GIT format patch.

- Documentation. :-)

- gitattributes support
  In particular CRLF conversion is not implemented. Files are treated
  as byte sequences.

- submodule support
  Submodules are not supported or even recognized.

== Support ==

  Post question, comments or patches to the official Git# mailing list at 
  http://groups.google.com/group/gitsharp/.


== Contributing ==

  Feel free to fork the source at github and start coding. We will pull your commits regularly.
  However, feedback and bug reports or linking to the website from your blog are also 
  contributions.

== About GIT itself ==

More information about GIT, its repository format, and the canonical
C based implementation can be obtained from the GIT websites:

  http://git.or.cz/
  http://www.kernel.org/pub/software/scm/git/
  http://www.kernel.org/pub/software/scm/git/docs/

More information about the Java implemetation which Git# stems from:
  http://git.or.cz/gitwiki/EclipsePlugin


This product includes software developed by Jon Skeet and Marc Gravell.
  Contact skeet@pobox.com, or see http://www.pobox.com/~skeet/).