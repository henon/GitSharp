using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty.Lib
{
	class DeltaRefPackedObjectLoader : DeltaPackedObjectLoader
	{
		public DeltaRefPackedObjectLoader(WindowCursor curs, PackFile pr, long dataOffset, 
			long objectOffset, int deltaSz, ObjectId idBase) 
			: base(curs, pr, dataOffset, objectOffset, deltaSz)
		{
			throw new NotImplementedException();
		}
	}
}
