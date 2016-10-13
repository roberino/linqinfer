using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class RoutingTable<T>
    {
        private readonly IList<Route> _routes;
        private readonly Func<IDictionary<string, string>, T, Task<bool>> _defaultRoute;

        public RoutingTable(Func<IDictionary<string, string>, T, Task<bool>> defaultRoute = null)
        {
            _routes = new List<Route>();
            _defaultRoute = defaultRoute;
        }

        public Func<T, Task<bool>> Map(Uri uri, Verb verb = Verb.Default)
        {
            foreach (var route in _routes)
            {
                IDictionary<string, string> parameters;

                if (route.Template.TryMap(uri, verb, out parameters))
                {
                    return (c) =>
                    {
                        DebugOutput.Log("Using handler {0} {1} => {2}", route.Template.Route.Verbs, route.Template.Route.Template, route.Handler.Method.Name);

                        return route.Handler(parameters, c);
                    };
                }
            }

            if (_defaultRoute != null) return c => _defaultRoute(new Dictionary<string, string>(), c);

            throw new ArgumentException("Route not found: " + uri.PathAndQuery + " " + verb.ToString());
        }

        public void AddHandler(UriRoute route, Func<IDictionary<string, string>, T, Task<bool>> handler)
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

            public Func<IDictionary<string, string>, T, Task<bool>> Handler { get; set; }
        }
    }
}