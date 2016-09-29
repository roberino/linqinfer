using System.IO;

namespace LinqInfer.Data.Remoting
{
    internal class PassThruCompressionProvider : ICompressionProvider
    {
        public string Name { get { return null; } }

        public Stream CompressTo(Stream input, bool closeStream = false)
        {
            return closeStream ? input : new PassThruStream(input);
        }

        public Stream DecompressFrom(Stream input)
        {
            return input;
        }

        private class PassThruStream : Stream
        {
            private readonly Stream _innerStream;

            public PassThruStream(Stream innerStream)
            {
                _innerStream = innerStream;
            }

            public override bool CanRead
            {
                get
                {
                    return _innerStream.CanRead;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return _innerStream.CanSeek;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return _innerStream.CanWrite;
                }
            }

            public override long Length
            {
                get
                {
                    return _innerStream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return _innerStream.Position;
                }

                set
                {
                    _innerStream.Position = value;
                }
            }

            public override void Flush()
            {
                _innerStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _innerStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _innerStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _innerStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _innerStream.Write(buffer, offset, count);
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }
        }
    }
}