using System;

using CmisSync.Lib;
using CmisSync.Lib.Events;
using CmisSync.Lib.Sync.Strategy;

using DotCMIS.Client;

using NUnit.Framework;

using Moq;

namespace TestLibrary.SyncStrategiesTests
{
    [TestFixture]
    public class ContentChangesTest
    {
        [Test, Category("Fast")]
        public void ConstructorTest()
        {
            var session = new Mock<ISession>().Object;
            var db = new Mock<IDatabase>().Object;
            var manager = new Mock<SyncEventManager>().Object;
            var queue = new Mock<SyncEventQueue>(manager).Object;
            int maxNumberOfContentChanges = 1000;
            bool isPropertyChangesSupported = true;
            new ContentChanges(session, db, queue);
            new ContentChanges(session, db, queue, maxNumberOfContentChanges);
            new ContentChanges(session, db, queue, maxNumberOfContentChanges, isPropertyChangesSupported);
            new ContentChanges(session, db, queue, isPropertyChangesSupported: true);
            new ContentChanges(session, db, queue, isPropertyChangesSupported: false);
            try{
                new ContentChanges(session, db, queue, -1);
                Assert.Fail();
            }catch(ArgumentException){}
            try{
                new ContentChanges(null, db, queue);
                Assert.Fail();
            }catch(ArgumentNullException){}
            try{
                new ContentChanges(session, null, queue);
                Assert.Fail();
            }catch(ArgumentNullException){}
            try{
                new ContentChanges(session, db, null);
                Assert.Fail();
            }catch(ArgumentNullException){}
        }

        [Test, Category("Fast")]
        public void IgnoreWrongEventTest() {
            var session = new Mock<ISession>().Object;
            var db = new Mock<IDatabase>().Object;
            var manager = new Mock<SyncEventManager>().Object;
            var queue = new Mock<SyncEventQueue>(manager).Object;
            var changes = new ContentChanges(session, db, queue);
            var wrongEvent = new Mock<ISyncEvent>().Object;
            Assert.IsFalse(changes.Handle(wrongEvent));
        }

        [Test, Category("Fast")]
        public void HandleFullSyncCompletedEventTest() {
            string changeLogToken = "token";
            var startSyncEvent = new StartNextSyncEvent(false);
            startSyncEvent.SetParam(ContentChanges.FULL_SYNC_PARAM_NAME, changeLogToken);
            var completedEvent = new FullSyncCompletedEvent(startSyncEvent);
            var session = new Mock<ISession>().Object;
            var database = new Mock<IDatabase>();
            database.Verify(db => db.SetChangeLogToken("token"), Times.Once());
            var manager = new Mock<SyncEventManager>().Object;
            var queue = new Mock<SyncEventQueue>(manager).Object;
            var changes = new ContentChanges(session, database.Object, queue);
            Assert.IsFalse(changes.Handle(completedEvent));
        }

        [Test, Category("Fast")]
        public void HandleStartSyncEventTest() {
            Assert.Fail("TODO");
        }

        [Test, Category("Fast")]
        public void IgnoreCrawlSyncEventTest() {
            var start = new StartNextSyncEvent(true);
            var database = new Mock<IDatabase>();
            var session = new Mock<ISession>().Object;
            var manager = new Mock<SyncEventManager>().Object;
            var queue = new Mock<SyncEventQueue>(manager).Object;
            database.Setup( db => db.GetChangeLogToken()).Returns("token");
            var changes = new ContentChanges(session, database.Object, queue);
            Assert.IsFalse(changes.Handle(start));
            string result;
            Assert.IsFalse(start.TryGetParam(ContentChanges.FULL_SYNC_PARAM_NAME, out result));
            Assert.IsNull(result);
        }

        [Test, Category("Fast")]
        public void ExtendCrawlSyncEventTest() {
            string serverSideChangeLogToken = "token";
            var start = new StartNextSyncEvent(true);
            var database = new Mock<IDatabase>();
            var session = new Mock<ISession>().Object;
            var manager = new Mock<SyncEventManager>().Object;
            var queue = new Mock<SyncEventQueue>(manager).Object;
            database.Setup( db => db.GetChangeLogToken()).Returns((string) null);
            var changes = new ContentChanges(session, database.Object, queue);
            Assert.IsFalse(changes.Handle(start));
            string result;
            Assert.IsTrue(start.TryGetParam(ContentChanges.FULL_SYNC_PARAM_NAME, out result));
            Assert.AreEqual(serverSideChangeLogToken, result);
        }
    }
}

