using System;
using System.Runtime.Serialization;

namespace GitSharp.Exceptions
{
	// [ammachado]: TODO
	/// <summary>
	/// The base class for all exceptions thrown from GitSharp
	/// </summary>
	[Serializable]
	public class GitSharpException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public GitSharpException()
		{
		}

		public GitSharpException(string message) : base(message)
		{
		}

		public GitSharpException(string message, Exception inner) : base(message, inner)
		{
		}

		protected GitSharpException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}
	}
}