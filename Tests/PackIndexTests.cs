using System.IO;
using Xunit;

namespace GitSharp.Tests
{
	public class PackIndexTests
	{
		[Fact]
		public void ObjectList()
		{
			var knownOffsets = new long[] { 370, 349, 304, 12, 175, 414 };
			var knownCRCs = new long[] { 1376555649, 3015185563, 2667925865, 914969567, 2706901546, 39477847 };
			var knownObjectIds = new[] { "1AFC38724D2B89264C7B3826D40B0655A95CFAB4", "557DB03DE997C86A4A028E1EBD3A1CEB225BE238", "67DC4302383B2715F4E0B8C41840EB05B1873697", "A48B402F61EB8ED445DACAA3AF80A2E9796DCB3B", "E41517D564000311F3D7A54F1390EE82F5F1A55B", "E965047AD7C57865823C7D992B1D046EA66EDF78" };

			var indexFile = new FileInfo("sample.git" + Path.DirectorySeparatorChar + "objects"
				+ Path.DirectorySeparatorChar + "pack"
                + Path.DirectorySeparatorChar + "pack-845b2ba3349cc201321e752b01c5ada8102a9a08.idx");

			var index = PackIndex.Open(indexFile);
			Assert.Equal(6, index.ObjectCount);
			Assert.True(index.HasCRC32Support);
			
			var i = 0;
			
			foreach (var item in index)
			{
				Assert.Equal(knownObjectIds[i], item.ToString().ToUpper());
				Assert.Equal(knownOffsets[i], item.Offset);
				Assert.Equal(knownCRCs[i], index.FindCRC32(item));
				i++;
			}
		}
	}
}