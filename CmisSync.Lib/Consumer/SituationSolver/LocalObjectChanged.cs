﻿//-----------------------------------------------------------------------
// <copyright file="LocalObjectChanged.cs" company="GRAU DATA AG">
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

namespace CmisSync.Lib.Consumer.SituationSolver {
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    using CmisSync.Lib.Cmis.ConvenienceExtenders;
    using CmisSync.Lib.Events;
    using CmisSync.Lib.Queueing;
    using CmisSync.Lib.Storage.Database;
    using CmisSync.Lib.Storage.Database.Entities;
    using CmisSync.Lib.Storage.FileSystem;

    using DataSpace.Common.Transmissions;

    using DotCMIS.Client;
    using DotCMIS.Exceptions;

    using log4net;

    /// <summary>
    /// A local object has been changed and should be uploaded (if necessary) to server or updated on the server.
    /// </summary>
    public class LocalObjectChanged : AbstractEnhancedSolver {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LocalObjectChanged));

        private ITransmissionFactory transmissionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmisSync.Lib.Consumer.SituationSolver.LocalObjectChanged"/> class.
        /// </summary>
        /// <param name="session">Cmis session.</param>
        /// <param name="storage">Meta data storage.</param>
        /// <param name="transmissionStorage">Transmission storage.</param>
        /// <param name="transmissionFactory">Transmission factory.</param>
        public LocalObjectChanged(
            ISession session,
            IMetaDataStorage storage,
            IFileTransmissionStorage transmissionStorage,
            ITransmissionFactory transmissionFactory) : base(session, storage, transmissionStorage)
        {
            if (transmissionFactory == null) {
                throw new ArgumentNullException("transmissionFactory");
            }

            this.transmissionFactory = transmissionFactory;
        }

        /// <summary>
        /// Solve the specified situation by using the storage, localFile and remoteId.
        /// Uploads the file content if content has been changed. Otherwise simply saves the
        /// last modification date.
        /// </summary>
        /// <param name="localFileSystemInfo">Local filesystem info instance.</param>
        /// <param name="remoteId">Remote identifier or object.</param>
        /// <param name="localContent">Hint if the local content has been changed.</param>
        /// <param name="remoteContent">Information if the remote content has been changed.</param>
        public override void Solve(
            IFileSystemInfo localFileSystemInfo,
            IObjectId remoteId,
            ContentChangeType localContent = ContentChangeType.NONE,
            ContentChangeType remoteContent = ContentChangeType.NONE)
        {
            if (localFileSystemInfo == null) {
                throw new ArgumentNullException("localFileSystemInfo");
            }

            if (remoteId == null) {
                throw new ArgumentNullException("remoteId");
            }

            var fullName = localFileSystemInfo.FullName;
            if (!localFileSystemInfo.Exists) {
                throw new ArgumentException("Given local path does not exists: " + fullName);
            }

            // Match local changes to remote changes and updated them remotely
            var mappedObject = this.Storage.GetObject(localFileSystemInfo);
            if (mappedObject == null) {
                throw new ArgumentException(string.Format("Could not find db entry for {0} => invoke crawl sync", fullName));
            }

            if (mappedObject.LastChangeToken != (remoteId as ICmisObjectProperties).ChangeToken) {
                throw new ArgumentException(string.Format("remote {1} {0} has also been changed since last sync => invoke crawl sync", remoteId.Id, remoteId is IDocument ? "document" : "folder"));
            }

            var localFile = localFileSystemInfo as IFileInfo;
            if (localFile != null && localFile.IsContentChangedTo(mappedObject, scanOnlyIfModificationDateDiffers: true)) {
                Logger.Debug(string.Format("\"{0}\" is different from {1}", fullName, mappedObject.ToString()));
                OperationsLogger.Debug(string.Format("Local file \"{0}\" has been changed", fullName));
                var doc = remoteId as IDocument;
                try {
                    var transmission = this.transmissionFactory.CreateTransmission(TransmissionType.UploadModifiedFile, fullName);
                    mappedObject.LastChecksum = this.UploadFile(localFile, doc, transmission);
                } catch (Exception ex) {
                    if (ex.InnerException is CmisPermissionDeniedException) {
                        OperationsLogger.Warn(string.Format("Local changed file \"{0}\" has not been uploaded: PermissionDenied", fullName));
                        return;
                    } else if (ex.InnerException is CmisStorageException) {
                        OperationsLogger.Warn(string.Format("Local changed file \"{0}\" has not been uploaded: StorageException", fullName), ex);
                        return;
                    }

                    throw;
                }

                mappedObject.LastRemoteWriteTimeUtc = doc.LastModificationDate;
                mappedObject.LastContentSize = localFile.Length;

                OperationsLogger.Info(string.Format("Local changed file \"{0}\" has been uploaded", fullName));
            }

            localFileSystemInfo.TryToSetReadOnlyStateIfDiffers(from: remoteId as ICmisObject);
            mappedObject.IsReadOnly = localFileSystemInfo.ReadOnly;
            var lastLocalWriteTimeUtc = localFileSystemInfo.LastWriteTimeUtc;
            if (this.ServerCanModifyDateTimes) {
                try {
                    var obj = remoteId as IFileableCmisObject;
                    if (obj != null) {
                        obj.UpdateLastWriteTimeUtc(lastLocalWriteTimeUtc);
                        mappedObject.LastRemoteWriteTimeUtc = obj.LastModificationDate ?? lastLocalWriteTimeUtc;
                    }
                } catch (CmisPermissionDeniedException) {
                    Logger.Debug(string.Format("Locally changed modification date \"{0}\" is not uploaded to the server: PermissionDenied", lastLocalWriteTimeUtc));
                }
            }

            mappedObject.LastChangeToken = (remoteId as ICmisObjectProperties).ChangeToken;
            mappedObject.LastLocalWriteTimeUtc = lastLocalWriteTimeUtc;
            this.Storage.SaveMappedObject(mappedObject);
        }
    }
}