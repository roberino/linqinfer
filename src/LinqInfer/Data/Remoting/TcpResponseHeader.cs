using System;
using System.Collections.Generic;
using System.Text;

namespace LinqInfer.Data.Remoting
{
    public class TcpResponseHeader
    {
        private const string Ok = "200 OK";
        private const string Error = "500 Internal Server Error";
        private const string HttpHead = "HTTP/1.1";
        private const string ContentLengthHeader = "Content-Length";

        public readonly IDictionary<string, string[]> _headers;

        private Func<long> _contentLength;

        internal TcpResponseHeader(Func<long> contentLength, IDictionary<string, string[]> headers = null)
        {
            _contentLength = contentLength;
            _headers = headers ?? new Dictionary<string, string[]>();

            TransportProtocol = headers != null ? TransportProtocol.Http : TransportProtocol.Tcp;

            MimeType = "application/octet-stream";
        }

        public string MimeType { get; set; }

        public Encoding TextEncoding { get; set; }

        public IDictionary<string, string[]> Headers { get { return _headers; } }

        public bool IsError { get; set; }

        public int? StatusCode { get; set; }

        public TransportProtocol TransportProtocol { get; internal set; }

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
            var date = DateTime.UtcNow.ToUniversalTime().ToString("r");

            header.AppendLine(HttpHead + " " + (IsError ? Error : Ok));

            _headers[ContentLengthHeader] = new [] { _contentLength().ToString() };
            _headers["Date"] = new[] { date };

            if (MimeType != null)
            {
                var contentType = MimeType;

                if (TextEncoding != null)
                {
                    contentType += "; charset=" + TextEncoding.BodyName.ToLower();
                }

                _headers["Content-Type"] = new[] { contentType };
            }

            foreach (var headerKv in _headers)
            {
                header.AppendLine(headerKv.Key + ':' + ArrayToString(headerKv.Value));
            }

            header.AppendLine();

            return header.ToString();
        }

        private string ArrayToString(string[] data)
        {
            if (data == null) return null;

            return string.Join(", ", data);
            //return data.Aggregate(string.Empty, (s, v) => (s.Length > 0 ? s + ", " : s) + v).ToString();
        }
    }
}