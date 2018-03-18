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

namespace Trinity.DynamicCluster.Test
{
    [TestClass]
    public class PartitionTests
    {
        List<Chunk> cks = new List<Chunk> { Chunk.FullRangeChunk };
        byte[] buf = new byte[16];
        unsafe byte* bp;
        private TrinityMessage tm;
        private GCHandle gchandle;

        [TestInitialize]
        public unsafe void Init()
        {
            gchandle = GCHandle.Alloc(buf, GCHandleType.Pinned);
            bp = (byte*)gchandle.AddrOfPinnedObject().ToPointer();
            tm = new TrinityMessage(bp, 16);
        }

        [TestCleanup]
        public void Cleanup()
        {
            gchandle.Free();
        }

        [TestMethod]
        public void PartitionInit()
        {
            var p = new Partition();
            p.Dispose();
        }

        [TestMethod]
        public unsafe void PartitionMount()
        {
            var stg = new IStorage1();
            using (var p = new Partition())
            {
                p.Mount(stg, cks);
                p.SendMessage(tm);
            }
            Assert.IsTrue(stg.SendMessageCalledOnce);
        }

        [TestMethod]
        public unsafe void PartitionRR()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 5; ++i)
                {
                    p.RoundRobin(_ => _.SendMessage(tm));
                }
            }
            Assert.IsTrue(stgs.All(_ => _.SendMessageCalledOnce));
        }

        [TestMethod]
        public unsafe void PartitionRR2()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 5; ++i)
                {
                    p.RoundRobin(_ => { _.SendMessage(tm, out var tr); return tr; });
                }
            }
            Assert.IsTrue(stgs.All(_ => _.SendMessageCalledOnce));
        }

        [TestMethod]
        public unsafe void PartitionRR3()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 5; ++i)
                {
                    p.RoundRobin(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                }
            }
            Assert.IsTrue(stgs.All(_ => _.SendMessageCalledOnce));
        }

        [TestMethod]
        public unsafe void PartitionFirstAvailable1()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 5; ++i)
                {
                    p.FirstAvailable(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                }
            }
            Assert.IsTrue(stgs.Any(_ => _.cnt == 5));
        }

        [TestMethod]
        public unsafe void PartitionFirstAvailable4()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 3; ++i)
                {
                    p.FirstAvailable(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                }
                int idx = stgs.FindIndex(_ => _.cnt == 3);
                p.Unmount(stgs[idx]);
                for (int i = 0; i < 2; ++i)
                {
                    p.FirstAvailable(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                }

            }
            Assert.IsTrue(stgs.Any(_ => _.cnt == 3));
            Assert.IsTrue(stgs.Any(_ => _.cnt == 2));
        }

        [TestMethod]
        public unsafe void PartitionFirstAvailable5()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 3; ++i)
                {
                    p.FirstAvailable(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                }
                int idx = stgs.FindIndex(_ => _.cnt != 3);
                p.Unmount(stgs[idx]);
                for (int i = 0; i < 2; ++i)
                {
                    p.FirstAvailable(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                }

            }
            Assert.IsTrue(stgs.Any(_ => _.cnt == 5));
        }

        [TestMethod]
        public unsafe void PartitionFirstAvailable2()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 5; ++i)
                {
                    p.FirstAvailable(_ => { _.SendMessage(tm, out var rsp); return rsp; });
                }
            }
            Assert.IsTrue(stgs.Any(_ => _.cnt == 5));
        }

        [TestMethod]
        public unsafe void PartitionFirstAvailable3()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 5; ++i)
                {
                    p.FirstAvailable(_ => _.SendMessage(tm));
                }
            }
            Assert.IsTrue(stgs.Any(_ => _.cnt == 5));
        }

        [TestMethod]
        public unsafe void PartitionUniformRandom1()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 1024; ++i)
                {
                    p.UniformRandom(_ => _.SendMessage(tm));
                }
            }
            Assert.AreEqual(204.8, stgs.Average(_ => (double)_.cnt));
        }
        [TestMethod]
        public unsafe void PartitionUniformRandomTest1()
        {
            int loopTimes = 10;
            int istorageListCount = 20;
            int messageSenderCount = 1000;
            var stgs = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            for (int i = 0; i < loopTimes; i++)
            {
                using (var p = new Partition())
                {
                    foreach (var s in stgs) p.Mount(s, cks);
                    for (int j = 0; j < messageSenderCount; ++j)
                        p.UniformRandom(_ => _.SendMessage(tm));
                }
                Assert.AreEqual(messageSenderCount * (i + 1) / istorageListCount, stgs.Average(_ => _.cnt));
            }
            Assert.AreEqual(messageSenderCount * loopTimes / istorageListCount, stgs.Average(_ => _.cnt));
        }

        [TestMethod]
        public unsafe void PartitionUniformRandom2()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 1024; ++i)
                {
                    p.UniformRandom(_ => { _.SendMessage(tm, out var rsp); return rsp; });
                }
            }
            Assert.AreEqual(204.8, stgs.Average(_ => (double)_.cnt));
        }
        [TestMethod]
        public unsafe void PartitionUniformRandomTest2()
        {
            int loopTimes = 10;
            int istorageListCount = 20;
            int messageSenderCount = 1000;

            var stgs = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            for (int j = 0; j < loopTimes; j++)
            {
                using (var p = new Partition())
                {
                    foreach (var s in stgs) p.Mount(s, cks);
                    for (int i = 0; i < messageSenderCount; ++i)
                    {
                        p.UniformRandom(_ => { _.SendMessage(tm, out var rsp); return rsp; });
                    }
                }
                Assert.AreEqual((messageSenderCount * (j + 1)) / istorageListCount, stgs.Average(_ => _.cnt));
            }
            Assert.AreEqual(messageSenderCount * loopTimes / istorageListCount, stgs.Average(_ => _.cnt));
        }
        [TestMethod]
        public unsafe void PartitionUniformRandom3()
        {
            var stgs = Utils.Infinity<IStorage1>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stgs) p.Mount(s, cks);
                for (int i = 0; i < 1024; ++i)
                {
                    p.UniformRandom(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                }
            }
            Assert.AreEqual(204.8, stgs.Average(_ => (double)_.cnt));
        }
        [TestMethod]
        public unsafe void PartitionUniformRandomTest3()
        {
            int loopTimes = 10;
            int istorageListCount = 20;
            int messageSenderCount = 1000;

            var stgs = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            for (int j = 0; j < loopTimes; j++)
            {
                using (var p = new Partition())
                {
                    foreach (var s in stgs) p.Mount(s, cks);
                    for (int i = 0; i < messageSenderCount; ++i)
                    {
                        p.UniformRandom(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                    }
                }
                Assert.AreEqual((messageSenderCount * (j + 1)) / istorageListCount, stgs.Average(_ => _.cnt));
            }
            Assert.AreEqual(messageSenderCount * loopTimes / istorageListCount, stgs.Average(_ => _.cnt));
        }
        [TestMethod]
        public unsafe void Broadcast1()
        {
            var stg1s = Utils.Infinity<IStorage1>().Take(5).ToList();
            var stg2s = Utils.Infinity<IStorage2>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);

                p.Broadcast(_ => _.SendMessage(tm));
                Assert.IsTrue(stg1s.All(_ => _.cnt == 1));
                foreach (var s in stg2s) p.Mount(s, cks);
                try
                {
                    p.Broadcast(_ => _.SendMessage(tm));
                    Assert.Fail();
                }
                catch (BroadcastException<TrinityResponse> bex)
                {
                    throw;
                }
                catch (BroadcastException ex)
                {
                    Assert.AreEqual(5, ex.Exceptions.Count());
                }
                Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
            }
        }
        [TestMethod]
        public unsafe void Broadcast12()
        {
            var stg1s = Utils.Infinity<IStorage1>().Take(5).ToList();
            var stg2s = Utils.Infinity<IStorage2>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);

                p.Broadcast(_ => _.SendMessage(tm));
                Assert.IsTrue(stg1s.All(_ => _.cnt == 1));
                foreach (var s in stg2s) p.Mount(s, cks);
                try
                {
                    p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return rsp; });
                    Assert.Fail();
                }
                catch (BroadcastException<TrinityResponse> bex)
                {
                    Assert.AreEqual(5, bex.Exceptions.Count());
                    Assert.AreEqual(5, bex.Results.Count());
                    bex.Dispose();
                }
                catch (BroadcastException ex) { throw; }
                Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
            }
        }
        [TestMethod]
        public async Task Broadcast13()
        {
            var stg1s = Utils.Infinity<IStorage1>().Take(5).ToList();
            var stg2s = Utils.Infinity<IStorage2>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);

                p.Broadcast(_ => _.SendMessage(tm));
                Assert.IsTrue(stg1s.All(_ => _.cnt == 1));
                foreach (var s in stg2s) p.Mount(s, cks);
                try
                {
                    await p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                    Assert.Fail();
                }
                catch (BroadcastException<TrinityResponse> bex)
                {
                    Assert.AreEqual(5, bex.Exceptions.Count());
                    Assert.AreEqual(5, bex.Results.Count());
                    bex.Dispose();
                }
                catch (BroadcastException ex) { throw; }
                Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
            }
        }
        [TestMethod]
        public unsafe void Broadcast2()
        {
            var stg1s = Utils.Infinity<IStorage1>().Take(5).ToList();
            var stg2s = Utils.Infinity<IStorage2>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return rsp; });
                Assert.IsTrue(stg1s.All(_ => _.cnt == 1));
                foreach (var s in stg2s) p.Mount(s, cks);
                try
                {
                    p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return rsp; });
                    Assert.Fail();
                }
                catch (BroadcastException<TrinityResponse> bex)
                {
                    Assert.AreEqual(5, bex.Exceptions.Count());
                    Assert.AreEqual(5, bex.Results.Count());
                    bex.Dispose();
                }
                catch (BroadcastException ex) { throw; }
                Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
            }
        }
        [TestMethod]
        public unsafe void Broadcast21()
        {
            var stg1s = Utils.Infinity<IStorage1>().Take(5).ToList();
            var stg2s = Utils.Infinity<IStorage2>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return rsp; });
                Assert.IsTrue(stg1s.All(_ => _.cnt == 1));
                foreach (var s in stg2s) p.Mount(s, cks);
                try
                {
                    p.Broadcast(_ => _.SendMessage(tm));
                    Assert.Fail();
                }
                catch (BroadcastException<TrinityResponse> bex)
                {
                    throw;
                }
                catch (BroadcastException ex) { Assert.AreEqual(5, ex.Exceptions.Count()); }
                Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
            }
        }
        [TestMethod]
        public async Task Broadcast23()
        {
            var stg1s = Utils.Infinity<IStorage1>().Take(5).ToList();
            var stg2s = Utils.Infinity<IStorage2>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return rsp; });
                Assert.IsTrue(stg1s.All(_ => _.cnt == 1));
                foreach (var s in stg2s) p.Mount(s, cks);
                try
                {
                    await p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                    Assert.Fail();
                }
                catch (BroadcastException<TrinityResponse> bex)
                {
                    Assert.AreEqual(5, bex.Exceptions.Count());
                    Assert.AreEqual(5, bex.Results.Count());
                    bex.Dispose();
                }
                catch (BroadcastException ex) { throw; }
                Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
            }
        }
        [TestMethod]
        public async Task Broadcast3()
        {
            var stg1s = Utils.Infinity<IStorage1>().Take(5).ToList();
            var stg2s = Utils.Infinity<IStorage2>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                await p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                Assert.IsTrue(stg1s.All(_ => _.cnt == 1));
                foreach (var s in stg2s) p.Mount(s, cks);
                try
                {
                    await p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                    Assert.Fail();
                }
                catch (BroadcastException<TrinityResponse> bex)
                {
                    Assert.AreEqual(5, bex.Exceptions.Count());
                    Assert.AreEqual(5, bex.Results.Count());
                    bex.Dispose();
                }
                catch (BroadcastException ex) { throw; }
                Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
            }
        }
        [TestMethod]
        public async Task Broadcast31()
        {
            var stg1s = Utils.Infinity<IStorage1>().Take(5).ToList();
            var stg2s = Utils.Infinity<IStorage2>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                await p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                Assert.IsTrue(stg1s.All(_ => _.cnt == 1));
                foreach (var s in stg2s) p.Mount(s, cks);
                try
                {
                    p.Broadcast(_ => _.SendMessage(tm));
                    Assert.Fail();
                }
                catch (BroadcastException<TrinityResponse> bex)
                {
                    throw;
                }
                catch (BroadcastException ex) { Assert.AreEqual(5, ex.Exceptions.Count()); }
                Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
            }
        }
        [TestMethod]
        public async Task Broadcast32()
        {
            var stg1s = Utils.Infinity<IStorage1>().Take(5).ToList();
            var stg2s = Utils.Infinity<IStorage2>().Take(5).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                await p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); });
                Assert.IsTrue(stg1s.All(_ => _.cnt == 1));
                foreach (var s in stg2s) p.Mount(s, cks);
                try
                {
                    p.Broadcast(_ => { _.SendMessage(tm, out var rsp); return rsp; });
                    Assert.Fail();
                }
                catch (BroadcastException<TrinityResponse> bex)
                {
                    Assert.AreEqual(5, bex.Exceptions.Count());
                    Assert.AreEqual(5, bex.Results.Count());
                    bex.Dispose();
                }
                catch (BroadcastException ex)
                {
                    throw;
                }
                Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
            }
        }
        [TestMethod]
        public void VoteTest1()
        {
            int istorageListCount = 100;
            int threshold = 50;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                p.Vote(_ => _.SendMessage(tm), threshold);
            }
            Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
        }
        [TestMethod]
        public void VoteTest3()
        {
            int istorageListCount = 100;
            int threshold = 100;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                try
                {
                    p.Vote(_ => { _.SendMessage(tm, out var rsp); return rsp; }, threshold);
                }
                catch (Exception ex) { }
            }
            Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
        }
        [TestMethod]
        public void VoteTest5()
        {
            int istorageListCount = 100;
            int threshold = 99;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                try
                {
                    p.Vote(_ => { _.SendMessage(tm, out var rsp); return rsp; }, threshold);
                }
                catch (Exception ex) { }
            }
            Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
        }
        [TestMethod]
        public void VoteTest4()
        {

            int istorageListCount = 100;
            int threshold = istorageListCount + 1;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                try
                {
                    p.Vote(_ => { _.SendMessage(tm, out var rsp); return rsp; }, threshold);
                }
                catch (Exception ex)
                {
                }
            }
            Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
        }
        [TestMethod]
        public void VoteTest2()
        {

            int istorageListCount = 100;
            int threshold = istorageListCount + 1;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                try
                {
                    p.Vote(_ => _.SendMessage(tm), threshold);
                }
                catch (Exception ex)
                {
                }
            }
            Assert.IsTrue(stg1s.All(_ => _.cnt == 2));
        }
        [TestMethod]
        public async Task VoteTest6()
        {
            int istorageListCount = 100;
            int threshold = 100;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                try
                {
                    await p.Vote(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); }, threshold);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Assert.IsTrue(stg1s.All(_ => _.cnt==2));
        }
        [TestMethod]
        public async Task VoteTest7()
        {
            int istorageListCount = 100;
            int threshold = 105;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                try
                {
                    await p.Vote(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); }, threshold);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Assert.IsTrue(stg1s.All(_ => _.cnt==2));
        }
        [TestMethod]
        public async Task VoteTest8()
        {
            int istorageListCount = 100;
            int threshold = 50;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                try
                {
                    await p.Vote(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); }, threshold);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Assert.AreEqual(stg1s.Average(_ => _.cnt), 2);
        }
        [TestMethod]
        public async Task VoteTest9()
        {
            int istorageListCount = 10;
            int threshold = 0;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                await p.Vote(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); }, threshold);
            }
            Assert.IsTrue( stg1s.All(_ => _.cnt==3));
        }
        [TestMethod]
        public async Task VoteTest10()
        {
            int istorageListCount = 10;
            int threshold = 1;

            var stg1s = Utils.Infinity<IStorage1>().Take(istorageListCount).ToList();
            using (var p = new Partition())
            {
                foreach (var s in stg1s) p.Mount(s, cks);
                await p.Vote(_ => { _.SendMessage(tm, out var rsp); return Task.FromResult(rsp); }, threshold);
            }
            Assert.IsTrue(stg1s.All(_ => _.cnt==3));
        }
    }
}
