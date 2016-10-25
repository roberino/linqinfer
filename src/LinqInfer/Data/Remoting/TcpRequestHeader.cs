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
            ContentEncoding = new UTF8Encoding(true);

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

                    Query = ParseQuery(Path);
                }
                else
                {
                    ContentLength = BitConverter.ToInt64(data, 0);
                    HeaderLength = sizeof(long);
                    Query = new Dictionary<string, string[]>();
                }
            }
            catch
            {
                ContentLength = BitConverter.ToInt64(data, 0);
                HeaderLength = sizeof(long);
                Query = new Dictionary<string, string[]>();
            }
        }

        public int HeaderLength { get; private set; }

        public Verb Verb
        {
            get
            {
                return HttpHeaderFormatter.ParseVerb(HttpVerb);
            }
        }

        public IDictionary<string, string[]> Query { get; private set; }

        public string HttpVerb { get; private set; }

        public string HttpProtocol { get; private set; }

        public string Path { get; internal set; }

        public Encoding ContentEncoding { get; private set; }

        public string ContentMimeType { get; private set; }

        public string PreferredMimeType(string[] supportedMimeTypes)
        {
            string[] accept;

            if (Headers.TryGetValue("Accept", out accept))
            {
                var best = accept.FirstOrDefault(a => supportedMimeTypes.Contains(a, StringComparer.OrdinalIgnoreCase));

                if (best != null) return best;
            }

            return supportedMimeTypes.First();
        }

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

        private IDictionary<string, string[]> ParseQuery(string pathAndQuery)
        {
            var qi = pathAndQuery.IndexOf('?');

            if (qi > -1)
            {
                var query = Path.Substring(qi + 1).Split('&').Select(q =>
                {
                    var kv = q.Split('=');

                    return new
                    {
                        key = Uri.UnescapeDataString(kv.FirstOrDefault() ?? string.Empty),
                        value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty
                    };
                });

                return query.GroupBy(q => q.key).ToDictionary(g => g.Key, g => g.Select(v => v.value).ToArray());
            }
            else
            {
                return new Dictionary<string, string[]>();
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

            string[] content;

            if (Headers.TryGetValue(HttpHeaderFormatter.ContentLengthHeaderName, out content) && content.Length > 0)
            {
                ContentLength = long.Parse(content[0]);
            }

            if (Headers.TryGetValue(HttpHeaderFormatter.ContentTypeHeaderName, out content) && content.Length > 0)
            {
                var parts = content.First().Split(';');

                if (parts.Length > 0)
                {
                    ContentMimeType = parts[0];

                    if (parts.Length > 1)
                    {
                        try
                        {
                            ContentEncoding = Encoding.GetEncoding(parts[0]);
                        }
                        catch { }
                    }
                }
            }

            TransportProtocol = TransportProtocol.Http;
        }

        private string[] SplitHeader(string name, string values)
        {
            if (values == null) return new string[0];

            if (name.StartsWith(HttpHeaderFormatter.AcceptHeaderName, StringComparison.CurrentCultureIgnoreCase))
            {
                return values.Split(',');
            }
            if (name.Equals(HttpHeaderFormatter.CookieHeaderName, StringComparison.CurrentCultureIgnoreCase))
            {
                return values.Split(';');
            }

            return new[] { values };
        }
    }
}