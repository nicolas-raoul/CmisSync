//-----------------------------------------------------------------------
// <copyright file="TransmissionController.cs" company="GRAU DATA AG">
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
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;

    using CmisSync.Lib;

    /// <summary>
    /// File transmission types.
    /// </summary>
    public enum TransmissionType {
        /// <summary>
        /// A new file is uploaded
        /// </summary>
        UPLOAD_NEW_FILE,

        /// <summary>
        /// A locally modified file is uploaded
        /// </summary>
        UPLOAD_MODIFIED_FILE,

        /// <summary>
        /// A new remote file is downloaded
        /// </summary>
        DOWNLOAD_NEW_FILE,

        /// <summary>
        /// A remotely modified file is downloaded
        /// </summary>
        DOWNLOAD_MODIFIED_FILE
    }

    /// <summary>
    /// Transmission status.
    /// </summary>
    public enum TransmissionStatus {
        /// <summary>
        /// Transmission is going on.
        /// </summary>
        TRANSMITTING,

        /// <summary>
        /// Transmission is requested to be aborted.
        /// </summary>
        ABORTING,

        /// <summary>
        /// Transmission is aborted.
        /// </summary>
        ABORTED,

        /// <summary>
        /// Transmission is paused.
        /// </summary>
        PAUSED,

        /// <summary>
        /// Transmission is finished successfully
        /// </summary>
        FINISHED
    }

    /// <summary>
    /// File transmission event.
    /// This event should be queued only once. The progress will not be reported on the queue.
    /// Interested entities should add themselfs as TransmissionEventHandler on the event TransmissionStatus to get informed about the progress.
    /// </summary>
    public class Transmission : INotifyPropertyChanged {
        private readonly TransmissionType type;
        private string relativePath = string.Empty;
        private string repo = string.Empty;
        private TransmissionStatus status = TransmissionStatus.TRANSMITTING;
        private long? length = null;
        private long? position = null;
        private long? bitsPerSecond = null;
        private Exception failedException = null;
        DateTime lastModification = DateTime.Now;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmisSync.Lib.FileTransmission.TransmissionController"/> class.
        /// </summary>
        /// <param name='type'>
        /// Type of the transmission.
        /// </param>
        /// <param name='path'>
        /// Path to the file of the transmission.
        /// </param>
        /// <param name='cachePath'>
        /// If a download runs and a cache file is used, this should be the path to the cache file
        /// </param>
        public Transmission(TransmissionType type, string path, string cachePath = null) {
            if (path == null) {
                throw new ArgumentNullException("Argument null in FSEvent Constructor", "path");
            }

            this.type = type;
            this.Path = path;
            this.CachePath = cachePath;
        }

        /// <summary>
        /// Occurs when property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the type of the transmission.
        /// </summary>
        /// <value>
        /// The type of the transmission.
        /// </value>
        public TransmissionType Type {
            get {
                return this.type;
            }
        }

        /// <summary>
        /// Gets or sets the repository.
        /// </summary>
        /// <value>The repository.</value>
        public string Repository {
            get {
                return this.repo;
            }

            set {
                if (this.repo != value) {
                    this.repo = value;
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.Repository));
                }
            }
        }

        /// <summary>
        /// Gets or sets the relative path.
        /// </summary>
        /// <value>The relative path.</value>
        public string RelativePath {
            get {
                return this.relativePath;
            }

            set {
                if (this.relativePath != value) {
                    this.relativePath = value;
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.RelativePath));
                }
            }
        }

        /// <summary>
        /// Gets the path to the file, which is transmitted.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; private set; }

        /// <summary>
        /// Gets download cache file. If a download happens, a cache file could be used. If the cache is used, this should be the path.
        /// </summary>
        /// <value>
        /// The cache path.
        /// </value>
        public string CachePath { get; private set; }

        /// <summary>
        /// Gets or sets the length of the file transmission in bytes.
        /// </summary>
        /// <value>
        /// The transmission length.
        /// </value>
        public long? Length {
            get {
                return this.length;
            }

            set {
                if (this.length != value) {
                    this.length = value;
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.Length));
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.Percent));
                }
            }
        }

        /// <summary>
        /// Gets or sets the actual position of the transmission progress.
        /// </summary>
        /// <value>
        /// The actual transmission position.
        /// </value>
        public long? Position {
            get {
                return this.position;
            }

            set {
                if (this.position != value) {
                    this.position = value;
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.Position));
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.Percent));
                }
            }
        }

        /// <summary>
        /// Gets the status of the transmission.
        /// </summary>
        /// <value>The status.</value>
        public TransmissionStatus Status {
            get {
                return this.status;
            }

            set {
                if (this.status != value) {
                    this.status = value;
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.Status));
                }
            }
        }

        /// <summary>
        /// Gets or sets the failed exception of the transmission, if any exception occures.
        /// </summary>
        /// <value>
        /// Transmission failed exception.
        /// </value>
        public Exception FailedException {
            get {
                return this.failedException;
            }

            set {
                if (this.failedException != value) {
                    this.failedException = value;
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.FailedException));
                    this.Status = TransmissionStatus.ABORTED;
                }
            }
        }

        /// <summary>
        /// Gets or sets the bits per second. Can be null if it is unknown.
        /// </summary>
        /// <value>
        /// The bits per second or null.
        /// </value>
        public long? BitsPerSecond {
            get {
                return this.bitsPerSecond;
            }

            set {
                if (this.bitsPerSecond != value) {
                    this.bitsPerSecond = value;
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.BitsPerSecond));
                }
            }
        }

        /// <summary>
        /// Gets the percentage of the transmission progress if known. Otherwise null.
        /// </summary>
        /// <value>
        /// The percentage of the transmission progress.
        /// </value>
        public double? Percent {
            get {
                if (this.Length == null || this.Position == null || this.Position < 0 || this.Length < 0) {
                    return null;
                }

                if (this.Length == 0) {
                    return 100d;
                }

                return ((double)this.Position * 100d) / (double)this.Length;
            }
        }

        public DateTime LastModification {
            get {
                return this.lastModification;
            }

            set {
                if (this.lastModification != value) {
                    this.lastModification = value;
                    this.NotifyPropertyChanged(Utils.NameOf(() => this.LastModification));
                }
            }
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="CmisSync.Lib.FileTransmission.TransmissionController"/> object.
        /// </summary>
        /// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        /// hash table.</returns>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        /// <summary>
        /// Calculates the bits per second.
        /// </summary>
        /// <returns>
        /// The bits per second.
        /// </returns>
        /// <param name='start'>
        /// Start time for calculation.
        /// </param>
        /// <param name='end'>
        /// End time for calculation.
        /// </param>
        /// <param name='bytes'>
        /// Bytes in period between start end end.
        /// </param>
        public static long? CalcBitsPerSecond(DateTime start, DateTime end, long bytes) {
            if (end < start) {
                throw new ArgumentException("The end of a transmission must be higher than the start");
            }

            if (start == end) {
                return null;
            }

            TimeSpan difference = end - start;
            double seconds = difference.TotalMilliseconds / 1000d;
            double dbytes = bytes;
            return (long)((dbytes * 8) / seconds);
        }

        public void Pause() {
            if (this.Status == TransmissionStatus.TRANSMITTING) {
                this.Status = TransmissionStatus.PAUSED;
            }
        }

        public void Resume() {
            if (this.Status == TransmissionStatus.PAUSED) {
                this.Status = TransmissionStatus.TRANSMITTING;
            }
        }

        public void Abort() {
            if (this.Status == TransmissionStatus.PAUSED || this.Status == TransmissionStatus.TRANSMITTING) {
                this.Status = TransmissionStatus.ABORTING;
            }
        }

        /// <summary>
        /// This method is called by the Set accessor of each property.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        private void NotifyPropertyChanged(string propertyName) {
            if (string.IsNullOrEmpty(propertyName)) {
                throw new ArgumentNullException("Given property name is null");
            }

            var handler = this.PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
 }