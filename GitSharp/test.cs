using System;
using System.IO;
using GitSharp;

class X {

	static void Main (string [] args)
	{
		var repo = Repository.Open(new DirectoryInfo ("/cvs/egit/.git"));

		Console.WriteLine ("Refs:");
        var refs = repo.Refs;
		foreach (var k in refs){
			Console.WriteLine ("  {0} -> {1}", k.Key, k.Value);
		}

		Ref heads_master = refs ["refs/heads/master"];

		ObjectLoader ol = repo.OpenObject (heads_master.ObjectId);

		//Console.WriteLine ("ObjectType: {0}", ol.ObjectType);
		Console.WriteLine ("      Size: {0}", ol.Size);
		Console.WriteLine ("   RawSize: {0}", ol.RawSize);
	}
}