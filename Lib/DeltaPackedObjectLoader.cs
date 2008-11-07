using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
	public abstract class DeltaPackedObjectLoader : PackedObjectLoader
	{
		public DeltaPackedObjectLoader(WindowCursor curs, PackFile pr, long dataOffset, long objectOffset, int deltaSz)
			: base(curs, pr, dataOffset, objectOffset)
		{
			throw new NotImplementedException();
		}

		public override ObjectId GetDeltaBase()
		{
			throw new NotImplementedException();
		}

		public override byte[] CachedBytes
		{
			get { throw new NotImplementedException(); }
		}

		public override ObjectType RawType
		{
			get { throw new NotImplementedException(); }
		}

		public override long RawSize
		{
			get { throw new NotImplementedException(); }
		}
	}
}
