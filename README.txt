== Git# ==

Git# is released under the BSD license. It is derived from the Java version jgit.
Please refer to the LICENSE.txt files for the complete license, and please refer to the individual source file 
header to determine which license covers it.


== WARNINGS / CAVEATS              ==

- Symbolic links are not supported because Windows does not directly support them.
  Such links could be damaged.

- Only the timestamp of the index is used by git check if  the index
  is dirty.

- CRLF conversion is never performed. You should therefore
  make sure your projects and workspaces are configured to save files
  with Unix (LF) line endings.

== Features                ==

    * Read loose and packed commits, trees, blobs, including
      deltafied objects.

    * Read objects from shared repositories

    * Write loose commits, trees, blobs.

    * Write blobs from local files or Java InputStreams.

    * Read blobs as Java InputStreams.

    * Copy trees to local directory, or local directory to a tree.

    * Lazily loads objects as necessary.

    * Read and write .git/config files.

    * Create a new repository.

    * Read and write refs, including walking through symrefs.

    * Read, update and write the Git index.

    * Checkout in dirty working directory if trivial.

    * Walk the history from a given set of commits looking for commits
      introducing changes in files under a specified path.

    * Object transport
      Fetch via ssh, git, http, Amazon S3 and bundles.
      Push via ssh, git and Amazon S3. JGit does not yet deltify
      the pushed packs so they may be a lot larger than C Git packs.

== Missing Features                ==

There are a lot of missing features. You need the real Git for this.
For some operations it may just be the preferred solution also. There
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

== Support                         ==

  Post question, comments or patches to meinrad.recheis@gmail.com.


== Contributing                    ==

  Fork the source at github and start coding. We will pull your commits regularly.
  However, feedback and bug reports are also contributions.

  Short how-to:
   - Make small logical changes.
   - Provide a meaningful commit message.

   - Include your Signed-Off-By line to note you agree with the
     Developer's Certificate of Origin (see below).
   - Make sure all code is under the proper license (BSD)

== About GIT                       ==

More information about GIT, its repository format, and the canonical
C based implementation can be obtained from the GIT websites:

  http://git.or.cz/
  http://www.kernel.org/pub/software/scm/git/
  http://www.kernel.org/pub/software/scm/git/docs/

