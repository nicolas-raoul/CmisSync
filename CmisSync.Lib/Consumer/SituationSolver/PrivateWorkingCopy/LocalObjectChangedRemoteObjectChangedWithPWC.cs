﻿//-----------------------------------------------------------------------
// <copyright file="LocalObjectChangedRemoteObjectChangedWithPWC.cs" company="GRAU DATA AG">
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

namespace CmisSync.Lib.Consumer.SituationSolver.PWC {
    using System;

    using CmisSync.Lib.Cmis.ConvenienceExtenders;
    using CmisSync.Lib.Events;
    using CmisSync.Lib.Queueing;
    using CmisSync.Lib.Storage.Database;
    using CmisSync.Lib.Storage.Database.Entities;
    using CmisSync.Lib.Storage.FileSystem;

    using DataSpace.Common.Transmissions;

    using DotCMIS.Client;
    using DotCMIS.Exceptions;

    /// <summary>
    /// Local object changed remote object changed situations are decorated if only the local content has been changed. Otherwise the given fallback will solve the situation.
    /// </summary>
    public class LocalObjectChangedRemoteObjectChangedWithPWC : AbstractEnhancedSolverWithPWC {
        private readonly ISolver fallbackSolver;
        private readonly ITransmissionFactory transmissionFactory;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="CmisSync.Lib.Consumer.SituationSolver.PWC.LocalObjectChangedRemoteObjectChangedWithPWC"/> class.
        /// </summary>
        /// <param name="session">Cmis session.</param>
        /// <param name="storage">Meta data storage.</param>
        /// <param name="transmissionStorage">Transmission storage.</param>
        /// <param name="manager">Transmission manager.</param>
        /// <param name="localObjectChangedRemoteObjectChangedFallbackSolver">Local object changed remote object changed fallback solver.</param>
        public LocalObjectChangedRemoteObjectChangedWithPWC(
            ISession session,
            IMetaDataStorage storage,
            IFileTransmissionStorage transmissionStorage,
            ITransmissionFactory transmissionFactory,
            ISolver localObjectChangedRemoteObjectChangedFallbackSolver) : base(session, storage, transmissionStorage)
        {
            if (localObjectChangedRemoteObjectChangedFallbackSolver == null) {
                throw new ArgumentNullException("localObjectChangedRemoteObjectChangedFallbackSolver", "Given fallback solver is null");
            }

            this.fallbackSolver = localObjectChangedRemoteObjectChangedFallbackSolver;
            this.transmissionFactory = transmissionFactory;
        }

        /// <summary>
        /// Solve the specified situation by using localFile and remote object if the local file content is the only changed content. Otherwise the given fallback will be used.
        /// </summary>
        /// <param name="localFileSystemInfo">Local filesystem info instance.</param>
        /// <param name="remoteId">Remote identifier or object.</param>
        /// <param name="localContent">Hint if the local content has been changed.</param>
        /// <param name="remoteContent">Information if the remote content has been changed.</param>
        public override void Solve(
            IFileSystemInfo localFileSystemInfo,
            IObjectId remoteId,
            ContentChangeType localContent,
            ContentChangeType remoteContent)
        {
            var localFile = localFileSystemInfo as IFileInfo;
            if (localFile == null) {
                this.fallbackSolver.Solve(localFileSystemInfo, remoteId, localContent, remoteContent);
                return;
            }

            var remoteDocument = remoteId as IDocument;
            if (remoteContent != ContentChangeType.NONE) {
                this.fallbackSolver.Solve(localFile, remoteId, localContent, remoteContent);
                return;
            }

            bool updateLocalDate = false;
            bool updateRemoteDate = false;
            var remoteDocumentId = remoteDocument.Id;
            var obj = this.Storage.GetObjectByRemoteId(remoteDocumentId);

            if (localFile.IsContentChangedTo(obj, true)) {
                updateRemoteDate = true;
                var fullName = localFile.FullName;
                try {
                    var transmission = this.transmissionFactory.CreateTransmission(TransmissionType.UploadModifiedFile, fullName);
                    obj.LastChecksum = UploadFileWithPWC(localFile, ref remoteDocument, transmission);
                    obj.ChecksumAlgorithmName = "SHA-1";
                    obj.LastContentSize = remoteDocument.ContentStreamLength ?? localFile.Length;
                    remoteDocumentId = remoteDocument.Id;
                    if (remoteDocumentId != obj.RemoteObjectId) {
                        this.TransmissionStorage.RemoveObjectByRemoteObjectId(obj.RemoteObjectId);
                        obj.RemoteObjectId = remoteDocumentId;
                    }
                } catch (Exception ex) {
                    var inner = ex.InnerException;
                    if (inner is CmisPermissionDeniedException) {
                        OperationsLogger.Warn(string.Format("Local changed file \"{0}\" has not been uploaded: PermissionDenied", fullName));
                        return;
                    } else if (inner is CmisStorageException) {
                        OperationsLogger.Warn(string.Format("Local changed file \"{0}\" has not been uploaded: StorageException", fullName), ex);
                        return;
                    }

                    throw;
                }
            } else {
                //  just date sync
                var lastRemoteModification = remoteDocument.LastModificationDate;
                if (lastRemoteModification != null && localFile.LastWriteTimeUtc < lastRemoteModification) {
                    updateLocalDate = true;
                } else {
                    updateRemoteDate = true;
                }
            }

            if (this.ServerCanModifyDateTimes) {
                if (updateLocalDate) {
                    localFile.LastWriteTimeUtc = (DateTime)remoteDocument.LastModificationDate;
                } else if (updateRemoteDate) {
                    remoteDocument.UpdateLastWriteTimeUtc(localFile.LastWriteTimeUtc);
                } else {
                    throw new ArgumentException("Algorithm failure");
                }
            }

            obj.LastChangeToken = remoteDocument.ChangeToken;
            obj.LastLocalWriteTimeUtc = localFileSystemInfo.LastWriteTimeUtc;
            obj.LastRemoteWriteTimeUtc = remoteDocument.LastModificationDate;
            this.Storage.SaveMappedObject(obj);
        }
    }
}