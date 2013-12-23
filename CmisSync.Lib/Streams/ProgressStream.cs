using System;
using System.IO;
using CmisSync.Lib.Events;

namespace CmisSync.Lib
{
    namespace Streams
    {
        public class ProgressStream : Stream
        {
            private FileTransmissionEvent TransmissionEvent;
            private Stream Stream;

            public ProgressStream (Stream stream, FileTransmissionEvent e)
            {
                if (stream == null)
                    throw new ArgumentNullException ("The stream which progress should be reported cannot be null");
                if (e == null)
                    throw new ArgumentNullException ("The event, where to publish the prgress cannot be null");
                Stream = stream;
                TransmissionEvent = e;
            }

            public override bool CanRead {
                get {
                    return this.Stream.CanRead;
                }
            }

            public override bool CanSeek {
                get {
                    return this.Stream.CanSeek;
                }
            }

            public override bool CanWrite {
                get {
                    return this.Stream.CanWrite;
                }
            }

            public override long Length {
                get {
                    long length = this.Stream.Length;
                    if (length != this.TransmissionEvent.Status.Length)
                        this.TransmissionEvent.ReportProgress (new TransmissionProgressEventArgs () {Length = length});
                    return length;
                }
            }

            public override long Position {
                get {
                    long pos = this.Stream.Position;
                    if (pos != this.TransmissionEvent.Status.ActualPosition)
                        this.TransmissionEvent.ReportProgress (new TransmissionProgressEventArgs () {ActualPosition = pos});
                    return pos;
                }
                set {
                    this.Stream.Position = value;
                    if (value != this.TransmissionEvent.Status.ActualPosition)
                        this.TransmissionEvent.ReportProgress (new TransmissionProgressEventArgs () {ActualPosition = value});
                }
            }

            public override void Flush ()
            {
                this.Stream.Flush ();
            }

            public override IAsyncResult BeginRead (byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                return this.Stream.BeginRead (buffer, offset, count, callback, state);
            }

            public override long Seek (long offset, SeekOrigin origin)
            {
                long result = this.Stream.Seek (offset, origin);
                this.TransmissionEvent.ReportProgress (new TransmissionProgressEventArgs () {ActualPosition = this.Stream.Position});
                return result;
            }

            public override int Read (byte[] buffer, int offset, int count)
            {
                int result = this.Stream.Read (buffer, offset, count);
                if(count > 0)
                    this.TransmissionEvent.ReportProgress (new TransmissionProgressEventArgs () {ActualPosition = this.Stream.Position});
                return result;
            }

            public override void SetLength (long value)
            {
                this.Stream.SetLength (value);
                if (this.TransmissionEvent.Status.Length == null || value != (long) this.TransmissionEvent.Status.Length)
                    this.TransmissionEvent.ReportProgress (new TransmissionProgressEventArgs () {Length = value});
            }

            public override void Write (byte[] buffer, int offset, int count)
            {
                this.Stream.Write (buffer, offset, count);
                if(count > 0)
                    this.TransmissionEvent.ReportProgress (new TransmissionProgressEventArgs () {ActualPosition = this.Stream.Position});
            }

            protected override void Dispose (bool disposing)
            {
                base.Dispose (disposing);
            }
        }
    }
}
