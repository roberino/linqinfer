using System;
using System.Collections.Generic;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    internal class RoutingTable
    {
        private readonly IList<Route> _routes;
        private readonly Func<DataBatch, Stream, bool> _defaultRoute;

        public RoutingTable(Func<DataBatch, Stream, bool> defaultRoute = null)
        {
            _routes = new List<Route>();
            _defaultRoute = defaultRoute;
        }

        public Func<DataBatch, Stream, bool> Map(Uri uri, Verb verb = Verb.Default)
        {
            foreach (var route in _routes)
            {
                IDictionary<string, string> parameters;

                if (route.Template.TryMap(uri, verb, out parameters))
                {
                    return (d, s) =>
                    {
                        foreach (var parameter in parameters)
                        {
                            d.Properties[parameter.Key.ToLower()] = parameter.Value;
                        }

                        return route.Handler(d, s);
                    };
                }
            }

            if (_defaultRoute != null) return _defaultRoute;

            throw new ArgumentException("Route not found: " + uri.PathAndQuery + " " + verb.ToString());
        }

        public void AddHandler(UriRoute route, Func<DataBatch, Stream, bool> handler)
        {
            _routes.Add(new Route()
            {
                Handler = handler,
                Template = new UriRoutingTemplate(route)
            });
        }

        private class Route
        {
            public UriRoutingTemplate Template { get; set; }

            public Func<DataBatch, Stream, bool> Handler { get; set; }
        }
    }
}