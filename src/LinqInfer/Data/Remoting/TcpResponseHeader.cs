using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace LinqInfer.Data.Remoting
{
    public class TcpResponseHeader
    {
        private const string ContentLengthHeader = "Content-Length";
        private const string ContentTypeHeader = "Content-Type";

        private readonly IDictionary<string, string[]> _headers;

        private Func<long> _contentLength;

        internal TcpResponseHeader(Func<long> contentLength, IDictionary<string, string[]> headers = null)
        {
            _contentLength = contentLength;
            _headers = headers ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            TransportProtocol = headers != null ? TransportProtocol.Http : TransportProtocol.Tcp;

            MimeType = "application/octet-stream";
            HttpProtocol = "1.1";
            TextEncoding = Encoding.UTF8;
        }

        public string MimeType { get; set; }

        public Encoding TextEncoding { get; set; }

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

        internal byte[] GetBytes()
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

                formatter.WriteDate();

                _headers[ContentLengthHeader] = new[] { _contentLength().ToString() };

                if (MimeType != null && !_headers.ContainsKey(ContentTypeHeader))
                {
                    var contentType = MimeType;

                    if (TextEncoding != null)
                    {
                        contentType += "; charset=" + TextEncoding.BodyName.ToLower();
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