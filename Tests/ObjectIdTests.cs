using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Gitty.Lib.CSharp.Tests
{
    [TestFixture]
    public class ObjectIdTests
    {
        [Test]
        public void ObjectIdToStringTest()
        {
            var id = ObjectId.FromString("003ae55c8f6f23aaee66acd2e1c35523fa6ddc33");
            Assert.AreEqual("003ae55c8f6f23aaee66acd2e1c35523fa6ddc33", id.ToString());
            Assert.AreEqual(0, id.GetFirstByte());
        }

        [Test]
        public void GetFirstByteTest()
        {
            for(var i = 0; i < 255;i++)
            {
                var iInHex = i.ToString("x").PadLeft(2, '0');
                foreach(var j in new[] {0x0,0x1,0xffffff})
                {
                    var firstFourBytes = iInHex + j.ToString("x").PadLeft(6, '0');
                    var id = ObjectId.FromString(firstFourBytes + "00000000000000000000000000000000");
                    Assert.AreEqual(i, id.GetFirstByte(),"GetFirstByteTest#" + firstFourBytes);    
                }
                
            }
        }
    }
}
