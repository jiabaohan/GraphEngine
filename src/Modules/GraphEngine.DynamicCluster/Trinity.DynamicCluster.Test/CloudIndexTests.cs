using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trinity.DynamicCluster.Consensus;
using Trinity.DynamicCluster.Storage;
using Trinity.Storage;
namespace Trinity.DynamicCluster.Test
{
    [TestClass]
    public class CloudIndexTests
    {
        static CloudIndex ci = default(CloudIndex);
        private void InitCloudIndex(bool isMaster, int partitionCount, int resolvePartition, Func<int, Partition> ptable = null)
        {
            CancellationTokenSource tsrc = new CancellationTokenSource();
            var namesvc = Mock.Of<INameService>();
            var ctable = Mock.Of<IChunkTable>();
            Guid id = Guid.NewGuid();

            Mock.Get(namesvc).Setup(svc => svc.IsMaster).Returns(isMaster);
            Mock.Get(namesvc).Setup(svc => svc.PartitionCount).Returns(partitionCount);
            Mock.Get(namesvc).Setup(svc => svc.InstanceId).Returns(id);
            Mock.Get(namesvc).Setup(svc => svc.ResolvePartition(resolvePartition))
                .ReturnsAsync(new List<ReplicaInformation> { new ReplicaInformation("localhost", 9999, id, 0) });

            CloudIndex _ci = new CloudIndex(tsrc.Token, namesvc, ctable, null, "myname", ptable);
            ci = _ci;
        }
        [TestMethod]
        public async Task CloudIndexProbesNameservice()
        {
            CancellationTokenSource tsrc = new CancellationTokenSource();
            var namesvc = Mock.Of<INameService>();
            var ctable = Mock.Of<IChunkTable>();
            Guid id = Guid.NewGuid();

            Mock.Get(namesvc).Setup(svc => svc.IsMaster).Returns(true);
            Mock.Get(namesvc).Setup(svc => svc.PartitionCount).Returns(1);
            Mock.Get(namesvc).Setup(svc => svc.InstanceId).Returns(id);
            Mock.Get(namesvc).Setup(svc => svc.ResolvePartition(0))
                .ReturnsAsync(new List<ReplicaInformation> { new ReplicaInformation("localhost", 9999, id, 0) });

            CloudIndex ci = new CloudIndex(tsrc.Token, namesvc, ctable, null, "myname", _ => null);
            await Task.Delay(1000);
            Mock.Get(namesvc).Verify(_ => _.PartitionCount, Times.AtLeastOnce);
            Mock.Get(namesvc).Verify(_ => _.ResolvePartition(It.IsAny<int>()), Times.AtLeastOnce);
        }
        [TestMethod]
        [ExpectedException(typeof(NoSuitableReplicaException),
         "One or more partition masters not found.")]
        public void GetMastersTest()
        {
            InitCloudIndex(true, 3, 0);
            ci.GetMasters();
        }
        [TestMethod]
        [ExpectedException(typeof(NotMasterException))]
        public void GetMastersTest1()
        {
            InitCloudIndex(false, 3, 0);
            ci.GetMasters();
        }
        [TestMethod]
        public void GetMasterTest()
        {
            int partitionCount = 4;
            InitCloudIndex(true, partitionCount, 0);
            var result = Utils.Integers(partitionCount).Select(ci.GetMaster);
            if (result.Any(_ => _ != null))
                Assert.Fail();
        }
        [TestMethod]
        public void SetMasterTest()
        {
            throw new NotImplementedException();
        }
        [TestMethod]
        public void GetMyPartitionReplicaChunksTest()
        {
            throw new NotImplementedException();
        }
        [TestMethod]
        public void GetStorageTest()
        {
            throw new NotImplementedException();
        }
        [TestMethod]
        public void SetStorageTest()
        {
            throw new NotImplementedException();
        }
        [TestMethod]
        public void SetChunksTest()
        {
            throw new NotImplementedException();
        }
        [TestMethod]
        public void GetChunksTest()
        {
            throw new NotImplementedException();
        }
    }
}
