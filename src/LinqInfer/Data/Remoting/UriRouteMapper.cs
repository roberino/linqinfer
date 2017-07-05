using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Data.Remoting
{
    internal class UriRouteMapper : IUriRouteMapper
    {
        private readonly UriRoute _route;
        private readonly IList<RoutePart> _parts;
        private readonly Func<IOwinContext, bool> _filter;

        public UriRouteMapper(UriRoute route, Func<IOwinContext, bool> filter = null)
        {
            Contract.Assert(route != null);

            _filter = filter ?? (_ => true);
            _route = route;
            _parts = Parse(route.Template).ToList();
        }

        public UriRoute Route
        {
            get
            {
                return _route;
            }
        }

        public bool IsTarget(IOwinContext context)
        {
            return _filter(context);
        }

        public bool IsMatch(IOwinContext context)
        {
            return CanMap(context.RequestUri, context.Request.Header.Verb);
        }

        public bool CanMap(Uri uri, Verb verb = Verb.Default)
        {
            IDictionary<string, string> p;
            return TryMap(uri, verb, out p);
        }

        public bool TryMap(Uri uri, Verb verb, out IDictionary<string, string> parameters)
        {
            parameters = null;

            if (!_route.BindToAnyHost && !(string.Equals(_route.BaseUri.Host, uri.Host) && _route.BaseUri.Port == uri.Port)) return false;

            if (!_route.Verbs.HasFlag(verb) && verb != Verb.All) return false;

            try
            {
                parameters = Parse(uri);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        internal IDictionary<string, string> Parse(Uri uri)
        {
            int i = 0;

            var parameters = _parts.Where(p => p.Type != RoutePartType.Static).GroupBy(p => p.Name).ToDictionary(p => p.Key, p => string.Empty);

            if (uri.PathAndQuery == "/") // Root
            {
                if (_parts.Count > 0)
                {
                    throw new ArgumentException();
                }
                return parameters;
            }

            var uriBuilder = new UriBuilder(uri);

            var path = uriBuilder.Path.Split('/');

            foreach (var part in path.Skip(1))
            {
                if (i < _parts.Count)
                {
                    var partType = _parts[i];

                    switch (partType.Type)
                    {
                        case RoutePartType.Static:
                            if (partType.Name != part) throw new ArgumentException(part);
                            break;
                        case RoutePartType.PathParameter:
                            parameters[partType.Name] = part;
                            break;
                        case RoutePartType.WildCard:
                            parameters[part] = part;
                            break;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(part)) break;

                    throw new ArgumentException(part);
                }

                i++;
            }

            if (uriBuilder.Query.Length > 1)
            {
                var query = uriBuilder.Query.Substring(1).Split('&');

                foreach (var item in query)
                {
                    var kv = item.Split('=');
                    var key = kv.First();

                    if (_parts.Any(p => p.Type == RoutePartType.QueryParameter && string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase)))
                    {
                        parameters[key] = kv.Length > 1 ? kv[1] : string.Empty;
                    }

                    parameters["query." + key] = kv.Length > 1 ? kv[1] : string.Empty;
                }
            }

            if (parameters.Any(p => !p.Key.StartsWith("query.") && string.IsNullOrEmpty(p.Value)))
            {
                throw new ArgumentException(uri.PathAndQuery);
            }

            return parameters;
        }

        private IEnumerable<RoutePart> Parse(string template)
        {
            if (string.IsNullOrEmpty(template)) throw new ArgumentException("Invalid route template - empty");

            int i = 0;

            var pathQuery = template.Split('?');

            if (pathQuery.Length > 2) throw new ArgumentException("Malformed query template");

            var paths = pathQuery.First().Split('/');

            foreach (var part in paths)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    if (part.StartsWith("{") && part.EndsWith("}"))
                    {
                        yield return new RoutePart()
                        {
                            Index = i,
                            Name = part.Substring(1, part.Length - 2),
                            Type = RoutePartType.PathParameter
                        };
                    }
                    else
                    {
                        yield return new RoutePart()
                        {
                            Index = i,
                            Name = part,
                            Type = part == "*" ? RoutePartType.WildCard : RoutePartType.Static
                        };
                    }

                    i++;
                }
            }

            if (pathQuery.Length == 2)
            {
                foreach (var queryParam in pathQuery.Last().Split('&'))
                {
                    yield return new RoutePart()
                    {
                        Index = i,
                        Name = queryParam.Split('=').First(),
                        Type = RoutePartType.QueryParameter
                    };

                    i++;
                }
            }
        }

        public class RoutePart
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public RoutePartType Type { get; set; }
        }

        public enum RoutePartType
        {
            Unknown,
            PathParameter,
            QueryParameter,
            Static,
            WildCard
        }
    }
}
