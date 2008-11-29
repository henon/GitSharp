CSC=gmcs

SOURCES = \
	CompleteAttribute.cs				\
	Exceptions/CorruptObjectException.cs		\
	Exceptions/FileLockedException.cs		\
	Exceptions/IncorrectObjectTypeException.cs	\
	Exceptions/MissingObjectException.cs		\
	Exceptions/ObjectWritingException.cs		\
	Exceptions/RevisionSyntaxException.cs		\
	Exceptions/SymlinksNotSupportedException.cs	\
	Extensions/IntExtensions.cs			\
	Lib/AbstractIndexTreeVisitor.cs			\
	Lib/AnyObjectId.cs				\
	Lib/BinaryDelta.cs				\
	Lib/ByteWindow.cs				\
	Lib/Commit.cs					\
	Lib/CoreConfig.cs				\
	Lib/DeltaOfsPackedObjectLoader.cs		\
	Lib/DeltaPackedObjectLoader.cs			\
	Lib/DeltaRefPackedObjectLoader.cs		\
	Lib/FileMode.cs					\
	Lib/FileTreeEntry.cs				\
	Lib/ForceModified.cs				\
	Lib/GitException.cs				\
	Lib/GitIndex.cs					\
	Lib/IndexDiff.cs				\
	Lib/IndexTreeVisitor.cs				\
	Lib/IndexTreeWalker.cs				\
	Lib/InflaterCache.cs				\
	Lib/LockFile.cs					\
	Lib/MutableObjectId.cs				\
	Lib/NullProgressMonitor.cs			\
	Lib/ObjectId.cs					\
	Lib/ObjectIdMap.cs				\
	Lib/ObjectLoader.cs				\
	Lib/ObjectType.cs				\
	Lib/ObjectWriter.cs				\
	Lib/PackedObjectLoader.cs			\
	Lib/PackFile.cs					\
	Lib/PackIndex.cs				\
	Lib/PackIndexV1.cs				\
	Lib/PackIndexV2.cs				\
	Lib/PackIndexWriter.cs				\
	Lib/PackReverseIndex.cs				\
	Lib/PersonIdent.cs				\
	Lib/ProgressMonitor.cs				\
	Lib/Ref.cs					\
	Lib/RefDatabase.cs				\
	Lib/RefLogWriter.cs				\
	Lib/RefUpdate.cs				\
	Lib/Repository.cs				\
	Lib/RepositoryConfig.cs				\
	Lib/RepositoryState.cs				\
	Lib/SymlinkTreeEntry.cs				\
	Lib/Tag.cs					\
	Lib/TextProgressMonitor.cs			\
	Lib/Tree.cs					\
	Lib/TreeEntry.cs				\
	Lib/Treeish.cs					\
	Lib/TreeVisitor.cs				\
	Lib/TreeVisitorWithCurrentDirectory.cs		\
	Lib/UnpackedObjectCache.cs			\
	Lib/UnpackedObjectLoader.cs			\
	Lib/WholePackedObjectLoader.cs			\
	Lib/WindowCursor.cs				\
	Lib/WindowedFile.cs				\
	Lib/WriteTree.cs				\
	Properties/AssemblyInfo.cs			\
	Util/BufferedReader.cs				\
	Util/Collections.cs				\
	Util/CheckedOutputStream.cs			\
	Util/Hex.cs					\
	Util/NestedDictionary.cs			\
	Util/Numbers.cs					\
	Util/PathUtil.cs				\
	Util/WeakReference.cs	

all: Gitty.Lib.CSharp.dll test.exe

Gitty.Lib.CSharp.dll: $(SOURCES)
	$(CSC) $(SOURCES) -out:Gitty.Lib.CSharp.dll -debug -target:library -r:ICSharpCode.SharpZipLib

test.exe: Gitty.Lib.CSharp.dll
	$(CSC) -r:Gitty.Lib.CSharp.dll -debug test.cs

clean: 
	rm -f *.dll *.mdb