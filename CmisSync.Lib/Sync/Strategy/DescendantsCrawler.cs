//-----------------------------------------------------------------------
// <copyright file="DescendantsCrawler.cs" company="GRAU DATA AG">
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

namespace CmisSync.Lib.Sync.Strategy
{
    using System;
    using System.Collections.Generic;

    using CmisSync.Lib.Data;
    using CmisSync.Lib.Events;
    using CmisSync.Lib.Storage;
    using DotCMIS.Client;

    using log4net;

    /// <summary>
    /// Decendants crawler.
    /// </summary>
    public class DescendantsCrawler : ReportingSyncEventHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DescendantsCrawler));
        private IFolder remoteFolder;
        private IDirectoryInfo localFolder;
        private IMetaDataStorage storage;
        private IFileSystemInfoFactory fsFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmisSync.Lib.Sync.Strategy.DescendantsCrawler"/> class.
        /// </summary>
        /// <param name="queue">Sync Event Queue.</param>
        /// <param name="remoteFolder">Remote folder.</param>
        /// <param name="localFolder">Local folder.</param>
        /// <param name="storage">Meta data storage.</param>
        /// <param name="fsFactory">File system info factory.</param>
        public DescendantsCrawler(
            ISyncEventQueue queue,
            IFolder remoteFolder,
            IDirectoryInfo localFolder,
            IMetaDataStorage storage,
            IFileSystemInfoFactory fsFactory = null)
            : base(queue)
        {
            if (remoteFolder == null) {
                throw new ArgumentNullException("Given remoteFolder is null");
            }

            if (localFolder == null) {
                throw new ArgumentNullException("Given localFolder is null");
            }

            if (storage == null) {
                throw new ArgumentNullException("Given storage is null");
            }

            this.storage = storage;
            this.remoteFolder = remoteFolder;
            this.localFolder = localFolder;

            if (fsFactory == null) {
                this.fsFactory = new FileSystemInfoFactory();
            } else {
                this.fsFactory = fsFactory;
            }
        }

        /// <summary>
        /// Handles StartNextSync events.
        /// </summary>
        /// <param name="e">The event to handle.</param>
        /// <returns>true if handled</returns>
        public override bool Handle(ISyncEvent e)
        {
            if(e is StartNextSyncEvent) {
                Logger.Debug("Starting DecendantsCrawlSync upon " + e);
                this.CrawlDescendants();
                this.Queue.AddEvent(new FullSyncCompletedEvent(e as StartNextSyncEvent));
                return true;
            }

            return false;
        }

        private void CrawlDescendants()
        {
            List<IFileableCmisObject> addedRemoteObjects = new List<IFileableCmisObject>();
            List<IFileSystemInfo> addedLocalObjects = new List<IFileSystemInfo>();
            List<IFileSystemInfo> removedLocalObjects = new List<IFileSystemInfo>();
            List<IFileableCmisObject> removedRemoteObjects = new List<IFileableCmisObject>();
            List<MappedObject> storedObjects = new List<MappedObject>();
            this.CrawlDescendants(
                this.localFolder,
                this.remoteFolder,
                this.remoteFolder.GetDescendants(-1),
                addedRemoteObjects,
                addedLocalObjects,
                removedLocalObjects,
                removedRemoteObjects,
                storedObjects);

            // Send out Events to queue
            this.InformAboutRemoteObjectsAdded(addedRemoteObjects);
            this.InformAboutLocalObjectsAdded(addedLocalObjects);
            this.InformAboutRemoteObjectsDeleted(removedRemoteObjects);
            this.InformAboutLocalObjectsDeleted(removedLocalObjects);
        }

        private void CrawlDescendants(
            IDirectoryInfo localFolder,
            IFolder remoteFolder,
            IList<ITree<IFileableCmisObject>> descendants,
            List<IFileableCmisObject> addedRemoteObjects,
            List<IFileSystemInfo> addedLocalObjects,
            List<IFileSystemInfo> removedLocalObjects,
            List<IFileableCmisObject> removedRemoteObjects,
            List<MappedObject> storedObjects)
        {
            var storedChildren = this.storage.GetChildren(this.storage.GetObjectByRemoteId(remoteFolder.Id));

            // Find all new remote objects in this folder hierarchy
            var desc = this.remoteFolder.GetDescendants(-1);
            foreach (var child in desc) {
                var matchingStoredObject = storedChildren.Find(c => c.RemoteObjectId == child.Item.Id);
                if (matchingStoredObject == null) {
                    addedRemoteObjects.Add(child.Item);
                }
            }

            // Find all new local object in this folder hierarchy
            var localDirectories = this.localFolder.GetDirectories();
            foreach (var localDir in localDirectories) {
                var matchingStoredObject = storedChildren.Find(c => c.Name == localDir.Name);
                if (matchingStoredObject == null) {
                    addedLocalObjects.Add(localDir);
                }
            }

            // Find all new local object in this folder hierarchy
            var localFiles = this.localFolder.GetFiles();
            foreach (var localFile in localFiles) {
                var matchingStoredObject = storedChildren.Find(c => c.Name == localFile.Name);
                if (matchingStoredObject == null) {
                    addedLocalObjects.Add(localFile);
                }
            }
        }

        private void InformAboutRemoteObjectsAdded(IList<IFileableCmisObject> objects) {
            foreach (var addedRemotely in objects) {
                if (addedRemotely is IFolder) {
                    this.Queue.AddEvent(new FolderEvent(null, addedRemotely as IFolder, this) { Remote = MetaDataChangeType.CREATED });
                } else if (addedRemotely is IDocument) {
                    this.Queue.AddEvent(new FileEvent(null, null, addedRemotely as IDocument) { Remote = MetaDataChangeType.CREATED });
                }
            }
        }

        private void InformAboutLocalObjectsAdded(IList<IFileSystemInfo> objects) {
            foreach (var addedLocally in objects) {
                if (addedLocally is IDirectoryInfo) {
                    this.Queue.AddEvent(new FolderEvent(addedLocally as IDirectoryInfo, null, this) { Local = MetaDataChangeType.CREATED });
                } else if (addedLocally is IFileInfo) {
                    this.Queue.AddEvent(new FileEvent(addedLocally as IFileInfo, null, null) { Local = MetaDataChangeType.CREATED });
                }
            }
        }

        private void InformAboutLocalObjectsDeleted(IList<IFileSystemInfo> objects) {
            foreach (var deleted in objects) {
                if (deleted is IDirectoryInfo) {
                    this.Queue.AddEvent(new FolderEvent(deleted as IDirectoryInfo, null, this) { Local = MetaDataChangeType.DELETED });
                } else if (deleted is IFileInfo) {
                    this.Queue.AddEvent(new FileEvent(deleted as IFileInfo) { Local = MetaDataChangeType.DELETED });
                }
            }
        }

        private void InformAboutRemoteObjectsDeleted(IList<IFileableCmisObject> objects) {
            foreach (var deleted in objects) {
                if (deleted is IFolder) {
                    this.Queue.AddEvent(new FolderEvent(null, deleted as IFolder, this) { Remote = MetaDataChangeType.DELETED });
                } else if (deleted is IDocument) {
                    this.Queue.AddEvent(new FileEvent(null, null, deleted as IDocument) { Remote = MetaDataChangeType.DELETED });
                }
            }
        }
    }
}