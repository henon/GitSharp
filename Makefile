CSC=gmcs

SOURCES =  \
	AbstractIndexTreeVisitor.cs \
	AnyObjectId.cs \
	BinaryDelta.cs \
	Codec.cs \
	Commit.cs \
	CompleteAttribute.cs \
	Constants.cs \
	CoreConfig.cs \
	DeltaOfsPackedObjectLoader.cs \
	DeltaPackedObjectLoader.cs \
	DeltaRefPackedObjectLoader.cs \
	Ensure.cs \
	Exceptions/CorruptObjectException.cs \
	Exceptions/EntryExistsException.cs \
	Exceptions/FileLockedException.cs \
	Exceptions/IncorrectObjectTypeException.cs \
	Exceptions/MissingObjectException.cs \
	Exceptions/ObjectWritingException.cs \
	Exceptions/RevisionSyntaxException.cs \
	Exceptions/SymlinksNotSupportedException.cs \
	Extensions.cs \
	FileMode.cs \
	FileTreeEntry.cs \
	ForceModified.cs \
	GitException.cs \
	GitIndex.cs \
	IndexDiff.cs \
	IndexTreeVisitor.cs \
	IndexTreeWalker.cs \
	LockFile.cs \
	MutableObjectId.cs \
	NullProgressMonitor.cs \
	ObjectId.cs \
	ObjectIdMap.cs \
	ObjectLoader.cs \
	ObjectType.cs \
	ObjectWriter.cs \
	PackedObjectLoader.cs \
	PackFile.cs \
	PackIndex.cs \
	PackIndexV1.cs \
	PackIndexV2.cs \
	PackIndexWriter.cs \
	PackIndexWriterV1.cs \
	PackIndexWriterV2.cs \
	PackReverseIndex.cs \
	PersonIdent.cs \
	ProgressMonitor.cs \
	Properties/AssemblyInfo.cs \
	Ref.cs \
	RefComparer.cs \
	RefDatabase.cs \
	RefLogWriter.cs \
	RefUpdate.cs \
	Repository.cs \
	RepositoryConfig.cs \
	RepositoryState.cs \
	SymlinkTreeEntry.cs \
	Tag.cs \
	test.cs \
	TextProgressMonitor.cs \
	Transport/PackedObjectInfo.cs \
	Tree.cs \
	TreeEntry.cs \
	Treeish.cs \
	TreeIterator.cs \
	TreeVisitor.cs \
	TreeVisitorWithCurrentDirectory.cs \
	UnpackedObjectLoader.cs \
	Util/BufferedReader.cs \
	Util/CheckedOutputStream.cs \
	Util/CRC32.cs \
	Util/Hex.cs \
	Util/MessageDigest.cs \
	Util/NestedDictionary.cs \
	Util/Numbers.cs \
	Util/PathUtil.cs \
	Util/RawParseUtils.cs \
	Util/WeakReference.cs \
	WholePackedObjectLoader.cs \
	WriteTree.cs

all: Gitty.Core.dll test.exe

Gitty.Core.dll: $(SOURCES)
	mkdir -p bin/Debug
	$(CSC) $(SOURCES) -out:bin/Debug/Gitty.Core.dll -debug -target:library

test.exe: Gitty.Core.dll test.cs
	$(CSC) -r:bin/Debug/Gitty.Core.dll -debug test.cs

run: test.exe
	mono --debug test.exe

clean: 
	rm -f *.dll *.mdb *.exe
