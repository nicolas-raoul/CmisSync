﻿//-----------------------------------------------------------------------
// <copyright file="AllHandlersIT.cs" company="GRAU DATA AG">
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

namespace TestLibrary.IntegrationTests {
    using System;
    using System.Collections.Generic;
    using System.IO;

    using CmisSync.Lib;
    using CmisSync.Lib.Accumulator;
    using CmisSync.Lib.Cmis;
    using CmisSync.Lib.Config;
    using CmisSync.Lib.Consumer;
    using CmisSync.Lib.Events;
    using CmisSync.Lib.FileTransmission;
    using CmisSync.Lib.Filter;
    using CmisSync.Lib.PathMatcher;
    using CmisSync.Lib.Producer.ContentChange;
    using CmisSync.Lib.Producer.Crawler;
    using CmisSync.Lib.Producer.Watcher;
    using CmisSync.Lib.Queueing;
    using CmisSync.Lib.SelectiveIgnore;
    using CmisSync.Lib.Storage.Database;
    using CmisSync.Lib.Storage.Database.Entities;
    using CmisSync.Lib.Storage.FileSystem;

    using DBreeze;

    using DotCMIS.Binding;
    using DotCMIS.Binding.Services;
    using DotCMIS.Client;
    using DotCMIS.Data;
    using DotCMIS.Data.Extensions;
    using DotCMIS.Enums;

    using Moq;

    using Newtonsoft.Json;

    using NUnit.Framework;

    using TestLibrary.TestUtils;

    using Strategy = CmisSync.Lib.Producer.Watcher;

    [TestFixture]
    public class AllHandlersIT : IsTestWithConfiguredLog4Net, IDisposable {
        private readonly string localRoot = Path.GetTempPath();
        private readonly string remoteRoot = "remoteroot";


        private readonly bool isPropertyChangesSupported = false;
        private readonly int maxNumberOfContentChanges = 1000;

        private DBreezeEngine engine;
        private Mock<IFolder> remoteFolder;

        [TestFixtureSetUp]
        public void ClassInit() {
            // Use Newtonsoft.Json as Serializator
            DBreeze.Utils.CustomSerializator.Serializator = JsonConvert.SerializeObject;
            DBreeze.Utils.CustomSerializator.Deserializator = JsonConvert.DeserializeObject;
        }

        [SetUp]
        public void SetupEngine() {
            this.engine = new DBreezeEngine(new DBreezeConfiguration { Storage = DBreezeConfiguration.eStorage.MEMORY });
        }

        [TearDown]
        public void DestroyEngine() {
            this.engine.Dispose();
            this.engine = null;
        }

        [Test, Category("Medium")]
        public void RunFakeEvent([Values(true, false)]bool pwcIsSupported) {
            var session = new Mock<ISession>();
            session.SetupTypeSystem();
            var observer = new ObservableHandler();
            var storage = this.GetInitializedStorage();
            var queue = this.CreateQueue(session, storage, observer, pwcIsSupported);
            var myEvent = new Mock<ISyncEvent>();
            queue.AddEvent(myEvent.Object);
            queue.Run();
            Assert.That(observer.List.Count, Is.EqualTo(1));
        }

        [Test, Category("Medium")]
        public void RunStartNewSyncEvent([Values(true, false)]bool pwcIsSupported) {
            string rootFolderName = "/";
            string rootFolderId = "root";
            var storage = this.GetInitializedStorage();
            storage.SaveMappedObject(new MappedObject(rootFolderName, rootFolderId, MappedObjectType.Folder, null, "oldtoken"));
            var session = new Mock<ISession>();
            session.SetupSessionDefaultValues();
            session.SetupChangeLogToken("default");
            session.SetupTypeSystem();
            var observer = new ObservableHandler();
            var queue = this.CreateQueue(session, storage, observer, pwcIsSupported);
            queue.RunStartSyncEvent();
            Assert.That(observer.List.Count, Is.EqualTo(1));
            Assert.That(observer.List[0], Is.TypeOf(typeof(FullSyncCompletedEvent)));
        }

        [Test, Category("Medium")]
        public void RunFSEventFileDeleted([Values(true, false)]bool pwcIsSupported) {
            var storage = this.GetInitializedStorage();
            var path = new Mock<IFileInfo>();
            var name = "a";
            path.Setup(p => p.FullName).Returns(Path.Combine(this.localRoot, name));
            string id = "id";

            var mappedObject = new MappedObject(name, id, MappedObjectType.File, null, "changeToken");
            storage.SaveMappedObject(mappedObject);

            var session = new Mock<ISession>();
            session.SetupSessionDefaultValues();
            session.SetupChangeLogToken("default");
            session.SetupTypeSystem();
            IDocument remote = MockOfIDocumentUtil.CreateRemoteDocumentMock(null, id, name, (string)null, changeToken: "changeToken").Object;
            session.Setup(s => s.GetObject(id, It.IsAny<IOperationContext>())).Returns(remote);
            var myEvent = new FSEvent(WatcherChangeTypes.Deleted, path.Object.FullName, false);
            var queue = this.CreateQueue(session, storage, pwcIsSupported);
            queue.AddEvent(myEvent);
            queue.Run();

            session.Verify(f => f.Delete(It.Is<IObjectId>(i => i.Id == id), true), Times.Once());
            Assert.That(storage.GetObjectByRemoteId(id), Is.Null);
        }

        [Test, Category("Medium")]
        public void RunFSEventFolderDeleted([Values(true, false)]bool pwcIsSupported) {
            var storage = this.GetInitializedStorage();
            var path = new Mock<IFileInfo>();
            var name = "a";
            path.Setup(p => p.FullName).Returns(Path.Combine(this.localRoot, name));
            string id = "id";

            var mappedObject = new MappedObject(name, id, MappedObjectType.Folder, null, "changeToken");
            storage.SaveMappedObject(mappedObject);

            var session = new Mock<ISession>();
            session.SetupSessionDefaultValues();
            session.SetupChangeLogToken("default");
            session.SetupTypeSystem();
            IFolder remote = MockOfIFolderUtil.CreateRemoteFolderMock(id, name, (string)null, changetoken: "changeToken").Object;
            session.Setup(s => s.GetObject(id, It.IsAny<IOperationContext>())).Returns(remote);
            var myEvent = new FSEvent(WatcherChangeTypes.Deleted, path.Object.FullName, true);
            var queue = this.CreateQueue(session, storage, pwcIsSupported);
            queue.AddEvent(myEvent);
            queue.Run();

            Mock.Get(remote).Verify(d => d.DeleteTree(false, UnfileObject.DeleteSinglefiled, true), Times.Once());
            Assert.That(storage.GetObjectByRemoteId(id), Is.Null);
        }

        [Test, Category("Medium")]
        public void ContentChangeIndicatesFolderDeletionOfExistingFolder([Values(true, false)]bool pwcIsSupported) {
            var storage = this.GetInitializedStorage();
            var name = "a";
            string path = Path.Combine(this.localRoot, name);
            string id = "1";
            Mock<IFileSystemInfoFactory> fsFactory = new Mock<IFileSystemInfoFactory>();
            var dirInfo = new Mock<IDirectoryInfo>();
            dirInfo.Setup(d => d.Exists).Returns(true);
            dirInfo.Setup(d => d.FullName).Returns(path);
            fsFactory.AddIDirectoryInfo(dirInfo.Object);
            var mappedObject = new MappedObject(name, id, MappedObjectType.Folder, null, null);
            storage.SaveMappedObject(mappedObject);
            storage.ChangeLogToken = "oldtoken";

            Mock<ISession> session = MockSessionUtil.GetSessionMockReturningFolderChange(DotCMIS.Enums.ChangeType.Deleted, id);
            session.SetupTypeSystem();
            var queue = this.CreateQueue(session, storage, fsFactory.Object, pwcIsSupported);
            queue.RunStartSyncEvent();
            dirInfo.Verify(d => d.Delete(false), Times.Once());
            Assert.That(storage.GetObjectByRemoteId(id), Is.Null);
        }

        [Test, Category("Medium")]
        public void ContentChangeIndicatesFolderRenameOfExistingFolder([Values(true, false)]bool pwcIsSupported) {
            var storage = this.GetInitializedStorage();
            string name = "a";
            string newName = "b";
            string parentId = "parentId";
            string path = Path.Combine(this.localRoot, name);
            string newPath = Path.Combine(this.localRoot, newName);
            string id = "1";
            string lastChangeToken = "changeToken";
            Guid guid = Guid.NewGuid();
            Mock<IFileSystemInfoFactory> fsFactory = new Mock<IFileSystemInfoFactory>();
            var dirInfo = new Mock<IDirectoryInfo>();
            dirInfo.Setup(d => d.Exists).Returns(true);
            dirInfo.Setup(d => d.FullName).Returns(path);
            dirInfo.Setup(d => d.Parent).Returns(Mock.Of<IDirectoryInfo>(r => r.FullName == this.localRoot));
            fsFactory.AddIDirectoryInfo(dirInfo.Object);
            var mappedRootObject = new MappedObject("/", parentId, MappedObjectType.Folder, null, storage.ChangeLogToken);
            storage.SaveMappedObject(mappedRootObject);
            var mappedObject = new MappedObject(name, id, MappedObjectType.Folder, parentId, null) { Guid = guid };
            storage.SaveMappedObject(mappedObject);
            storage.ChangeLogToken = "oldChangeToken";
            Console.WriteLine(storage.ToFindString());

            Mock<ISession> session = MockSessionUtil.GetSessionMockReturningFolderChange(DotCMIS.Enums.ChangeType.Updated, id, newName, this.remoteRoot + "/" + newName, parentId, lastChangeToken);
            session.SetupTypeSystem();

            var queue = this.CreateQueue(session, storage, fsFactory.Object, pwcIsSupported);
            dirInfo.Setup(d => d.MoveTo(It.IsAny<string>()))
                .Callback(() => {
                    queue.AddEvent(Mock.Of<IFSMovedEvent>(fs => fs.IsDirectory == true && fs.OldPath == path && fs.LocalPath == newPath && fs.Type == WatcherChangeTypes.Renamed));
                    var newDirInfo = new Mock<IDirectoryInfo>();
                    newDirInfo.Setup(d => d.Exists).Returns(true);
                    newDirInfo.Setup(d => d.FullName).Returns(newPath);
                    newDirInfo.Setup(d => d.Uuid).Returns(guid);
                    newDirInfo.Setup(d => d.Parent).Returns(Mock.Of<IDirectoryInfo>(r => r.FullName == this.localRoot));
                    fsFactory.AddIDirectoryInfo(newDirInfo.Object);
                });

            queue.RunStartSyncEvent();
            dirInfo.Verify(d => d.MoveTo(It.Is<string>(p => p.Equals(newPath))), Times.Once());
            Assert.That(storage.GetObjectByRemoteId(id), Is.Not.Null);
            Assert.That(storage.GetObjectByRemoteId(id).Name, Is.EqualTo(newName));
            Assert.That(storage.GetObjectByLocalPath(Mock.Of<IDirectoryInfo>(d => d.FullName == path)), Is.Null);
            Assert.That(storage.GetObjectByLocalPath(Mock.Of<IDirectoryInfo>(d => d.FullName == newPath)), Is.Not.Null);
        }

        [Test, Category("Medium")]
        public void ContentChangeIndicatesFolderCreation([Values(true, false)]bool pwcIsSupported) {
            string rootFolderName = "/";
            string rootFolderId = "root";
            string folderName = "folder";
            string parentId = "root";
            string lastChangeToken = "changeToken";
            Mock<IFileSystemInfoFactory> fsFactory = new Mock<IFileSystemInfoFactory>();
            var dirInfo = fsFactory.AddDirectory(Path.Combine(this.localRoot, folderName));

            string id = "1";
            Mock<ISession> session = MockSessionUtil.GetSessionMockReturningFolderChange(DotCMIS.Enums.ChangeType.Created, id, folderName, this.remoteRoot + "/" + folderName, parentId, lastChangeToken);
            session.SetupTypeSystem();
            var storage = this.GetInitializedStorage();
            storage.ChangeLogToken = "oldtoken";
            storage.SaveMappedObject(new MappedObject(rootFolderName, rootFolderId, MappedObjectType.Folder, null, "oldtoken"));
            var queue = this.CreateQueue(session, storage, fsFactory.Object, pwcIsSupported);
            var fsFolderCreatedEvent = new Mock<IFSEvent>();
            fsFolderCreatedEvent.Setup(f => f.IsDirectory).Returns(true);
            fsFolderCreatedEvent.Setup(f => f.LocalPath).Returns(Path.Combine(this.localRoot, folderName));
            fsFolderCreatedEvent.Setup(f => f.Type).Returns(WatcherChangeTypes.Created);
            dirInfo.Setup(d => d.Create()).Callback(delegate { queue.AddEvent(fsFolderCreatedEvent.Object); });

            queue.RunStartSyncEvent();

            dirInfo.Verify(d => d.Create(), Times.Once());
            var mappedObject = storage.GetObjectByRemoteId(id);
            Assert.That(mappedObject, Is.Not.Null);
            Assert.That(mappedObject.RemoteObjectId, Is.EqualTo(id), "RemoteObjectId incorrect");
            Assert.That(mappedObject.Name, Is.EqualTo(folderName), "Name incorrect");
            Assert.That(mappedObject.ParentId, Is.EqualTo(parentId), "ParentId incorrect");
            Assert.That(mappedObject.LastChangeToken, Is.EqualTo(lastChangeToken), "LastChangeToken incorrect");
            Assert.That(mappedObject.Type, Is.EqualTo(MappedObjectType.Folder), "Type incorrect");
        }

        [Test, Category("Medium")]
        public void ContentChangeIndicatesFolderMove([Values(true, false)]bool pwcIsSupported) {
            // Moves /a/b to /b
            string rootFolderId = "rootId";
            string folderAName = "a";
            string folderAId = "aid";
            string folderBName = "b";
            string folderBId = "bid";

            string lastChangeToken = "changeToken";

            Mock<IFileSystemInfoFactory> fsFactory = new Mock<IFileSystemInfoFactory>();
            var folderBInfo = fsFactory.AddDirectory(Path.Combine(this.localRoot, folderAName, folderBName));

            Mock<ISession> session = MockSessionUtil.GetSessionMockReturningFolderChange(DotCMIS.Enums.ChangeType.Updated, folderBId, folderBName, this.remoteRoot + "/" + folderBName, rootFolderId, lastChangeToken);
            session.SetupTypeSystem();

            var storage = this.GetInitializedStorage();
            storage.ChangeLogToken = "oldtoken";
            var mappedRootObject = new MappedObject("/", rootFolderId, MappedObjectType.Folder, null, storage.ChangeLogToken);
            storage.SaveMappedObject(mappedRootObject);
            var mappedAObject = new MappedObject(folderAName, folderAId, MappedObjectType.Folder, rootFolderId, storage.ChangeLogToken);
            storage.SaveMappedObject(mappedAObject);
            var mappedBObject = new MappedObject(folderBName, folderBId, MappedObjectType.Folder, folderAId, storage.ChangeLogToken);
            storage.SaveMappedObject(mappedBObject);

            var queue = this.CreateQueue(session, storage, fsFactory.Object, pwcIsSupported);

            queue.RunStartSyncEvent();

            folderBInfo.Verify(d => d.MoveTo(Path.Combine(this.localRoot, folderBName)), Times.Once());
        }

        #region boilerplatecode
        public void Dispose() {
            if (this.engine != null) {
                this.engine.Dispose();
                this.engine = null;
            }
        }
        #endregion

        private SingleStepEventQueue CreateQueue(Mock<ISession> session, IMetaDataStorage storage, bool pwcSupported) {
            return this.CreateQueue(session, storage, new ObservableHandler(), pwcSupported);
        }

        private SingleStepEventQueue CreateQueue(Mock<ISession> session, IMetaDataStorage storage, IFileSystemInfoFactory fsFactory, bool pwcSupported) {
            return this.CreateQueue(session, storage, new ObservableHandler(), pwcSupported, fsFactory);
        }

        private IMetaDataStorage GetInitializedStorage() {
            IPathMatcher matcher = new PathMatcher(this.localRoot, this.remoteRoot);
            return new MetaDataStorage(this.engine, matcher, true);
        }

        private SingleStepEventQueue CreateQueue(Mock<ISession> session, IMetaDataStorage storage, ObservableHandler observer, bool pwcIsSupported, IFileSystemInfoFactory fsFactory = null) {
            var manager = new SyncEventManager();
            SingleStepEventQueue queue = new SingleStepEventQueue(manager);

            manager.AddEventHandler(observer);

            var connectionScheduler = new ConnectionScheduler(new RepoInfo(), queue, Mock.Of<ISessionFactory>(), Mock.Of<IAuthenticationProvider>());
            manager.AddEventHandler(connectionScheduler);

            var changes = new ContentChanges(session.Object, storage, queue, this.maxNumberOfContentChanges, this.isPropertyChangesSupported);
            manager.AddEventHandler(changes);

            var transformer = new ContentChangeEventTransformer(queue, storage, fsFactory);
            manager.AddEventHandler(transformer);

            var ccaccumulator = new ContentChangeEventAccumulator(session.Object, queue);
            manager.AddEventHandler(ccaccumulator);

            var remoteFetcher = new RemoteObjectFetcher(session.Object, storage);
            manager.AddEventHandler(remoteFetcher);

            var localFetcher = new LocalObjectFetcher(storage.Matcher, fsFactory);
            manager.AddEventHandler(localFetcher);

            var watcher = new Strategy.WatcherConsumer(queue);
            manager.AddEventHandler(watcher);

            var localDetection = new LocalSituationDetection();
            var remoteDetection = new RemoteSituationDetection();
            var transmissionManager = new TransmissionManager();
            var activityAggregator = new ActivityListenerAggregator(Mock.Of<IActivityListener>(), transmissionManager);
            var transmissionFactory = transmissionManager.CreateFactory();
            var localFolder = new Mock<IDirectoryInfo>();
            localFolder.Setup(f => f.FullName).Returns(this.localRoot);
            var ignoreFolderFilter = new IgnoredFoldersFilter();
            var ignoreFolderNameFilter = new IgnoredFolderNameFilter(localFolder.Object);
            var ignoreFileNamesFilter = new IgnoredFileNamesFilter();
            var invalidFolderNameFilter = new InvalidFolderNameFilter();
            var filterAggregator = new FilterAggregator(ignoreFileNamesFilter, ignoreFolderNameFilter, invalidFolderNameFilter, ignoreFolderFilter);

            var syncMechanism = new SyncMechanism(localDetection, remoteDetection, queue, session.Object, storage, Mock.Of<IFileTransmissionStorage>(), activityAggregator, filterAggregator, transmissionFactory, pwcIsSupported);
            manager.AddEventHandler(syncMechanism);

            this.remoteFolder = MockSessionUtil.CreateCmisFolder();
            this.remoteFolder.Setup(r => r.Path).Returns(this.remoteRoot);
            this.remoteFolder.SetupId("root");
            var generator = new CrawlEventGenerator(storage, fsFactory);
            var ignoreStorage = new IgnoredEntitiesStorage(new IgnoredEntitiesCollection(), storage);
            var treeBuilder = new DescendantsTreeBuilder(storage, remoteFolder.Object, localFolder.Object, filterAggregator, ignoreStorage);
            var notifier = new CrawlEventNotifier(queue);
            var crawler = new DescendantsCrawler(queue, treeBuilder, generator, notifier, Mock.Of<IActivityListener>());
            manager.AddEventHandler(crawler);

            var permissionDenied = new GenericHandleDublicatedEventsFilter<PermissionDeniedEvent, ConfigChangedEvent>();
            manager.AddEventHandler(permissionDenied);

            var alreadyAddedFilter = new IgnoreAlreadyHandledFsEventsFilter(storage, fsFactory);
            manager.AddEventHandler(alreadyAddedFilter);

            var ignoreContentChangesFilter = new IgnoreAlreadyHandledContentChangeEventsFilter(storage, session.Object);
            manager.AddEventHandler(ignoreContentChangesFilter);

            var delayRetryAndNextSyncEventHandler = new DelayRetryAndNextSyncEventHandler(queue);
            manager.AddEventHandler(delayRetryAndNextSyncEventHandler);

            /* This is not implemented yet
            var failedOperationsFilder = new FailedOperationsFilter(queue);
            manager.AddEventHandler(failedOperationsFilder);
            */

            var reportingFilter = new ReportingFilter(queue, ignoreFolderFilter, ignoreFileNamesFilter, ignoreFolderNameFilter, invalidFolderNameFilter, new SymlinkFilter());
            manager.AddEventHandler(reportingFilter);

            var debugHandler = new DebugLoggingHandler();
            manager.AddEventHandler(debugHandler);

            var movedOrRenamed = new RemoteObjectMovedOrRenamedAccumulator(queue, storage, fsFactory);
            manager.AddEventHandler(movedOrRenamed);

            return queue;
        }
    }
}