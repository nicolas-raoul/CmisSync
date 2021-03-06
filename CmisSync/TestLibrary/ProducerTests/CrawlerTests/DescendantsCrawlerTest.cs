//-----------------------------------------------------------------------
// <copyright file="DescendantsCrawlerTest.cs" company="GRAU DATA AG">
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

namespace TestLibrary.ProducerTests.CrawlerTests {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    using CmisSync.Lib;
    using CmisSync.Lib.Consumer;
    using CmisSync.Lib.Events;
    using CmisSync.Lib.Exceptions;
    using CmisSync.Lib.Filter;
    using CmisSync.Lib.PathMatcher;
    using CmisSync.Lib.Producer.Crawler;
    using CmisSync.Lib.Producer.Watcher;
    using CmisSync.Lib.Queueing;
    using CmisSync.Lib.SelectiveIgnore;
    using CmisSync.Lib.Storage.Database;
    using CmisSync.Lib.Storage.Database.Entities;
    using CmisSync.Lib.Storage.FileSystem;

    using DBreeze;

    using DotCMIS.Client;
    using DotCMIS.Client.Impl;
    using DotCMIS.Exceptions;

    using Moq;

    using Newtonsoft.Json;

    using NUnit.Framework;

    using TestUtils;

    [TestFixture, Category("Fast")]
    public class DescendantsCrawlerTest : IDisposable {
        protected readonly string remoteRootId = "rootId";
        protected readonly string remoteRootPath = "/";
        protected readonly Guid rootGuid = Guid.NewGuid();
        protected Mock<ISyncEventQueue> queue;
        protected IMetaDataStorage storage;
        protected Mock<IFolder> remoteFolder;
        protected Mock<IDirectoryInfo> localFolder;
        protected Mock<IFileSystemInfoFactory> fsFactory;
        protected string localRootPath;
        protected MappedObject mappedRootObject;
        protected IPathMatcher matcher;
        protected DBreezeEngine storageEngine;
        protected DateTime lastLocalWriteTime = DateTime.Now;
        protected IFilterAggregator filter;
        protected Mock<IActivityListener> listener;
        protected Queue<Guid> localGuids;

        [TestFixtureSetUp]
        public void InitCustomSerializator() {
            // Use Newtonsoft.Json as Serializator
            DBreeze.Utils.CustomSerializator.Serializator = JsonConvert.SerializeObject;
            DBreeze.Utils.CustomSerializator.Deserializator = JsonConvert.DeserializeObject;
        }

        private void SetUpMocks() {
            this.storageEngine = new DBreezeEngine(new DBreezeConfiguration { Storage = DBreezeConfiguration.eStorage.MEMORY });
            this.localRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            this.matcher = new PathMatcher(this.localRootPath, this.remoteRootPath);
            this.queue = new Mock<ISyncEventQueue>();
            this.remoteFolder = MockOfIFolderUtil.CreateRemoteFolderMock(this.remoteRootId, this.remoteRootPath, this.remoteRootPath).SetupReadOnly(false);
            this.remoteFolder.SetupDescendants();
            this.localFolder = new Mock<IDirectoryInfo>(MockBehavior.Strict).SetupFullName(this.localRootPath).SetupExists(true).SetupGuid(this.rootGuid).SetupLastWriteTimeUtc(this.lastLocalWriteTime).SetupReadOnly(false);
            this.localFolder.Setup(f => f.IsExtendedAttributeAvailable()).Returns(true);
            this.localFolder.SetupDirectories().SetupFiles();
            this.fsFactory = new Mock<IFileSystemInfoFactory>();
            this.fsFactory.AddIDirectoryInfo(this.localFolder.Object);
            this.mappedRootObject = new MappedObject(
                this.remoteRootPath,
                this.remoteRootId,
                MappedObjectType.Folder,
                null,
                "changeToken") {
                Guid = this.rootGuid,
                LastLocalWriteTimeUtc = this.lastLocalWriteTime
            };
            this.storage = new MetaDataStorage(this.storageEngine, this.matcher, true, true);
            this.storage.SaveMappedObject(this.mappedRootObject);
            this.filter = MockOfIFilterAggregatorUtil.CreateFilterAggregator().Object;
            this.listener = new Mock<IActivityListener>();
            this.localGuids = new Queue<Guid>();
        }

        [TearDown]
        public void TearDownStorage() {
            this.storageEngine.Dispose();
            this.storageEngine = null;
        }

        [Test]
        public void ConstructorThrowsExceptionIfLocalFolderIsNull() {
            this.SetUpMocks();
            Assert.Throws<ArgumentNullException>(() => new DescendantsCrawler(Mock.Of<ISyncEventQueue>(), Mock.Of<IFolder>(), null, Mock.Of<IMetaDataStorage>(), this.filter, this.listener.Object, Mock.Of<IIgnoredEntitiesStorage>()));
        }

        [Test]
        public void ConstructorThrowsExceptionIfRemoteFolderIsNull() {
            this.SetUpMocks();
            Assert.Throws<ArgumentNullException>(() => new DescendantsCrawler(Mock.Of<ISyncEventQueue>(), null, Mock.Of<IDirectoryInfo>(), Mock.Of<IMetaDataStorage>(), this.filter, this.listener.Object, Mock.Of<IIgnoredEntitiesStorage>()));
        }

        [Test]
        public void ConstructorThrowsExceptionIfQueueIsNull() {
            this.SetUpMocks();
            Assert.Throws<ArgumentNullException>(() => new DescendantsCrawler(null, Mock.Of<IFolder>(), Mock.Of<IDirectoryInfo>(), Mock.Of<IMetaDataStorage>(), this.filter, this.listener.Object, Mock.Of<IIgnoredEntitiesStorage>()));
        }

        [Test]
        public void ConstructorThrowsExceptionIfStorageIsNull() {
            this.SetUpMocks();
            Assert.Throws<ArgumentNullException>(() => new DescendantsCrawler(Mock.Of<ISyncEventQueue>(), Mock.Of<IFolder>(), Mock.Of<IDirectoryInfo>(), null, this.filter, this.listener.Object, Mock.Of<IIgnoredEntitiesStorage>()));
        }

        [Test]
        public void ConstructorThrowsExceptionIfListenerIsNull() {
            this.SetUpMocks();
            Assert.Throws<ArgumentNullException>(() => new DescendantsCrawler(Mock.Of<ISyncEventQueue>(), Mock.Of<IFolder>(), Mock.Of<IDirectoryInfo>(), Mock.Of<IMetaDataStorage>(), this.filter, null, Mock.Of<IIgnoredEntitiesStorage>()));
        }

        [Test]
        public void ConstructorWorksWithoutFsInfoFactory() {
            this.SetUpMocks();
            var localFolder = Mock.Of<IDirectoryInfo>(p => p.FullName == this.localRootPath);
            var remoteFolder = Mock.Of<IFolder>(p => p.Path == this.remoteRootPath);
            new DescendantsCrawler(Mock.Of<ISyncEventQueue>(), remoteFolder, localFolder, Mock.Of<IMetaDataStorage>(), this.filter, this.listener.Object, Mock.Of<IIgnoredEntitiesStorage>());
        }

        [Test]
        public void ConstructorTakesFsInfoFactory() {
            this.SetUpMocks();
            this.CreateCrawler();
        }

        [Test]
        public void PriorityIsNormal() {
            this.SetUpMocks();
            var crawler = this.CreateCrawler();
            Assert.That(crawler.Priority == EventHandlerPriorities.NORMAL);
        }

        [Test]
        public void IgnoresNonFittingEvents() {
            this.SetUpMocks();
            var crawler = this.CreateCrawler();
            Assert.That(crawler.Handle(Mock.Of<ISyncEvent>()), Is.False);
            this.queue.Verify(q => q.AddEvent(It.IsAny<ISyncEvent>()), Times.Never());
            this.listener.Verify(l => l.ActivityStarted(), Times.Never);
        }

        [Test]
        public void HandlesStartNextSyncEventAndReportsOnQueueIfDone() {
            this.SetUpMocks();
            var crawler = this.CreateCrawler();
            var startEvent = new StartNextSyncEvent();
            Assert.That(crawler.Handle(startEvent), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FullSyncCompletedEvent>(e => e.StartEvent.Equals(startEvent))), Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneRemoteFolderAdded() {
            this.SetUpMocks();
            IFolder newRemoteFolder = MockOfIFolderUtil.CreateRemoteFolderMock("id", "name", "/name", this.remoteRootId).Object;
            this.remoteFolder.SetupChildren(newRemoteFolder);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FolderEvent>(e => e.RemoteFolder.Equals(newRemoteFolder))), Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneRemoteFileAdded() {
            this.SetUpMocks();
            IDocument newRemoteDocument = MockOfIDocumentUtil.CreateRemoteDocumentMock(null, "id", "name", this.remoteRootId).Object;
            this.remoteFolder.SetupChildren(newRemoteDocument);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FileEvent>(e => e.RemoteFile.Equals(newRemoteDocument))), Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void FailuresInSubmodulesAreHandledByAddingTheNotYetHandledEventBackToQueue([Values(true, false)]bool forceCrawl) {
            this.SetUpMocks();
            var crawler = this.CreateCrawler();
            this.localFolder.Setup(f => f.GetDirectories()).Throws<Exception>();
            var startEvent = new StartNextSyncEvent(fullSyncRequested: forceCrawl);
            Assert.That(crawler.Handle(startEvent), Is.False);
            this.queue.Verify(q => q.AddEvent(It.Is<StartNextSyncEvent>(e => e.FullSyncRequested == forceCrawl && e.LastTokenOnServer == startEvent.LastTokenOnServer)), Times.Once());
            this.queue.VerifyThatNoOtherEventIsAddedThan<StartNextSyncEvent>();
        }

        [Test]
        public void OneRemoteFileRemoved() {
            this.SetUpMocks();
            Guid fileGuid = Guid.NewGuid();
            DateTime modificationDate = DateTime.UtcNow;
            MappedObject obj = new MappedObject("name", "id", MappedObjectType.File, this.remoteRootId, "changeToken", 0) {
                Guid = fileGuid,
                LastLocalWriteTimeUtc = modificationDate
            };
            var oldLocalFile = this.fsFactory.AddFile(Path.Combine(this.localRootPath, "name"));
            oldLocalFile.SetupGuid(fileGuid);
            oldLocalFile.SetupLastWriteTimeUtc(modificationDate);
            this.localFolder.SetupFiles(oldLocalFile.Object);
            this.storage.SaveMappedObject(obj);

            var underTest = this.CreateCrawler();

            Assert.That(underTest.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FileEvent>(e => e.Remote == MetaDataChangeType.DELETED)), Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void SimpleRemoteFolderHierarchyAdded() {
            this.SetUpMocks();
            var newRemoteSubFolder = MockOfIFolderUtil.CreateRemoteFolderMock("remoteSubFolder", "sub", "/name/sub", "remoteFolder");
            var newRemoteFolder = MockOfIFolderUtil.CreateRemoteFolderMock("remoteFolder", "name", "/name", this.remoteRootId);
            newRemoteFolder.SetupChildren(newRemoteSubFolder.Object);
            this.remoteFolder.SetupChildren(newRemoteFolder.Object);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FolderEvent>(e => e.RemoteFolder.Equals(newRemoteFolder.Object))), Times.Once());
            this.queue.Verify(q => q.AddEvent(It.Is<FolderEvent>(e => e.RemoteFolder.Equals(newRemoteSubFolder.Object))), Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Exactly(2));
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneLocalFolderAdded() {
            this.SetUpMocks();
            var newFolderMock = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, "newFolder"));
            this.localFolder.SetupDirectories(newFolderMock.Object);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FolderEvent>(e => e.LocalFolder.Equals(newFolderMock.Object))), Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneLocalFileAdded() {
            this.SetUpMocks();
            var newFileMock = this.fsFactory.AddFile(Path.Combine(this.localRootPath, "newFile"));
            this.localFolder.SetupFiles(newFileMock.Object);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FileEvent>(e => e.LocalFile.Equals(newFileMock.Object))), Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneLocalFileRemoved() {
            this.SetUpMocks();
            var oldLocalFile = this.fsFactory.AddFile(Path.Combine(this.localRootPath, "oldFile"));
            oldLocalFile.Setup(f => f.Exists).Returns(false);
            var remoteFile = MockOfIDocumentUtil.CreateRemoteDocumentMock(null, "oldFileId", "oldFile", this.remoteRootId, changeToken: "oldChange");
            var storedFile = new MappedObject("oldFile", "oldFileId", MappedObjectType.File, this.remoteRootId, "oldChange") {
                Guid = Guid.NewGuid()
            };
            this.storage.SaveMappedObject(storedFile);

            this.remoteFolder.SetupDescendants(remoteFile.Object);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FileEvent>(e => e.LocalFile != null && e.LocalFile.Equals(oldLocalFile.Object) && e.Local.Equals(MetaDataChangeType.DELETED))), Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneLocalFolderRemoved() {
            this.SetUpMocks();
            var oldLocalFolder = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, "oldFolder"));
            oldLocalFolder.Setup(f => f.Exists).Returns(false);
            var remoteSubFolder = MockOfIFolderUtil.CreateRemoteFolderMock("oldFolderId", "oldFolder", this.remoteRootPath + "oldFolder", this.remoteRootId, "oldChange");
            var storedFolder = new MappedObject("oldFolder", "oldFolderId", MappedObjectType.Folder, this.remoteRootId, "oldChange") {
                Guid = Guid.NewGuid()
            };
            this.storage.SaveMappedObject(storedFolder);
            this.remoteFolder.SetupDescendants(remoteSubFolder.Object);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FolderEvent>(e => e.LocalFolder != null && e.LocalFolder.Equals(oldLocalFolder.Object))), Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneLocalFolderRenamed() {
            this.SetUpMocks();
            var uuid = Guid.NewGuid();
            var oldLocalFolder = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, "oldFolder"));
            var newLocalFolder = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, "newFolder"));
            oldLocalFolder.Setup(f => f.Exists).Returns(false);
            newLocalFolder.SetupGuid(uuid);
            var remoteSubFolder = MockOfIFolderUtil.CreateRemoteFolderMock("oldFolderId", "oldFolder", this.remoteRootPath + "oldFolder", this.remoteRootId, "oldChange");
            var storedFolder = new MappedObject("oldFolder", "oldFolderId", MappedObjectType.Folder, this.remoteRootId, "oldChange") {
                Guid = uuid
            };
            this.storage.SaveMappedObject(storedFolder);
            this.remoteFolder.SetupDescendants(remoteSubFolder.Object);

            this.localFolder.SetupDirectories(newLocalFolder.Object);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(
                q =>
                q.AddEvent(
                It.Is<FolderEvent>(
                e =>
                e.LocalFolder.Equals(newLocalFolder.Object) &&
                e.Local.Equals(MetaDataChangeType.CHANGED))),
                Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneLocalFileCopied() {
            this.SetUpMocks();
            var uuid = Guid.NewGuid();
            var oldFile = this.fsFactory.AddFile(Path.Combine(this.localRootPath, "old"), uuid);
            oldFile.Setup(f => f.LastWriteTimeUtc).Returns(this.lastLocalWriteTime);
            var newFile = this.fsFactory.AddFile(Path.Combine(this.localRootPath, "new"), uuid);

            var remoteSubFolder = MockOfIDocumentUtil.CreateRemoteDocumentMock("streamId", "oldId", oldFile.Object.Name, this.remoteFolder.Object, changeToken: "oldChange");
            var storedFolder = new MappedObject(oldFile.Object.Name, "oldId", MappedObjectType.File, this.remoteRootId, "oldChange") {
                Guid = uuid,
                LastLocalWriteTimeUtc = this.lastLocalWriteTime
            };
            this.storage.SaveMappedObject(storedFolder);
            this.remoteFolder.SetupChildren(remoteSubFolder.Object);
            this.localFolder.SetupFiles(newFile.Object, oldFile.Object);

            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(
                q =>
                q.AddEvent(
                It.Is<FileEvent>(
                e =>
                e.LocalFile.Equals(newFile.Object) &&
                e.Local.Equals(MetaDataChangeType.CREATED))),
                Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
        }

        [Test]
        public void NoChangeOnExistingFileAndFolderCreatesNoEventsInQueue() {
            this.SetUpMocks();
            DateTime changeTime = DateTime.UtcNow;
            string changeToken = "token";
            string fileName = "name";
            string fileId = "fileId";
            string folderName = "folder";
            string folderId = "folderId";
            Guid folderGuid = Guid.NewGuid();
            Guid fileGuid = Guid.NewGuid();
            var remoteFile = MockOfIDocumentUtil.CreateRemoteDocumentMock(null, fileId, fileName, this.remoteRootId, changeToken: changeToken);
            var existingRemoteFolder = MockOfIFolderUtil.CreateRemoteFolderMock(folderId, folderName, this.remoteRootPath + folderName, this.remoteRootId, changeToken);
            var file = this.fsFactory.AddFile(Path.Combine(this.localRootPath, fileName), fileGuid);
            file.Setup(f => f.LastWriteTimeUtc).Returns(changeTime);
            var storedFile = new MappedObject(fileName, fileId, MappedObjectType.File, this.remoteRootId, changeToken) { Guid = fileGuid, LastLocalWriteTimeUtc = changeTime };
            this.storage.SaveMappedObject(storedFile);
            var folder = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, folderName), folderGuid);
            folder.Setup(f => f.LastWriteTimeUtc).Returns(changeTime);
            var storedFolder = new MappedObject(folderName, folderId, MappedObjectType.Folder, this.remoteRootId, changeToken) { Guid = folderGuid, LastLocalWriteTimeUtc = changeTime };
            this.storage.SaveMappedObject(storedFolder);
            this.remoteFolder.SetupChildren(remoteFile.Object, existingRemoteFolder.Object);
            this.localFolder.SetupFilesAndDirectories(file.Object, folder.Object);

            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<AbstractFolderEvent>(e => e.Local == MetaDataChangeType.NONE && e.Remote == MetaDataChangeType.NONE)), Times.Never());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Never());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneRemoteFolderRenamed() {
            this.SetUpMocks();
            DateTime modification = DateTime.UtcNow;
            string oldFolderName = "folderName";
            string newFolderName = "newfolderName";
            string folderId = "folderId";
            var localGuid = Guid.NewGuid();
            var oldLocalFolder = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, oldFolderName));
            oldLocalFolder.Setup(f => f.LastWriteTimeUtc).Returns(modification);
            oldLocalFolder.SetupGuid(localGuid);
            var storedFolder = new MappedObject(oldFolderName, folderId, MappedObjectType.Folder, this.remoteRootId, "changeToken") {
                Guid = localGuid,
                LastLocalWriteTimeUtc = modification
            };
            this.localFolder.SetupDirectories(oldLocalFolder.Object);
            this.storage.SaveMappedObject(storedFolder);
            var renamedRemoteFolder = MockOfIFolderUtil.CreateRemoteFolderMock(folderId, newFolderName, this.remoteRootPath + newFolderName, this.remoteRootId, "newChangeToken");
            this.remoteFolder.SetupChildren(renamedRemoteFolder.Object);
            Assert.That(this.CreateCrawler().Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(
                q =>
                q.AddEvent(
                It.Is<FolderEvent>(
                e =>
                e.Remote == MetaDataChangeType.CHANGED &&
                e.Local == MetaDataChangeType.NONE &&
                e.RemoteFolder.Equals(renamedRemoteFolder.Object))),
                Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneRemoteFileRenamedAndContentHashIsNotAvailable() {
            this.SetUpMocks();
            string oldFileName = "fileName";
            string newFileName = "newfileName";
            string fileId = "fileId";
            var localGuid = Guid.NewGuid();
            DateTime modificationDate = DateTime.UtcNow;
            var oldLocalFile = this.fsFactory.AddFile(Path.Combine(this.localRootPath, oldFileName));
            oldLocalFile.SetupGuid(localGuid);
            oldLocalFile.SetupLastWriteTimeUtc(modificationDate);
            var storedFile = new MappedObject(oldFileName, fileId, MappedObjectType.File, this.remoteRootId, "changeToken") {
                Guid = localGuid,
                LastLocalWriteTimeUtc = modificationDate,
                ChecksumAlgorithmName = "SHA-1"
            };
            this.localFolder.SetupFiles(oldLocalFile.Object);
            this.storage.SaveMappedObject(storedFile);
            var renamedRemoteFile = MockOfIDocumentUtil.CreateRemoteDocumentMock(null, fileId, newFileName, this.remoteRootId, changeToken: "newChangeToken");
            this.remoteFolder.SetupChildren(renamedRemoteFile.Object);
            Assert.That(this.CreateCrawler().Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(
                q =>
                q.AddEvent(
                It.Is<FileEvent>(
                e =>
                e.Remote == MetaDataChangeType.CHANGED &&
                e.RemoteContent == ContentChangeType.CHANGED &&
                e.Local == MetaDataChangeType.NONE &&
                e.RemoteFile.Equals(renamedRemoteFile.Object))),
                Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneRemoteFileRenamedAndContentHashIsAvailable() {
            this.SetUpMocks();
            byte[] checksum = new byte[20];
            string type = "SHA-1";
            string oldFileName = "fileName";
            string newFileName = "newfileName";
            string fileId = "fileId";
            DateTime lastModification = DateTime.UtcNow;
            var localGuid = Guid.NewGuid();
            var oldLocalFile = this.fsFactory.AddFile(Path.Combine(this.localRootPath, oldFileName));
            oldLocalFile.SetupLastWriteTimeUtc(lastModification);
            oldLocalFile.SetupGuid(localGuid);
            var storedFile = new MappedObject(oldFileName, fileId, MappedObjectType.File, this.remoteRootId, "changeToken") {
                Guid = localGuid,
                ChecksumAlgorithmName = type,
                LastChecksum = checksum,
                LastLocalWriteTimeUtc = lastModification
            };
            this.localFolder.SetupFiles(oldLocalFile.Object);
            this.storage.SaveMappedObject(storedFile);
            var renamedRemoteFile = MockOfIDocumentUtil.CreateRemoteDocumentMock(null, fileId, newFileName, this.remoteRootId, changeToken: "newChangeToken");
            renamedRemoteFile.SetupContentStreamHash(checksum, type);
            this.remoteFolder.SetupChildren(renamedRemoteFile.Object);
            Assert.That(this.CreateCrawler().Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(
                q =>
                q.AddEvent(
                It.Is<FileEvent>(
                e =>
                e.Remote == MetaDataChangeType.CHANGED &&
                e.RemoteContent == ContentChangeType.NONE &&
                e.Local == MetaDataChangeType.NONE &&
                e.RemoteFile.Equals(renamedRemoteFile.Object))),
                Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneRemoteFolderMoved() {
            this.SetUpMocks();
            string oldFolderName = "folderName";
            string folderId = "folderId";
            var localGuid = Guid.NewGuid();
            DateTime modification = DateTime.UtcNow;
            var localTargetGuid = Guid.NewGuid();
            var oldLocalFolder = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, oldFolderName));
            oldLocalFolder.SetupLastWriteTimeUtc(modification);
            oldLocalFolder.SetupGuid(localGuid);
            var localTargetFolder = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, "target"));
            localTargetFolder.SetupLastWriteTimeUtc(modification);
            localTargetFolder.SetupGuid(localTargetGuid);
            var storedFolder = new MappedObject(oldFolderName, folderId, MappedObjectType.Folder, this.remoteRootId, "changeToken") {
                Guid = localGuid,
                LastLocalWriteTimeUtc = modification
            };
            var storedTargetFolder = new MappedObject("target", "targetId", MappedObjectType.Folder, this.remoteRootId, "changeToken") {
                Guid = localTargetGuid,
                LastLocalWriteTimeUtc = modification
            };
            this.localFolder.SetupDirectories(oldLocalFolder.Object, localTargetFolder.Object);
            this.storage.SaveMappedObject(storedFolder);
            this.storage.SaveMappedObject(storedTargetFolder);
            var targetFolder = MockOfIFolderUtil.CreateRemoteFolderMock("targetId", "target", this.remoteRootPath + "target", this.remoteRootId, "changeToken");
            var renamedRemoteFolder = MockOfIFolderUtil.CreateRemoteFolderMock(folderId, oldFolderName, this.remoteRootPath + oldFolderName, "targetId", "newChangeToken");
            targetFolder.SetupChildren(renamedRemoteFolder.Object);
            this.remoteFolder.SetupChildren(targetFolder.Object);
            Assert.That(this.CreateCrawler().Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(
                q =>
                q.AddEvent(
                It.Is<FolderEvent>(
                e =>
                e.Remote == MetaDataChangeType.MOVED &&
                e.Local == MetaDataChangeType.NONE &&
                e.RemoteFolder.Equals(renamedRemoteFolder.Object))),
                Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneRemoteFolderRemoved() {
            this.SetUpMocks();
            Guid uuid = Guid.NewGuid();
            DateTime modification = DateTime.UtcNow;
            var oldLocalFolder = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, "folderName"));
            oldLocalFolder.SetupGuid(uuid);
            oldLocalFolder.SetupLastWriteTimeUtc(modification);
            var storedFolder = new MappedObject("folderName", "folderId", MappedObjectType.Folder, this.remoteRootId, "changeToken") {
                Guid = uuid,
                LastLocalWriteTimeUtc = modification
            };
            this.storage.SaveMappedObject(storedFolder);
            this.localFolder.SetupDirectories(oldLocalFolder.Object);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FolderEvent>(e => e.Remote == MetaDataChangeType.DELETED && e.LocalFolder.Equals(oldLocalFolder.Object))), Times.Once());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void OneRemoteAndTheSameLocalFolderRemoved() {
            this.SetUpMocks();
            string folderName = "folderName";
            var oldLocalFolder = this.fsFactory.AddDirectory(Path.Combine(this.localRootPath, folderName));
            var storedFolder = new MappedObject(folderName, "folderId", MappedObjectType.Folder, this.remoteRootId, "changeToken");
            this.storage.SaveMappedObject(storedFolder);
            var crawler = this.CreateCrawler();

            Assert.That(crawler.Handle(new StartNextSyncEvent()), Is.True);
            this.queue.Verify(q => q.AddEvent(It.Is<FolderEvent>(e => e.Remote == MetaDataChangeType.DELETED && e.Local == MetaDataChangeType.DELETED && e.LocalFolder.Equals(oldLocalFolder.Object))), Times.Once());
            this.queue.Verify(q => q.AddEvent(It.Is<FolderEvent>(e => e.Remote == MetaDataChangeType.NONE && e.Local == MetaDataChangeType.DELETED && e.LocalFolder.Equals(oldLocalFolder.Object))), Times.Never());
            this.queue.Verify(q => q.AddEvent(It.Is<FolderEvent>(e => e.Remote == MetaDataChangeType.DELETED && e.Local == MetaDataChangeType.NONE && e.LocalFolder.Equals(oldLocalFolder.Object))), Times.Never());
            this.VerifyThatCountOfAddedEventsIsLimitedTo(Times.Once());
            this.VerifyThatListenerHasBeenUsed();
        }

        [Test]
        public void PathTooLongExceptionGetsEmbeddedIntoInteractionNeededException([Values(true, false)]bool filesThrowsException) {
            this.SetUpMocks();
            var crawler = this.CreateCrawler();
            if (filesThrowsException) {
                this.localFolder.Setup(f => f.GetFiles()).Throws<PathTooLongException>();
            } else {
                this.localFolder.Setup(f => f.GetDirectories()).Throws<PathTooLongException>();
            }

            var exception = Assert.Throws<InteractionNeededException>(() => crawler.Handle(new StartNextSyncEvent()));

            Assert.That(exception.InnerException, Is.TypeOf<PathTooLongException>());
        }

        [Test]
        public void SetRemoteReadOnlyFlagToLocal(
            [Values(true, false)]bool localIsReadOnly,
            [Values(true, false)]bool remoteIsReadOnly)
        {
            this.SetUpMocks();
            this.localFolder.SetupReadOnly(localIsReadOnly);
            this.remoteFolder.SetupReadOnly(remoteIsReadOnly);
            var underTest = this.CreateCrawler();
            Assert.That(underTest.Handle(new StartNextSyncEvent()), Is.True);

            Assert.That(this.localFolder.Object.ReadOnly, Is.EqualTo(remoteIsReadOnly));
            this.localFolder.VerifySet((l) => l.ReadOnly = remoteIsReadOnly, localIsReadOnly != remoteIsReadOnly ? Times.Once() : Times.Never());
        }

        [Test]
        public void ConnectionExceptionsAreThrownWithoutPassingAnythingToQueue() {
            this.SetUpMocks();
            this.remoteFolder.Setup(f => f.GetChildren()).Throws<CmisConnectionException>();
            var underTest = this.CreateCrawler();

            Assert.Throws<CmisConnectionException>(() => underTest.Handle(new StartNextSyncEvent()));
            this.queue.VerifyThatNoEventIsAdded();
        }

        #region boilerplatecode
        public void Dispose() {
            if (this.storageEngine != null) {
                this.storageEngine.Dispose();
                this.storageEngine = null;
            }
        }
        #endregion

        protected DescendantsCrawler CreateCrawler() {
            var generator = new CrawlEventGenerator(this.storage, this.fsFactory.Object);
            var treeBuilder = new DescendantsTreeBuilder(this.storage, this.remoteFolder.Object, this.localFolder.Object, this.filter, Mock.Of<IIgnoredEntitiesStorage>());
            var notifier = new CrawlEventNotifier(this.queue.Object);
            return new DescendantsCrawler(this.queue.Object, treeBuilder, generator, notifier, this.listener.Object);
        }

        protected void VerifyThatListenerHasBeenUsed() {
            this.listener.Verify(l => l.ActivityStarted(), Times.AtLeastOnce());
            this.listener.Verify(l => l.ActivityStopped(), Times.AtLeastOnce());
        }

        private void VerifyThatCountOfAddedEventsIsLimitedTo(Times times) {
            this.queue.Verify(q => q.AddEvent(It.IsAny<AbstractFolderEvent>()), times);
        }
    }
}