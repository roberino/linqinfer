using System;
using System.Diagnostics.Contracts;

namespace LinqInfer.Data.Remoting
{
    class UriRoute : IUriRoute
    {
        bool _bindToAnyHost;

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

        public bool BindToAnyHost { get { return _bindToAnyHost || BaseUri.Host == "0.0.0.0"; } set { _bindToAnyHost = value; } }

        public IUriRouteMapper Mapper { get; }

        public Uri BaseUri { get; }

        public string Template { get; }

        public Verb Verbs { get; }

        public override string ToString()
        {
            return string.Format("{0} {1}", Verbs, Template);
        }
    }
}
