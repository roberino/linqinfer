using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class HttpApplicationHost : HttpServer
    {
        private readonly IList<Middleware> _handlers;

        public HttpApplicationHost(string serverId = null, int port = DefaultPort, string host = "127.0.0.1")
            : base(serverId, port, host)
        {
            _handlers = new List<Middleware>();
        }

        public void AddComponent(Func<IOwinContext, Task> handler)
        {
            Contract.Ensures(handler != null);

            _handlers.Add(new Middleware()
            {
                Handler = c => handler(((IOwinContext)c)),
                Stage = PipelineStage.PreHandlerExecute
            });
        }

        public void AddComponent(Func<IDictionary<string, object>, Task> handler, PipelineStage stage)
        {
            Contract.Ensures(handler != null);

            _handlers.Add(new Middleware()
            {
                Handler = handler,
                Stage = stage
            });
        }

        protected override async Task<bool> Process(IOwinContext context)
        {
            foreach (var handler in _handlers.OrderBy(s => s.Stage))
            {
                try
                {
                    await handler.Handler(context);

                    if ((bool)context["owin.CallCancelled"])
                    {
                        DebugOutput.Log("Request aborted");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    DebugOutput.Log(ex);
                    return false;
                }
            }

            return true;
        }

        public enum PipelineStage
        {
            Authenticate = 0,
            Authorize = 1,
            PreHandlerExecute = 2,
            PostHandlerExecute = 3
        }

        private class Middleware
        {
            public PipelineStage Stage { get; set; }
            public Func<IDictionary<string, object>, Task> Handler { get; set; }
        }
    }
}