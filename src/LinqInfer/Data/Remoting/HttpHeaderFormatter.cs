﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace LinqInfer.Data.Remoting
{
    class HttpHeaderFormatter : IDisposable
    {
        const string Ok = "200 OK";
        const string NotFound = "404 Not Found";
        const string Error = "500 Internal Server Error";
        const string HttpHead = "HTTP/{0} {1}";

        public const string DefaultHttpProtocol = "1.1";
        public const string ContentTypeHeaderName = "Content-Type";
        public const string ContentLengthHeaderName = "Content-Length";
        public const string AcceptHeaderName = "Accept";
        public const string CookieHeaderName = "Cookie";

        readonly TextWriter _writer;
        readonly bool _closeWriter;

        public HttpHeaderFormatter(TextWriter writer, bool closeWriter = false)
        {
            _writer = writer;
            _closeWriter = closeWriter;
        }

        public void WriteDate(DateTime? date = null)
        {
            _writer.WriteLine("Date: " + FormatDate(date.GetValueOrDefault(DateTime.UtcNow)));
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

        public void WriteRequestAndProtocol(string httpVerb, string path, string httpProtocol = "1.1")
        {
            _writer.WriteLine(string.Format("{0} {1} HTTP/{2}", httpVerb, path, httpProtocol));
        }

        public void WriteResponseProtocolAndStatus(string httpProtocol, int statusCode, string statusText = null)
        {
            _writer.WriteLine(string.Format(HttpHead, httpProtocol, GetStatus(statusCode, statusText)));
        }

        public static Verb ParseVerb(string httpVerb)
        {
            switch (httpVerb)
            {
                case "GET":
                    return Verb.Get;
                case "PUT":
                    return Verb.Put;
                case "POST":
                    return Verb.Post;
                case "DELETE":
                    return Verb.Delete;
                case "OPTIONS":
                    return Verb.Options;
                case "PATCH":
                    return Verb.Patch;
                default:
                    return Verb.Default;
            }
        }

        public static string TranslateVerb(Verb verb)
        {
            if (verb == Verb.Default) return "GET";

            return verb.ToString().ToUpper();
        }

        public void Dispose()
        {
            _writer.Flush();

            if (_closeWriter)
            {
                _writer.Dispose();
            }
        }

        string GetStatus(int statusCode, string statusText)
        {
            var status = (HttpStatusCode)statusCode;
            var text = string.IsNullOrEmpty(statusText) ? CamelCaseSplit(status.ToString()) : statusText;

            return string.Format("{0} {1}", statusCode, text);
        }

        static string CamelCaseSplit(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var result = new StringBuilder();

            foreach (var c in text)
            {
                if (char.IsUpper(c) && result.Length > 0)
                {
                    result.Append(' ');
                }

                result.Append(c);
            }

            return result.ToString();
        }

        string FormatDate(DateTime date)
        {
            return date.ToUniversalTime().ToString("r");
        }

        string ArrayToString(string headerName, string[] data)
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
