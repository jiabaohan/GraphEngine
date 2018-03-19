using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Trinity.DynamicCluster.Storage;
using Trinity.DynamicCluster.Test.Mocks;
using Trinity.Network.Messaging;
using Trinity.Storage;
using Trinity.DynamicCluster.Replication;
using Trinity.DynamicCluster.Tasks;
namespace Trinity.DynamicCluster.Test
{
    [TestClass]
    public class ReplicationPlannerTests
    {
        private List<(ReplicaInformation rp, IEnumerable<Chunk> cks)> GenerateInput()
        {
            int repInfoCount = 10;
            int chunkListLength = 20;
            long start = 0;
            long range = 1 << 30;
            List<ReplicaInformation> repInfoList = new List<ReplicaInformation>();
            for (int i = 0; i < repInfoCount; i++)
            {
                repInfoList.Add(new ReplicaInformation("hostname:" + i.ToString(), 800 + i, new Guid(), i));
            }
            List<List<Chunk>> ckList = new List<List<Chunk>>();
            for (int i = 0; i < repInfoCount; i++)
            {
                List<Chunk> cks = new List<Chunk>();
                for (int j = 0; j < chunkListLength; j++)
                {
                    cks.Add(new Chunk(start, start + range));
                    start = start + range;
                }
                ckList.Add(cks);
            }
            List<(ReplicaInformation rp, IEnumerable<Chunk> cks)> result = new List<(ReplicaInformation rp, IEnumerable<Chunk> cks)>();
            for (int i = 0; i < 10; i++)
            {
                result.Add((repInfoList[i], ckList[i]));
            }
            return result;
        }
        [TestMethod]
        public void ShardingPlannerTest()
        {
            ShardingPlanner sp = new ShardingPlanner();
            var input = GenerateInput();
            var result = sp.Plan(3, input);
            List<ITask> final = new List<ITask>();
            foreach (var task in result)
                final.Add(task);
            Console.WriteLine();
        }
        [TestMethod]
        public void PlannerTest()
        {

            MirroringPlanner mp = new MirroringPlanner();
            var input = GenerateInput();
            var result = mp.Plan(3, input);
            List<ITask> final = new List<ITask>();
            foreach (var task in result)
                final.Add(task);
            Console.WriteLine();
        }
    }
}
