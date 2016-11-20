using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public sealed class TcpResponse : ICloneableObject<TcpResponse>, IDisposable
    {
        private readonly Stream _innerStream;
        private readonly Stream _compressionStream;
        private readonly TcpResponseHeader _header;
        private readonly ICompressionProvider _compression;

        private TextWriter _text;

        internal TcpResponse(TcpRequestHeader requestHeader, ICompressionProvider compression)
        {
            _compression = compression;
            _innerStream = new MemoryStream();
            _compressionStream = compression.CompressTo(_innerStream);

            _header = new TcpResponseHeader(() => _innerStream.Length)
            {
                TransportProtocol = requestHeader.TransportProtocol,
                HttpProtocol = requestHeader.HttpProtocol
            };

            if (compression.Name != null)
            {
                _header.Headers["Content-Encoding"] = new[] { compression.Name };
                _header.Headers["Vary"] = new[] { "Accept-Encoding" };
            }
        }

        internal TcpResponse(TransportProtocol transportProtocol, Stream responseStream = null)
        {
            _compressionStream = responseStream ?? new MemoryStream();
            _innerStream = _compressionStream;

            _header = new TcpResponseHeader(() => _innerStream.Length)
            {
                 TransportProtocol = transportProtocol
            };
        }

        public TcpResponseHeader Header { get { return _header; } }

        public Stream Content { get { return _compressionStream; } }

        public bool HasContent
        {
            get
            {
                return Content.Position > 0;
            }
        }

        public void CreateStatusResponse(int status)
        {
            Header.StatusCode = status;
        }

        public TextWriter CreateTextResponse(Encoding encoding = null)
        {
            if (_text == null || (encoding != null && _text.Encoding != encoding))
            {
                if (_text != null)
                {
                    _text.Flush();
                }

                Header.TextEncoding = encoding ?? (Header.TextEncoding ?? new UTF8Encoding(true));
                Header.ContentMimeType = "text/plain";

                _text = new StreamWriter(Content, Header.TextEncoding, 1024, true);
            }

            return _text;
        }

        internal async Task WriteTo(Stream output)
        {
            var header = _header.GetBytes();

            output.Write(header, 0, header.Length);

            await GetSendStream().CopyToAsync(output);
        }

        internal Stream GetSendStream()
        {
            Flush();

            if (!ReferenceEquals(_innerStream, _compressionStream)) _compressionStream.Dispose();

            _innerStream.Position = 0;
            return _innerStream;
        }

        internal void Flush()
        {
            if (_text != null) _text.Flush();
            _compressionStream.Flush();
            _innerStream.Flush();
        }

        public TcpResponse Clone(bool deep)
        {
            if (deep)
            {
                if (_compression != null && !(_compression is PassThruCompressionProvider) && !ReferenceEquals(_compressionStream, _innerStream))
                {
                    throw new NotSupportedException("Not supported for compressed responses");
                }

                var res = new TcpResponse(Header.TransportProtocol);

                if (Content.Position > 0) Content.CopyTo(res.Content);

                foreach(var h in Header.Headers)
                {
                    res.Header.Headers[h.Key] = h.Value;
                }

                res.Header.HttpProtocol = Header.HttpProtocol;
                res.Header.IsError = Header.IsError;
                res.Header.StatusCode = Header.StatusCode;
                res.Header.StatusText = Header.StatusText;
                res.Header.TextEncoding = Header.TextEncoding;

                return res;
            }
            else
            {
                return this;
            }
        }

        public void Dispose()
        {
            if (!ReferenceEquals(_innerStream, _compressionStream)) _compressionStream.Dispose();
            _innerStream.Dispose();
        }
    }
}