using System;
using System.Diagnostics.Contracts;

namespace LinqInfer.Data.Remoting
{
    public class UriRoute
    {
        internal UriRoute(Uri baseUri, string template = null, Verb verbs = Verb.All)
        {
            Contract.Assert(baseUri != null);
            BaseUri = baseUri;
            Template = template ?? baseUri.PathAndQuery;
            Verbs = verbs;

            if (template == null)
            {
                Template = baseUri.PathAndQuery;
            }
            else
            {
                if (template.StartsWith("/") || !baseUri.PathAndQuery.EndsWith("/"))
                {
                    Template = template;
                }
                else
                {
                    Template = baseUri.PathAndQuery + template;
                }
            }
        }

        public Uri BaseUri { get; private set; }

        public string Template { get; private set; }

        public Verb Verbs { get; private set; }
    }
}
