using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trinity.DynamicCluster.Persistency;
namespace Trinity.DynamicCluster.Test
{
    [TestClass]
    public class PersistedCellEnumeratorTest
    {
        byte[] contentInt16 = BitConverter.GetBytes(Int16.MaxValue);
        byte[] contentInt32 = BitConverter.GetBytes(Int32.MaxValue);
        byte[] contentInt64 = BitConverter.GetBytes(Int64.MaxValue);
        byte[] contentString = Encoding.Default.GetBytes("JJJJJJJJJJJJJJJJ");//char count==16
        [TestMethod]
        public unsafe void MoveNextTest()
        {

            long lowKey = 20;
            long highKey = 400;
            PersistedCellEnumerator pe = new PersistedCellEnumerator(contentInt32, lowKey, highKey);
            var current = pe.Current;

            Assert.AreEqual(true, pe.MoveNext());
            Assert.AreEqual(false, pe.MoveNext());
        }
        [TestMethod]
        public unsafe void MoveNextTest1()
        {
            long lowKey = 20;
            long highKey = 400;
            PersistedCellEnumerator pe = new PersistedCellEnumerator(contentInt16, lowKey, highKey);
            var current = pe.Current;

            Assert.AreEqual(true, pe.MoveNext());
            Assert.AreEqual(false, pe.MoveNext());
        }
        [TestMethod]
        public unsafe void MoveNextTest2()
        {
            long lowKey = 20;
            long highKey = 400;
            PersistedCellEnumerator pe = new PersistedCellEnumerator(contentInt64, lowKey, highKey);
            var current = pe.Current;

            Assert.AreEqual(true, pe.MoveNext());
            Assert.AreEqual(false, pe.MoveNext());
        }
        [TestMethod]
        public unsafe void MoveNextTest3()
        {
            long lowKey = 20;
            long highKey = 400;
            PersistedCellEnumerator pe = new PersistedCellEnumerator(contentString, lowKey, highKey);
            var current = pe.Current;
            Assert.AreEqual(true, pe.MoveNext());
            Assert.AreEqual(false, pe.MoveNext());
        }
    }
}
