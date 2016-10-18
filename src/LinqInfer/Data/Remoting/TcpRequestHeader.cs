﻿using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LinqInfer.Data.Remoting
{
    public sealed class TcpRequestHeader
    {
        private static readonly Regex _httpHeaderTest;

        static TcpRequestHeader()
        {
            _httpHeaderTest = new Regex(@"(GET|POST|PUT|OPTIONS|DELETE)\s+([^\s]+)\s+HTTP\/(1.\d)", RegexOptions.Singleline | RegexOptions.Compiled);
        }

        public TcpRequestHeader(byte[] data)
        {
            Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            TransportProtocol = TransportProtocol.Tcp;
            Path = "/";

            try
            {
                var ascii = Encoding.ASCII.GetString(data);

                var http = _httpHeaderTest.Matches(ascii).Cast<Match>().ToList();

                if (http.Any())
                {
                    //DebugOutput.Log(ascii);

                    ReadHttpHeaders(ascii);

                    HttpVerb = http.First().Groups[1].Value;
                    Path = http.First().Groups[2].Value;
                    HttpProtocol = http.First().Groups[3].Value;
                    HeaderLength = ascii.IndexOf("\n\n") + 2;
                }
                else
                {
                    ContentLength = BitConverter.ToInt64(data, 0);
                    HeaderLength = sizeof(long);
                }
            }
            catch
            {
                ContentLength = BitConverter.ToInt64(data, 0);
                HeaderLength = sizeof(long);
            }
        }

        public int HeaderLength { get; private set; }

        public Verb Verb
        {
            get
            {
                switch (HttpVerb)
                {
                    case "GET":
                        return Verb.Get;
                    case "PUT":
                        return Verb.Create;
                    case "POST":
                        return Verb.Update;
                    case "DELETE":
                        return Verb.Delete;
                    default:
                        return Verb.Default;
                }
            }
        }

        public string HttpVerb { get; private set; }

        public string HttpProtocol { get; private set; }

        public string Path { get; internal set; }

        public TransportProtocol TransportProtocol { get; private set; }

        public IDictionary<string, string[]> Headers { get; private set; }

        public long ContentLength { get; private set; }

        internal void WriteTo(Stream output)
        {
            if (TransportProtocol == TransportProtocol.Http)
            {
                using (var writer = new StreamWriter(output, Encoding.ASCII, 1024, true))
                {
                    using (var formatter = new HttpHeaderFormatter(writer))
                    {
                        formatter.WriteRequestAndProtocol(HttpVerb, Path, HttpProtocol);
                        formatter.WriteHeaders(Headers);
                        formatter.WriteEnd();
                    }
                }
            }
            else
            {
                new BinaryWriter(output, Encoding.UTF8, true).Write(ContentLength);
            }
        }

        private void ReadHttpHeaders(string header)
        {
            string line = null;
            var firstLineRead = false;

            using (var reader = new StringReader(header))
            {
                while (true)
                {
                    line = reader.ReadLine();

                    if (string.IsNullOrEmpty(line)) break;

                    if (!firstLineRead)
                    {
                        firstLineRead = true;
                    }
                    else
                    {
                        var split = line.IndexOf(':');
                        var name = line.Substring(0, split);
                        Headers[name] = SplitHeader(name, line.Substring(split + 1).Trim());
                    }
                }
            }

            string[] contentLen;

            if (Headers.TryGetValue("Content-Length", out contentLen) && contentLen.Length > 0)
            {
                ContentLength = long.Parse(contentLen[0]);
            }

            TransportProtocol = TransportProtocol.Http;
        }

        private string[] SplitHeader(string name, string values)
        {
            if (values == null) return new string[0];

            if (name.StartsWith("Accept", StringComparison.CurrentCultureIgnoreCase))
            {
                return values.Split(',');
            }
            if (name.Equals("Cookie", StringComparison.CurrentCultureIgnoreCase))
            {
                return values.Split(';');
            }

            return new[] { values };
        }
    }
}