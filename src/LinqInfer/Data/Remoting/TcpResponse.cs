﻿using System;
using System.IO;
using System.Text;

namespace LinqInfer.Data.Remoting
{
    public class TcpResponse : IDisposable
    {
        private readonly Stream _innerStream;
        private readonly Stream _responseStream;
        private readonly TcpResponseHeader _header;

        private TextWriter _text;

        internal TcpResponse(ICompressionProvider compression)
        {
            _innerStream = new MemoryStream();
            _responseStream = compression.CompressTo(_innerStream);

            _header = new TcpResponseHeader(() => _innerStream.Length);

            if (compression.Name != null) _header.Headers["Content-Encoding"] = compression.Name;
        }

        internal TcpResponse(Stream responseStream = null)
        {
            _responseStream = responseStream ?? new MemoryStream();
            _innerStream = _responseStream;

            _header = new TcpResponseHeader(() => _innerStream.Length);
        }

        public TcpResponseHeader Header { get { return _header; } }

        public Stream Content { get { return _responseStream; } }

        public TextWriter CreateTextResponse(Encoding encoding = null)
        {
            Header.TextEncoding = encoding ?? Encoding.UTF8;
            Header.MimeType = "text/plain";

            _text = new StreamWriter(Content, Header.TextEncoding, 1024, true);

            return _text;
        }

        internal Stream GetSendStream()
        {
            Flush();
            _responseStream.Dispose();
            _innerStream.Position = 0;
            return _innerStream;
        }

        internal void Flush()
        {
            if (_text != null) _text.Flush();
            _responseStream.Flush();
            _innerStream.Flush();
        }

        public void Dispose()
        {
            _responseStream.Dispose();
            _innerStream.Dispose();
        }
    }
}