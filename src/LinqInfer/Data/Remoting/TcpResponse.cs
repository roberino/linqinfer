using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public class TcpResponse : ICloneableObject<TcpResponse>, IDisposable
    {
        private readonly Stream _outputStream;
        private readonly Stream _buffer;
        private readonly IResponseHeader _header;
        private readonly ICompressionProvider _compression;
        private readonly bool _isBuffered;

        private long _lastFlushPos;
        private TextWriter _text;

        internal TcpResponse(IRequestHeader requestHeader, ICompressionProvider compression)
        {
            _compression = compression;
            _outputStream = new MemoryStream();
            _buffer = compression.CompressTo(_outputStream);

            _header = new TcpResponseHeader(() => _outputStream.Length)
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

        internal TcpResponse(TransportProtocol transportProtocol, Stream responseStream = null, string httpProtocol = HttpHeaderFormatter.DefaultHttpProtocol)
        {
            _buffer = responseStream ?? new MemoryStream();
            _outputStream = _buffer;

            _header = new TcpResponseHeader(() => _outputStream.Length, null, httpProtocol)
            {
                 TransportProtocol = transportProtocol
            };
        }

        internal TcpResponse(IResponseHeader header, Stream responseStream, bool bufferOutput = false)
        {
            _outputStream = responseStream;
            _buffer = bufferOutput ? new MemoryStream() : _outputStream;
            _isBuffered = bufferOutput;
            _header = header;
        }

        public virtual IResponseHeader Header { get { return _header; } }

        public virtual Stream Content { get { return _buffer; } }

        public bool IsBuffered
        {
            get
            {
                return _isBuffered;
            }
        }

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

        public TextWriter CreateTextResponse(Encoding encoding = null, string mimeType = "text/plain")
        {
            if (_text == null || (encoding != null && _text.Encoding != encoding))
            {
                if (_text != null)
                {
                    _text.Flush();
                }

                Header.TextEncoding = encoding ?? (Header.TextEncoding ?? new UTF8Encoding(true));
                Header.ContentMimeType = mimeType;

                _text = new StreamWriter(Content, Header.TextEncoding, 1024, true);
            }

            return _text;
        }

        internal async Task WriteTo(Stream output)
        {
            var header = _header.GetBytes();

            output.Write(header, 0, header.Length);

            if (_isBuffered)
            {
                var pos = _buffer.Position;
                _buffer.Position = 0;
                await _buffer.CopyToAsync(output);
                _buffer.Position = pos;
            }
            else
            {
                await GetSendStream().CopyToAsync(output);
            }
        }

        internal Stream GetSendStream()
        {
            Flush();

            if (!ReferenceEquals(_outputStream, _buffer))
            {
                if (_isBuffered)
                {
                    _buffer.Position = 0;
                    return _buffer;
                }
                _buffer.Dispose();
            }

            _outputStream.Position = 0;
            return _outputStream;
        }

        public async Task FlushAsync()
        {
            if (_text != null) await _text.FlushAsync();
            await _buffer.FlushAsync();

            if (_isBuffered)
            {
                _buffer.Position = _lastFlushPos;
                await _buffer.CopyToAsync(_outputStream);
                _lastFlushPos = _buffer.Length;
            }

            await _outputStream.FlushAsync();
        }

        internal void Flush()
        {
            if (_text != null) _text.Flush();
            _buffer.Flush();

            if (_isBuffered)
            {
                if (_lastFlushPos > 0) _buffer.Position = _lastFlushPos;
                _buffer.CopyTo(_outputStream);
                _lastFlushPos = _buffer.Length;
            }

            _outputStream.Flush();
        }

        public TcpResponse Clone(bool deep)
        {
            if (deep)
            {
                if (_compression != null && !(_compression is PassThruCompressionProvider) && !ReferenceEquals(_buffer, _outputStream))
                {
                    throw new NotSupportedException("Not supported for compressed responses");
                }

                var res = new TcpResponse(Header.TransportProtocol, null, Header.HttpProtocol);

                if (Content.CanSeek)
                {
                    if (_text != null)
                    {
                        _text.Flush();
                    }

                    if (Content.Position > 0)
                    {
                        var pos = Content.Position;
                        Content.Position = 0;
                        try
                        {
                            Content.CopyTo(res.Content);
                        }
                        finally
                        {
                            Content.Position = pos;
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("Unsupported stream - seek not allowed");
                }

                foreach (var h in Header.Headers)
                {
                    res.Header.Headers[h.Key] = h.Value;
                }

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
            if (!ReferenceEquals(_outputStream, _buffer)) _buffer.Dispose();
            _outputStream.Dispose();
        }
    }
}