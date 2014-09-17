//-----------------------------------------------------------------------
// <copyright file="LocalObjectChangedRemoteObjectChanged.cs" company="GRAU DATA AG">
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

namespace CmisSync.Lib.Consumer.SituationSolver
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    using CmisSync.Lib.Cmis.ConvenienceExtenders;
    using CmisSync.Lib.Events;
    using CmisSync.Lib.Storage.Database;
    using CmisSync.Lib.Storage.Database.Entities;
    using CmisSync.Lib.Storage.FileSystem;

    using DotCMIS.Client;

    /// <summary>
    /// Local object changed and remote object changed.
    /// </summary>
    public class LocalObjectChangedRemoteObjectChanged : AbstractEnhancedSolver
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="CmisSync.Lib.Consumer.SituationSolver.LocalObjectChangedRemoteObjectChanged"/> class.
        /// </summary>
        /// <param name="session">Cmis session.</param>
        /// <param name="storage">Meta data storage.</param>
        /// <param name="serverCanModifyCreationAndModificationDate">If set to <c>true</c> server can modify creation and modification date.</param>
        public LocalObjectChangedRemoteObjectChanged(
            ISession session,
            IMetaDataStorage storage,
            bool serverCanModifyCreationAndModificationDate) : base(session, storage, serverCanModifyCreationAndModificationDate) {
        }

        /// <summary>
        /// Solve the specified situation by using localFile and remote object.
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
            var obj = this.Storage.GetObjectByRemoteId(remoteId.Id);
            if (localFileSystemInfo is IDirectoryInfo) {
                obj.LastLocalWriteTimeUtc = localFileSystemInfo.LastWriteTimeUtc;
                obj.LastRemoteWriteTimeUtc = (remoteId as IFolder).LastModificationDate;
                obj.LastChangeToken = (remoteId as IFolder).ChangeToken;
            } else if (localFileSystemInfo is IFileInfo) {
                var fileInfo = localFileSystemInfo as IFileInfo;
                var doc = remoteId as IDocument;
                bool updateLocalDate = false;
                bool updateRemoteDate = false;
                if (remoteContent == ContentChangeType.NONE) {
                    if (fileInfo.IsContentChangedTo(obj, true)) {
                        // Upload local content
                        throw new NotImplementedException();
                    } else {
                        // Just date sync
                        if (doc.LastModificationDate != null && fileInfo.LastWriteTimeUtc < (DateTime)doc.LastModificationDate) {
                            updateLocalDate = true;
                        } else {
                            updateRemoteDate = true;
                        }
                    }
                } else {
                    byte[] actualLocalHash;
                    if (fileInfo.IsContentChangedTo(obj, out actualLocalHash, true)) {
                        // Check if both are changed to the same value
                        if (actualLocalHash == null) {
                            using (var f = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) {
                                actualLocalHash = SHA1Managed.Create().ComputeHash(f);
                            }
                        }

                        byte[] remoteHash = doc.ContentStreamHash();
                        if (remoteHash != null && actualLocalHash.SequenceEqual(remoteHash)) {
                            // Both files are equal
                            obj.LastChecksum = remoteHash;
                            obj.LastContentSize = fileInfo.Length;

                            // Sync dates
                            if (doc.LastModificationDate != null && fileInfo.LastWriteTimeUtc < (DateTime)doc.LastModificationDate) {
                                updateLocalDate = true;
                            } else {
                                updateRemoteDate = true;
                            }
                        } else {
                            // Both are different => Check modification dates
                            throw new NotImplementedException();
                        }
                    } else {
                        // Download remote content
                        throw new NotImplementedException();
                    }
                }

                if (this.ServerCanModifyDateTimes) {
                    if (updateLocalDate) {
                        fileInfo.LastWriteTimeUtc = (DateTime)doc.LastModificationDate;
                    } else if (updateRemoteDate) {
                        doc.UpdateLastWriteTimeUtc(fileInfo.LastWriteTimeUtc);
                    } else {
                        throw new ArgumentException();
                    }
                }

                obj.LastChangeToken = doc.ChangeToken;
                obj.LastLocalWriteTimeUtc = localFileSystemInfo.LastWriteTimeUtc;
                obj.LastRemoteWriteTimeUtc = doc.LastModificationDate;
            }

            this.Storage.SaveMappedObject(obj);
        }
    }
}