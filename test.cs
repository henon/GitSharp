using System;
using System.IO;
using Gitty.Lib;

class X {

	static void Main (string [] args)
	{
		var repo = new Repository (new DirectoryInfo ("/cvs/egit/.git"));

		Console.WriteLine ("Refs:");
		var refs = repo.GetAllRefs ();
		foreach (var k in refs){
			Console.WriteLine ("  {0} -> {1}", k.Key, k.Value);
		}
	}
}