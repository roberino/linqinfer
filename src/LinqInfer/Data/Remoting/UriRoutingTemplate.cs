using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Data.Remoting
{
    internal class UriRoutingTemplate
    {
        private readonly UriRoute _route;
        private readonly IList<RoutePart> _parts;

        public UriRoutingTemplate(UriRoute route)
        {
            Contract.Assert(route != null);

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

        public bool IsMatch(Uri uri, Verb verb = Verb.Default)
        {
            IDictionary<string, string> p;
            return TryMap(uri, verb, out p);
        }

        public bool TryMap(Uri uri, Verb verb, out IDictionary<string, string> parameters)
        {
            parameters = null;

            if (!(string.Equals(_route.BaseUri.Host, uri.Host) && _route.BaseUri.Port == uri.Port)) return false;

            if (!_route.Verbs.HasFlag(verb)) return false;

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

        public IDictionary<string, string> Parse(Uri uri)
        {
            int i = 0;

            var parameters = _parts.ToDictionary(p => p.Name, p => string.Empty);

            if (uri.PathAndQuery == "/" && parameters.Count == 0) return parameters; // Root

            foreach (var part in uri.PathAndQuery.Split('/').Skip(1))
            {
                if (i < _parts.Count)
                {
                    var partType = _parts[i];

                    if (partType.Type == RoutePartType.Static && partType.Name != part)
                    {
                        throw new ArgumentException(part);
                    }

                    parameters[partType.Name] = part;
                }
                else
                {
                    throw new ArgumentException(part);
                }

                i++;
            }

            if (parameters.Any(p => string.IsNullOrEmpty(p.Value)))
            {
                throw new ArgumentException(uri.PathAndQuery);
            }

            return parameters;
        }

        private IEnumerable<RoutePart> Parse(string template)
        {
            int i = 0;

            if (template.Contains("?"))
            {
                throw new NotSupportedException("Query strings not supported");
            }

            foreach(var part in template.Split('/'))
            {
                if (!string.IsNullOrEmpty(part))
                {
                    if (part.StartsWith("{") && part.EndsWith("}"))
                    {
                        yield return new RoutePart()
                        {
                            Index = i,
                            Name = part.Substring(1, part.Length - 2),
                            Type = RoutePartType.Parameter
                        };
                    }
                    else
                    {
                        yield return new RoutePart()
                        {
                            Index = i,
                            Name = part,
                            Type = RoutePartType.Static
                        };
                    }

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
            Parameter,
            Static
        }
    }
}
