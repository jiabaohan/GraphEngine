using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trinity.Storage;
using Trinity;
using Trinity.DynamicCluster.Persistency;
using System.IO;

namespace Trinity.DynamicCluster.Test
{
    [TestClass]
    public class NewMemoryDataChunkTest
    {
        int threshold = Int32.MaxValue - 55;
        int validThreshold = Int32.MaxValue - 56;
        int cellInfoSize = sizeof(long) + sizeof(ushort) + sizeof(int);
        private List<CellInfo> GetIntCellInfo()
        {
            int cellCount = 5;
            for (int i = 0; i < cellCount; i++)
            {
                IntCell intCell = new IntCell(1);
                Global.LocalStorage.SaveIntCell(intCell);
            }
            var cells = Global.LocalStorage.GetEnumerator();
            List<CellInfo> cellInfoList = new List<CellInfo>();
            while (cells.MoveNext())
                cellInfoList.Add(cells.Current);
            return cellInfoList;
        }
        [TestMethod]
        public unsafe void NewChunkTest()
        {
            var cellInfoList = GetIntCellInfo();
            var result = InMemoryDataChunk.New(cellInfoList, threshold);
        }
        [TestMethod]
        public unsafe void NewChunkTest1()
        {
            var cellInfoList = GetIntCellInfo();
            var result = InMemoryDataChunk.New(cellInfoList, validThreshold);
        }
        [TestMethod]
        public unsafe void NewChunkTest2()
        {
            var cellInfoList = GetIntCellInfo();
            var obj = InMemoryDataChunk.New(cellInfoList, (int)Global.LocalStorage.TotalCellSize);
            var capacity = obj.GetBuffer();

            Console.WriteLine();
        }
    }
}
