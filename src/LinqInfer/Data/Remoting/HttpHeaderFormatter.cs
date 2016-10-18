using System;
using System.Collections.Generic;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    internal class HttpHeaderFormatter : IDisposable
    {
        private readonly TextWriter _writer;
        private readonly bool _closeWriter;

        public HttpHeaderFormatter(TextWriter writer, bool closeWriter = false)
        {
            _writer = writer;
            _closeWriter = closeWriter;
        }

        public void WriteDate()
        {
            _writer.WriteLine("Date: " + FormatDate(DateTime.UtcNow));
        }

        public void WriteHeaders(IDictionary<string, string[]> headers)
        {
            foreach (var headerKv in headers)
            {
                _writer.WriteLine(headerKv.Key + ": " + ArrayToString(headerKv.Key, headerKv.Value));
            }
        }

        public void WriteEnd()
        {
            _writer.WriteLine();
        }

        public void WriteRequestAndProtocol(string httpVerb, string path, string httpProtocol)
        {
            _writer.WriteLine(string.Format("{0} {1} HTTP {2}", httpVerb, path, httpProtocol));
        }

        public void Dispose()
        {
            _writer.Flush();

            if (_closeWriter)
            {
                _writer.Close();
                _writer.Dispose();
            }
        }

        private string FormatDate(DateTime date)
        {
            return date.ToUniversalTime().ToString("r");
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
