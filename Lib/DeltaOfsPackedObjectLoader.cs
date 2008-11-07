using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
	class DeltaOfsPackedObjectLoader : DeltaPackedObjectLoader
	{
		public DeltaOfsPackedObjectLoader(WindowCursor curs, PackFile pr, long dataOffset,
			long objectOffset, int deltaSz, long baseValue)
			: base(curs, pr, dataOffset, objectOffset, deltaSz)
		{
			throw new NotImplementedException();
		}
	}
}
