using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public sealed class RoutingHandler
    {
        private readonly RoutingTable<IOwinContext> _routes;

        public RoutingHandler()
        {
            _routes = new RoutingTable<IOwinContext>();
        }

        public Func<IOwinContext, Task> CreateApplicationDelegate()
        {
            return async c =>
            {
                var handler = _routes.Map(c);

                if (handler != null)
                {
                    if (!await handler(c))
                    {
                        c.Cancel();
                    }
                }
                else
                {
                    if (GetMappings(c.RequestUri).Any())
                    {
                        c.Response.CreateStatusResponse(405);
                    }
                    else
                    {
                        c.Response.CreateStatusResponse(404);
                    }
                }
            };
        }

        public void AddRoute(IUriRoute route, Func<IOwinContext, Task> handler)
        {
            _routes.AddHandler(route, async (r, c) =>
            {
                foreach (var v in r)
                {
                    c["route." + v.Key] = v.Value;
                }

                await handler(c);

                return true;
            });
        }

        internal IEnumerable<IUriRoute> GetMappings(Uri requestUri)
        {
            return _routes.Mappings(requestUri);
        }
    }
}