using System;
using System.Diagnostics.Contracts;

namespace LinqInfer.Data.Remoting
{
    internal class UriRoute : IUriRoute
    {
        public UriRoute(Uri baseUri, string template = null, Verb verbs = Verb.All, Func<IOwinContext, bool> filter = null)
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

            Mapper = new UriRouteMapper(this, filter);
        }

        public IUriRouteMapper Mapper { get; private set; }

        public Uri BaseUri { get; private set; }

        public string Template { get; private set; }

        public Verb Verbs { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", Verbs, Template);
        }
    }
}
