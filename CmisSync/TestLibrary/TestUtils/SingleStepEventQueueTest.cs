//-----------------------------------------------------------------------
// <copyright file="SingleStepEventQueueTest.cs" company="GRAU DATA AG">
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see http://www.gnu.org/licenses/.
//
// </copyright>
//-----------------------------------------------------------------------

namespace TestLibrary.QueueingTests {
    using System;

    using CmisSync.Lib.Events;
    using CmisSync.Lib.Queueing;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class SingleStepEventQueueTest {
        [Test, Category("Fast")]
        public void InitialState() {
            var manager = new Mock<ISyncEventManager>();
            var queue = new SingleStepEventQueue(manager.Object);
            Assert.That(queue.IsStopped, Is.True, "Queue starts stopped");
        }

        [Test, Category("Fast")]
        public void StartAndStopWorks() {
            var manager = new Mock<ISyncEventManager>();
            var queue = new SingleStepEventQueue(manager.Object);
            var syncEvent = new Mock<ISyncEvent>();
            queue.AddEvent(syncEvent.Object);
            Assert.That(queue.IsStopped, Is.False, "Queue should not start immediatly");
            queue.Step();
            Assert.That(queue.IsStopped, Is.True, "Queue should be Stopped if empty again");
        }

        [Test, Category("Fast")]
        public void EventsGetForwarded() {
            var manager = new Mock<ISyncEventManager>();
            var queue = new SingleStepEventQueue(manager.Object);
            var syncEvent = new Mock<ISyncEvent>();
            queue.AddEvent(syncEvent.Object);
            queue.Step();
            manager.Verify(m => m.Handle(syncEvent.Object), Times.Once());
        }

        [Test, Category("Fast")]
        public void QueueIsFifo() {
            var manager = new Mock<ISyncEventManager>();
            var queue = new SingleStepEventQueue(manager.Object);
            var syncEvent1 = new Mock<ISyncEvent>();
            var syncEvent2 = new Mock<ISyncEvent>();
            queue.AddEvent(syncEvent1.Object);
            queue.AddEvent(syncEvent2.Object);
            queue.Step();
            manager.Verify(m => m.Handle(syncEvent1.Object), Times.Once());
            manager.Verify(m => m.Handle(syncEvent2.Object), Times.Never());
            queue.Step();
            manager.Verify(m => m.Handle(syncEvent1.Object), Times.Once());
            manager.Verify(m => m.Handle(syncEvent2.Object), Times.Once());
        }

        [Test, Category("Fast")]
        public void QueueRethrowsExceptionsByDefault() {
            var manager = new Mock<ISyncEventManager>();
            manager.Setup(m => m.Handle(It.IsAny<ISyncEvent>())).Throws<Exception>();
            var queue = new SingleStepEventQueue(manager.Object);
            Assert.Throws<Exception>(() => queue.RunStartSyncEvent());
        }

        [Test, Category("Fast")]
        public void QueueSwallowsExceptionsIfConfigured() {
            var manager = new Mock<ISyncEventManager>();
            manager.Setup(m => m.Handle(It.IsAny<ISyncEvent>())).Throws(new Exception());
            var queue = new SingleStepEventQueue(manager.Object);
            queue.SwallowExceptions = true;
            queue.RunStartSyncEvent();
        }

        [Test, Category("Fast")]
        public void DropAllFSEventsIfConfigured([Values(true, false)]bool dropAll) {
            var manager = new SyncEventManager();
            var handler = new Mock<SyncEventHandler>() { CallBase = true };
            handler.Setup(h => h.Priority).Returns(EventHandlerPriorities.CRITICAL);
            var underTest = new SingleStepEventQueue(manager);
            manager.AddEventHandler(handler.Object);

            underTest.DropAllLocalFileSystemEvents = dropAll;
            underTest.AddEvent(Mock.Of<IFSEvent>());
            underTest.AddEvent(Mock.Of<ISyncEvent>());
            underTest.Run();

            handler.Verify(h => h.Handle(It.IsAny<IFSEvent>()), dropAll ? Times.Never() : Times.Once());
            handler.Verify(h => h.Handle(It.IsAny<ISyncEvent>()), dropAll ? Times.Once() : Times.Exactly(2));
        }
    }
}