using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.Http
{
    public sealed class HttpDocument : TokenisedTextDocument
    {
        internal HttpDocument(Uri url, IEnumerable<IToken> tokens, IEnumerable<RelativeLink> links = null, IDictionary<string, string[]> headers = null) : base(url.ToString(), tokens)
        {
            BaseUrl = url;
            Links = links ?? Enumerable.Empty<RelativeLink>();
            Headers = headers ?? new Dictionary<string, string[]>();
            Metadata = new Dictionary<string, string>();
        }

        public string Title { get; internal set; }

        public Uri BaseUrl { get; }

        public IDictionary<string, string> Metadata { get; }

        public IDictionary<string, string[]> Headers { get; }

        public IEnumerable<RelativeLink> Links { get; }

        public static HttpDocument CreateEmpty(Uri uri, IDictionary<string, string[]> headers = null)
        {
            return new HttpDocument(uri, Enumerable.Empty<IToken>(), Enumerable.Empty<RelativeLink>(), headers);
        }
    }
}