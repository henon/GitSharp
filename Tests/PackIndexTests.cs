using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;

namespace Gitty.Core.Tests
{
    [TestFixture]
    public class PackIndexTests
    {
        [Test]
        public void ObjectList()
        {
            var knownOffsets = new long[] { 370, 349, 304, 12, 175, 414 };
            var knownCRCs = new long[] { 1376555649, 3015185563, 2667925865, 914969567, 2706901546, 39477847 };
            var knownObjectIds = new string[] { "1AFC38724D2B89264C7B3826D40B0655A95CFAB4", "557DB03DE997C86A4A028E1EBD3A1CEB225BE238", "67DC4302383B2715F4E0B8C41840EB05B1873697", "A48B402F61EB8ED445DACAA3AF80A2E9796DCB3B", "E41517D564000311F3D7A54F1390EE82F5F1A55B", "E965047AD7C57865823C7D992B1D046EA66EDF78" };
            var indexFile = new FileInfo(@"sample.git\objects\pack\pack-845b2ba3349cc201321e752b01c5ada8102a9a08.idx");
            var index = PackIndex.Open(indexFile);
            Assert.AreEqual(6, index.ObjectCount);
            Assert.IsTrue(index.HasCRC32Support);
            var i = 0;
            foreach (var item in index)
            {
                Assert.AreEqual(knownObjectIds[i], item.ToString().ToUpper(), "ObjectListId#"+i.ToString());
                Assert.AreEqual(knownOffsets[i], item.Offset, "ObjectListOffset#" + i.ToString());
                Assert.AreEqual(knownCRCs[i], index.FindCRC32(item), "ObjectListCRC#" + i.ToString());
                i++;
            }
        }

    }
}
