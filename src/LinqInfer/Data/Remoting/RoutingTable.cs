using LinqInfer.Utility;
using System;
using System.Collections.Generic;

namespace LinqInfer.Data.Remoting
{
    internal class RoutingTable
    {
        private readonly IList<Route> _routes;
        private readonly Func<DataBatch, TcpResponse, bool> _defaultRoute;

        public RoutingTable(Func<DataBatch, TcpResponse, bool> defaultRoute = null)
        {
            _routes = new List<Route>();
            _defaultRoute = defaultRoute;
        }

        public Func<DataBatch, TcpResponse, bool> Map(Uri uri, Verb verb = Verb.Default)
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

                        DebugOutput.Log("Using handler {0} {1} => {2}", route.Template.Route.Verbs, route.Template.Route.Template, route.Handler.Method.Name);

                        return route.Handler(d, s);
                    };
                }
            }

            if (_defaultRoute != null) return _defaultRoute;

            throw new ArgumentException("Route not found: " + uri.PathAndQuery + " " + verb.ToString());
        }

        public void AddHandler(UriRoute route, Func<DataBatch, TcpResponse, bool> handler)
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

            public Func<DataBatch, TcpResponse, bool> Handler { get; set; }
        }
    }
}