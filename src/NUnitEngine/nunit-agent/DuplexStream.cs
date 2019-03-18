using System;
using System.IO;

namespace NUnit.Agent
{
    public sealed class DuplexStream : Stream
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;
        private readonly bool _keepOpen;

        public DuplexStream(Stream readStream, Stream writeStream, bool keepOpen)
        {
            if (readStream == null) throw new ArgumentNullException(nameof(readStream));
            if (writeStream == null) throw new ArgumentNullException(nameof(writeStream));

            if (!readStream.CanRead) throw new ArgumentException("The read stream must be readable.");
            if (!writeStream.CanWrite) throw new ArgumentException("The write stream must be writable.");

            _readStream = readStream;
            _writeStream = writeStream;
            _keepOpen = keepOpen;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_keepOpen)
            {
                _readStream.Dispose();
                _writeStream.Dispose();
            }
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _readStream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return _readStream.ReadByte();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _readStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _readStream.EndRead(asyncResult);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _writeStream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            _writeStream.WriteByte(value);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _writeStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _writeStream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            _writeStream.Flush();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
