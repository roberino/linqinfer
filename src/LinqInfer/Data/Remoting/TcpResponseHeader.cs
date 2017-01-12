using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqInfer.Data.Remoting
{
    public class TcpResponseHeader : IResponseHeader
    {
        internal const string ContentLengthHeader = "Content-Length";
        internal const string ContentTypeHeader = "Content-Type";

        private readonly IDictionary<string, string[]> _headers;

        private Func<long> _contentLength;
        private Encoding _encoding;
        private string _mimeType;

        internal TcpResponseHeader(Func<long> contentLength, IDictionary<string, string[]> headers = null, string httpProtocol = HttpHeaderFormatter.DefaultHttpProtocol)
        {
            _contentLength = contentLength;
            _headers = headers ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            TransportProtocol = headers != null ? TransportProtocol.Http : TransportProtocol.Tcp;

            ContentMimeType = "application/octet-stream";
            HttpProtocol = httpProtocol;
            Date = DateTime.UtcNow;
            _encoding = new UTF8Encoding(true);
        }

        public string ContentMimeType
        {
            get
            {
                string[] mt;
                if (_headers.TryGetValue(ContentTypeHeader, out mt) && mt.Length > 0)
                {
                    return mt.First().Split(';').First().Trim();
                }
                return _mimeType;
            }
            set
            {
                _mimeType = value;
            }
        }

        public DateTime Date { get; set; }

        public Encoding TextEncoding
        {
            get { return _encoding; }
            set
            {
                if (value == null) throw new NullReferenceException();
                _encoding = value;
            }
        }

        public IDictionary<string, string[]> Headers { get { return _headers; } }

        public bool IsError { get; set; }

        public int? StatusCode { get; set; }

        public string StatusText { get; set; }

        public TransportProtocol TransportProtocol { get; internal set; }

        public string HttpProtocol { get; internal set; }

        public void CopyFrom(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var header in headers)
            {
                _headers[header.Key] = header.Value.ToArray();
            }
        }

        public byte[] GetBytes()
        {
            if (TransportProtocol == TransportProtocol.Http)
            {
                return Encoding.ASCII.GetBytes(GetHttpHeader());
            }
            else
            {
                return BitConverter.GetBytes(_contentLength());
            }
        }

        private string GetHttpHeader()
        {
            var header = new StringBuilder();

            using (var formatter = new HttpHeaderFormatter(new StringWriter(header), true))
            {
                formatter.WriteResponseProtocolAndStatus(HttpProtocol, StatusCode.GetValueOrDefault(IsError ? 500 : 200), StatusText);

                formatter.WriteDate(Date);

                _headers[ContentLengthHeader] = new[] { _contentLength().ToString() };

                if (_mimeType != null && !_headers.ContainsKey(ContentTypeHeader))
                {
                    var contentType = ContentMimeType;

                    if (TextEncoding != null)
                    {
                        contentType += "; charset=" + TextEncoding.WebName;
                    }

                    _headers[ContentTypeHeader] = new[] { contentType };
                }

                formatter.WriteHeaders(_headers);
                formatter.WriteEnd();
            }

            return header.ToString();
        }

        private string ArrayToString(string headerName, string[] data)
        {
            if (data == null) return null;

            if (string.Equals(headerName, "Cookie", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(";", data);
            }

            return string.Join(",", data);
        }
    }
}