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
        private int CalcMemoryCapacity(int cellCount, int eachCellSize, int initCapacity)
        {
            int minThreshold = 256;
            int unit = cellInfoSize + eachCellSize;
            if (initCapacity <= minThreshold)
            {
                return CalcMemoryCapacityImp(unit, cellCount, minThreshold);
            }
            else
            {
                minThreshold = initCapacity;
                return CalcMemoryCapacityImp(unit, cellCount, minThreshold);
            }
        }
        private int CalcMemoryCapacityImp(int unit, int cellCount, int minThreshold)
        {
            int total = unit * cellCount;
            if (total < minThreshold)
                return minThreshold;
            else
            {
                var time = CalcTimes(total, minThreshold, unit);
                return minThreshold * (1 << time);
            }
        }
        private int CalcTimes(int Total, int minThreshold, int unit)
        {
            int cuttentThreshold = minThreshold;
            int time = 0;
            for (int i = 0; i < Total; i += unit)
                if (i > cuttentThreshold)
                {
                    cuttentThreshold *= 2;
                    time++;
                }
            return time;
        }
        private List<CellInfo> GetIntCellInfo(int cellCount)
        {
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
            var cellInfoList = GetIntCellInfo(5);
            var result = InMemoryDataChunk.New(cellInfoList, threshold);
        }
        [TestMethod]
        public unsafe void NewChunkTest1()
        {
            var cellInfoList = GetIntCellInfo(5);
            var result = InMemoryDataChunk.New(cellInfoList, validThreshold);
        }
        [TestMethod]
        public unsafe void NewChunkTest2()
        {
            int cellCount = 5;
            int eachCellSize = sizeof(int);
            var cellInfoList = GetIntCellInfo(cellCount);
            var obj = InMemoryDataChunk.New(cellInfoList, (int)Global.LocalStorage.TotalCellSize);
            var capacity = obj.GetBuffer();
            Assert.AreEqual(CalcMemoryCapacity(cellCount,eachCellSize,(int)Global.LocalStorage.TotalCellSize), capacity.Length);
        }
        [TestMethod]
        public unsafe void NewChunkTest3()
        {
            int cellCount = 100;
            int eachCellSize = sizeof(int);
            var cellInfoList = GetIntCellInfo(cellCount);
            var obj = InMemoryDataChunk.New(cellInfoList, (int)Global.LocalStorage.TotalCellSize);
            var capacity = obj.GetBuffer();
            Assert.AreEqual(CalcMemoryCapacity(cellCount, eachCellSize, (int)Global.LocalStorage.TotalCellSize), capacity.Length);
        }
    }
}
