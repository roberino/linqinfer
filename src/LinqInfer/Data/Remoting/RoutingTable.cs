using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class RoutingTable<T>
    {
        private readonly IList<RouteHandlerPair> _routes;
        private readonly Func<IDictionary<string, string>, T, Task<bool>> _defaultRoute;

        public RoutingTable(Func<IDictionary<string, string>, T, Task<bool>> defaultRoute = null)
        {
            _routes = new List<RouteHandlerPair>();
            _defaultRoute = defaultRoute;
        }

        public Func<T, Task<bool>> Map(IOwinContext context)
        {
            return Map(_routes.Where(r => r.UriRoute.Mapper.IsTarget(context)), context.RequestUri, context.Request.Header.Verb, false);
        }

        public Func<T, Task<bool>> Map(Uri uri, Verb verb = Verb.Default)
        {
            return Map(_routes, uri, verb);
        }

        public void AddHandler(IUriRoute route, Func<IDictionary<string, string>, T, Task<bool>> handler)
        {
            _routes.Add(new RouteHandlerPair()
            {
                Handler = handler,
                UriRoute = route
            });
        }

        internal IEnumerable<IUriRoute> Mappings(Uri uri)
        {
            IDictionary<string, string> para;

            return _routes.Where(r => r.UriRoute.Mapper.TryMap(uri, Verb.All, out para)).Select(r => r.UriRoute);
        }

        private Func<T, Task<bool>> Map(IEnumerable<RouteHandlerPair> applicableRoutes, Uri uri, Verb verb = Verb.Default, bool throwIfMissing = true)
        {
            foreach (var route in applicableRoutes)
            {
                IDictionary<string, string> parameters;

                if (route.UriRoute.Mapper.TryMap(uri, verb, out parameters))
                {
                    return (c) =>
                    {
                        DebugOutput.Log("Using handler {0} {1} => {2}", route.UriRoute.Verbs, route.UriRoute.Template, route.Handler.Method.Name);

                        return route.Handler(parameters, c);
                    };
                }
            }

            if (_defaultRoute != null) return c => _defaultRoute(new Dictionary<string, string>(), c);

            if (!throwIfMissing) return null;

            throw new ArgumentException("Route not found: " + uri.PathAndQuery + " " + verb.ToString());
        }

        private class RouteHandlerPair
        {
            public IUriRoute UriRoute { get; set; }

            public Func<IDictionary<string, string>, T, Task<bool>> Handler { get; set; }
        }
    }
}