using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace LinqInfer.Data.Remoting
{
    public class TcpResponseHeader
    {
        private const string Ok = "200 OK";
        private const string NotFound = "404 Not Found";
        private const string Error = "500 Internal Server Error";
        private const string HttpHead = "HTTP/1.1";
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
        }

        public string MimeType { get; set; }

        public Encoding TextEncoding { get; set; }

        public IDictionary<string, string[]> Headers { get { return _headers; } }

        public bool IsError { get; set; }

        public int? StatusCode { get; set; }

        public TransportProtocol TransportProtocol { get; internal set; }

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

        private string GetStatus()
        {
            if (StatusCode.HasValue)
            {
                var status = (HttpStatusCode)StatusCode.Value;

                switch (status)
                {
                    case HttpStatusCode.NotFound:
                        return NotFound;
                    case HttpStatusCode.InternalServerError:
                        return Error;
                    case HttpStatusCode.OK:
                        return Ok;
                    default:
                        return string.Format("{0} {1}", StatusCode.Value, status.ToString());
                }
            }

            return (IsError ? Error : Ok);
        }

        private string GetHttpHeader()
        {
            var header = new StringBuilder();
            var date = DateTime.UtcNow.ToUniversalTime().ToString("r");

            header.AppendLine(HttpHead + " " + GetStatus());

            _headers[ContentLengthHeader] = new[] { _contentLength().ToString() };
            _headers["Date"] = new[] { date };

            if (MimeType != null && !_headers.ContainsKey(ContentTypeHeader))
            {
                var contentType = MimeType;

                if (TextEncoding != null)
                {
                    contentType += "; charset=" + TextEncoding.BodyName.ToLower();
                }

                _headers[ContentTypeHeader] = new[] { contentType };
            }

            foreach (var headerKv in _headers)
            {
                header.AppendLine(headerKv.Key + ": " + ArrayToString(headerKv.Key, headerKv.Value));
            }

            header.AppendLine();

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