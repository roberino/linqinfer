using System;
using System.Collections.Generic;

namespace LinqInfer.Text.Http
{
    public sealed class HttpDocument : TokenisedTextDocument
    {
        internal HttpDocument(Uri url, IEnumerable<IToken> tokens, IEnumerable<RelativeLink> links, IDictionary<string, string[]> headers) : base(url.ToString(), tokens)
        {
            BaseUrl = url;
            Links = links;
            Headers = headers;
        }

        public Uri BaseUrl { get; private set; }

        public IDictionary<string, string[]> Headers { get; private set; }

        public IEnumerable<RelativeLink> Links { get; private set; }
    }
}