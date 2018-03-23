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
            int temp = 0;
            for (int i = 0; i <= Total; i += unit)
            {
                if (i > cuttentThreshold)
                {
                    cuttentThreshold *= 2;
                    time++;
                }
                temp = i;
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
        private List<CellInfo> GetDoubleCellInfo(int cellCount)
        {
            for (int i = 0; i < cellCount; i++)
            {
                DoubleCell doubleCell = new DoubleCell(0.001);
                Global.LocalStorage.SaveDoubleCell(doubleCell);
            }
            var cells = Global.LocalStorage.GetEnumerator();
            List<CellInfo> cellInfoList = new List<CellInfo>();
            while (cells.MoveNext())
                cellInfoList.Add(cells.Current);
            return cellInfoList;
        }
        private List<CellInfo> GetStringCellInfo(int cellCount, int charCount)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < charCount; i++)
            {
                sb.Append('a');
            }
            for (int i = 0; i < cellCount; i++)
            {
                StringCell stringCell = new StringCell(sb.ToString());
                Global.LocalStorage.SaveStringCell(stringCell);
            }
            var cells = Global.LocalStorage.GetEnumerator();
            List<CellInfo> cellInfoList = new List<CellInfo>();
            while (cells.MoveNext())
                cellInfoList.Add(cells.Current);
            return cellInfoList;
        }
        private List<CellInfo> GetStringListCellInfo(int cellCount, int charCount, int listLength)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < charCount; i++)
                sb.Append('a');
            List<string> content = new List<string>();
            for (int i = 0; i < listLength; i++)
                content.Add(sb.ToString());
            for (int i = 0; i < cellCount; i++)
            {
                ListCell listCell = new ListCell(content);
                Global.LocalStorage.SaveListCell(listCell);
            }
            var cells = Global.LocalStorage.GetEnumerator();
            List<CellInfo> cellInfoList = new List<CellInfo>();
            while (cells.MoveNext())
                cellInfoList.Add(cells.Current);
            return cellInfoList;
        }
        private List<CellInfo> GetNestStringListCellInfo(int cellCount, int charCount, int structLength, int structListLength)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < charCount; i++)
                sb.Append('a');
            List<string> content = new List<string>();
            for (int i = 0; i < structListLength; i++)
                content.Add(sb.ToString());
            List<MyStruct> cellContent = new List<MyStruct>();
            for (int i = 0; i < structLength; i++)
                cellContent.Add(new MyStruct(new List<string>(content)));

            for (int i = 0; i < cellCount; i++)
            {
                ComplexCell cc = new ComplexCell(cellContent);
                Global.LocalStorage.SaveComplexCell(cc);
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
            Assert.AreEqual(CalcMemoryCapacity(cellCount, eachCellSize, (int)Global.LocalStorage.TotalCellSize), capacity.Length);
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
        [TestMethod]
        public unsafe void NewChunkTest4()
        {
            int cellCount = 100;
            int eachCellSize = sizeof(double);
            var cellInfoList = GetDoubleCellInfo(cellCount);
            var obj = InMemoryDataChunk.New(cellInfoList, (int)Global.LocalStorage.TotalCellSize);
            var capacity = obj.GetBuffer();
            Assert.AreEqual(CalcMemoryCapacity(cellCount, eachCellSize, (int)Global.LocalStorage.TotalCellSize), capacity.Length);
        }
        [TestMethod]
        public unsafe void NewChunkTest5()
        {
            int cellCount = 100;
            int charCount = 10000;
            var cellInfoList = GetStringCellInfo(cellCount, charCount);
            int eachCellSize = 2 * charCount + 4;
            var obj = InMemoryDataChunk.New(cellInfoList, (int)Global.LocalStorage.TotalCellSize);
            var capacity = obj.GetBuffer();
            Assert.AreEqual(CalcMemoryCapacity(cellCount, eachCellSize, (int)Global.LocalStorage.TotalCellSize), capacity.Length);
        }
        [TestMethod]
        public unsafe void NewChunkTest6()
        {
            int cellCount = 100;
            int charCount = 10;
            var listLength = 10000000;
            var cellInfoList = GetStringListCellInfo(cellCount, charCount, listLength);
            var eachCellSize = (2 * charCount + 4) * listLength + 4;
            var obj = InMemoryDataChunk.New(cellInfoList, (int)Global.LocalStorage.TotalCellSize);
            var capacity = obj.GetBuffer();
            Assert.AreEqual(CalcMemoryCapacity(cellCount, eachCellSize, (int)Global.LocalStorage.TotalCellSize), capacity.Length);
        }
        [TestMethod]
        public unsafe void NewChunkTest7()
        {
            int cellCount = 100;
            int charCount = 10;
            int listStructLength = 10000;
            int listInStructLength = 10000;
            var cellInfoList = GetNestStringListCellInfo(cellCount, charCount, listStructLength, listInStructLength);
            var eachCellSize = (int)Global.LocalStorage.TotalCellSize / 100;
            var obj = InMemoryDataChunk.New(cellInfoList, (int)Global.LocalStorage.TotalCellSize);
            var capacity = obj.GetBuffer();
            Assert.AreEqual(CalcMemoryCapacity(cellCount, eachCellSize, (int)Global.LocalStorage.TotalCellSize), capacity.Length);
        }
    }
}
