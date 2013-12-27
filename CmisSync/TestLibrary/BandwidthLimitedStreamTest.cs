using System;
using System.IO;

using CmisSync.Lib;
using CmisSync.Lib.Events;
using CmisSync.Lib.Streams;

using NUnit.Framework;

namespace TestLibrary
{
    [TestFixture]
    public class BandwidthLimitedStreamTest
    {
        private int Length;
        private byte[] Buffer;

        [SetUp]
        public void Setup ()
        {
            Length = 1024 * 1024;
            Buffer = new byte[Length];
        }

        [Test, Category("Fast")]
        public void ConstructorTest ()
        {
            long limit = 0;
            try {
                using (Stream memory = new MemoryStream(Buffer))
                    using (BandwidthLimitedStream limited = new BandwidthLimitedStream(memory, limit))
                        Assert.Fail ();
            } catch (ArgumentException) {
            }
            limit = -1;
            try {
                using (Stream memory = new MemoryStream(Buffer))
                    using (BandwidthLimitedStream limited = new BandwidthLimitedStream(memory, limit))
                        Assert.Fail ();
            } catch (ArgumentException) {
            }
            limit = 1;
            try {
                using (BandwidthLimitedStream limited = new BandwidthLimitedStream(null, limit))
                    Assert.Fail ();
            } catch (ArgumentNullException) {
            }

            limit = -10;
            try {
                using (BandwidthLimitedStream limited = new BandwidthLimitedStream(null, limit))
                    Assert.Fail ();
            } catch (ArgumentNullException) {
            } catch (ArgumentException) {
            }
            limit = Length;
            using (Stream memory = new MemoryStream(Buffer))
            using (BandwidthLimitedStream limited = new BandwidthLimitedStream(memory, limit)) {
                Assert.AreEqual (limit, limited.ReadLimit);
                Assert.AreEqual (limit, limited.WriteLimit);
            }

            using (Stream memory = new MemoryStream(Buffer))
            using (BandwidthLimitedStream limited = new BandwidthLimitedStream(memory)) {
                Assert.Less (limited.ReadLimit, 0);
                Assert.Less (limited.WriteLimit, 0);
            }
        }

        [Ignore]
        [Test, Category("Slow")]
        public void ReadTest ()
        {
            byte [] buf = new byte[Length];
            int counter = 0;
            using (MemoryStream memstream = new MemoryStream(this.Buffer))
            using (BandwidthLimitedStream stream = new BandwidthLimitedStream(memstream, limit: Length)) {
                DateTime start = DateTime.Now;
                while(counter < Length)
                    counter += stream.Read (buf, 0, Length-counter);
                TimeSpan duration = DateTime.Now - start;
                Assert.GreaterOrEqual (duration.TotalMilliseconds, 1000);
                Assert.AreEqual (1, duration.TotalSeconds);
            }
            counter = 0;
            using (MemoryStream memstream = new MemoryStream(this.Buffer))
            using (BandwidthLimitedStream stream = new BandwidthLimitedStream(memstream)) {
                DateTime start = DateTime.Now;
                while(counter < Length)
                    counter += stream.Read (buf, 0, Length-counter);
                TimeSpan duration = DateTime.Now - start;
                Assert.GreaterOrEqual (duration.TotalMilliseconds, 1000);
                Assert.AreEqual (1, duration.TotalSeconds);
            }
        }

        [Ignore]
        [Test, Category("Slow")]
        public void WriteTest ()
        {
            byte [] buf = new byte[Length];
            using (MemoryStream memstream = new MemoryStream(this.Buffer))
            using (BandwidthLimitedStream stream = new BandwidthLimitedStream(memstream, limit: Length)) {
                DateTime start = DateTime.Now;
                stream.Write (buf, 0, Length);
                TimeSpan duration = DateTime.Now - start;
                Assert.GreaterOrEqual (duration.TotalMilliseconds, 1000);
                Assert.AreEqual (1, duration.TotalSeconds);
            }

            using (MemoryStream memstream = new MemoryStream(this.Buffer))
            using (BandwidthLimitedStream stream = new BandwidthLimitedStream(memstream)) {
                DateTime start = DateTime.Now;
                stream.Write (buf, 0, Length);
                TimeSpan duration = DateTime.Now - start;
                Assert.GreaterOrEqual (duration.TotalMilliseconds, 1000);
                Assert.AreEqual (1, duration.TotalSeconds);
            }
        }

        [Test, Category("Fast")]
        public void ConfigureLimitsTest ()
        {
            long limit = Length;
            using (Stream memory = new MemoryStream(Buffer))
            using (BandwidthLimitedStream limited = new BandwidthLimitedStream(memory)) {
                Assert.Less(limited.ReadLimit, 0);
                Assert.Less(limited.WriteLimit, 0);
                limited.ReadLimit = limit;
                Assert.AreEqual(limit, limited.ReadLimit);
                Assert.Less(limited.WriteLimit, 0);
                limited.WriteLimit = limit;
                Assert.AreEqual(limit, limited.ReadLimit);
                Assert.AreEqual(limit, limited.WriteLimit);
                limited.DisableLimits();
                Assert.Less(limited.ReadLimit, 0);
                Assert.Less(limited.WriteLimit, 0);
                limited.ReadLimit = limit;
                limited.WriteLimit = limit;
                Assert.AreEqual(limit, limited.ReadLimit);
                Assert.AreEqual(limit, limited.WriteLimit);
                limited.DisableReadLimit();
                Assert.Less(limited.ReadLimit, 0);
                Assert.AreEqual(limit, limited.WriteLimit);
                limited.DisableWriteLimit();
                Assert.Less(limited.ReadLimit, 0);
                Assert.Less(limited.WriteLimit, 0);
            }
        }
    }
}

