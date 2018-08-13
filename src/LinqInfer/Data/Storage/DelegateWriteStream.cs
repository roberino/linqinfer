using System;
using System.IO;

namespace LinqInfer.Data.Storage
{
    class DelegateWriteStream : Stream
    {
        readonly Stream _innerStream;
        readonly Stream _buffer;
        long _pos;
        bool _disposed;

        public DelegateWriteStream(Stream innerStream)
        {
            _innerStream = innerStream;
            _buffer = new MemoryStream();
        }

        public event EventHandler Disposed;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _pos;

        public override long Position { get => _pos; set => throw new NotSupportedException(); }

        public override void Flush()
        {
            _buffer.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _buffer.Write(buffer, offset, count);
            _pos += count;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _buffer.Flush();
                    _buffer.Position = 0;
                    _buffer.CopyTo(_innerStream);
                    _buffer.Dispose();
                    _innerStream.Flush();

                    Disposed?.Invoke(this, EventArgs.Empty);
                    Disposed = null;
                }
            }
        }
    }
}