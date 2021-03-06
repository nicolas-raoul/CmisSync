﻿//-----------------------------------------------------------------------
// <copyright file="IFileUploader.cs" company="GRAU DATA AG">
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

namespace CmisSync.Lib.FileTransmission {
    using System;
    using System.IO;
    using System.Security.Cryptography;

    using CmisSync.Lib.Events;

    using DataSpace.Common.Transmissions;

    using DotCMIS.Client;

    /// <summary>
    /// I file Upload Module must implement this interface.
    /// </summary>
    public interface IFileUploader : IDisposable {
        /// <summary>
        /// Uploads the localFileStream to remoteDocument.
        /// </summary>
        /// <returns>
        /// The new CMIS document.
        /// </returns>
        /// <param name='remoteDocument'>
        /// Remote document where the local content should be uploaded to.
        /// </param>
        /// <param name='localFileStream'>
        /// Local file stream.
        /// </param>
        /// <param name='transmission'>
        /// Transmission status where the uploader should report its uploading status.
        /// </param>
        /// <param name='hashAlg'>
        /// Hash alg which should be used to calculate a checksum over the uploaded content.
        /// </param>
        /// <param name='overwrite'>
        /// If true, the local content will overwrite the existing content.
        /// </param>
        /// <param name="update">Will be called on every new chunk which is uploaded.</param>
        IDocument UploadFile(
            IDocument remoteDocument,
            Stream localFileStream,
            Transmission transmission,
            HashAlgorithm hashAlg,
            bool overwrite = true,
            Action<byte[], long> update = null);

        /// <summary>
        /// Appends the localFileStream to the remoteDocument.
        /// </summary>
        /// <returns>
        /// The new CMIS document.
        /// </returns>
        /// <param name='remoteDocument'>
        /// Remote document where the local content should be appended to.
        /// </param>
        /// <param name='localFileStream'>
        /// Local file stream.
        /// </param>
        /// <param name='transmission'>
        /// Transmission status where the uploader should report its appending status.
        /// </param>
        /// <param name='hashAlg'>
        /// Hash alg which should be used to calculate a checksum over the appended content.
        /// </param>
        IDocument AppendFile(
            IDocument remoteDocument,
            Stream localFileStream,
            Transmission transmission,
            HashAlgorithm hashAlg);
    }
}