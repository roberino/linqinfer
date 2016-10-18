using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class HttpApplicationHost : HttpServer, IOwinApplication
    {
        private readonly IList<Middleware> _handlers;
        private readonly IList<Func<IOwinContext, Exception, Task<bool>>> _errorHandlers;

        public HttpApplicationHost(string serverId = null, int port = DefaultPort, string host = "127.0.0.1")
            : base(serverId, port, host)
        {
            _handlers = new List<Middleware>();
            _errorHandlers = new List<Func<IOwinContext, Exception, Task<bool>>>();

            CompressUsing(new PassThruCompressionProvider());
        }

        public void AddComponent(Func<IOwinContext, Task> handler, OwinPipelineStage stage = OwinPipelineStage.PreHandlerExecute)
        {
            Contract.Ensures(handler != null);

            _handlers.Add(new Middleware()
            {
                Handler = handler,
                Stage = stage
            });
        }

        public void AddComponent(Func<IDictionary<string, object>, Task> handler, OwinPipelineStage stage)
        {
            Contract.Ensures(handler != null);

            _handlers.Add(new Middleware()
            {
                Handler = handler,
                Stage = stage
            });
        }

        public void AddErrorHandler(Func<IOwinContext, Exception, Task<bool>> errorHandler)
        {
            _errorHandlers.Add(errorHandler);
        }

        protected override bool HandleTransportError(Exception ex)
        {
            DebugOutput.Log(ex.Message);
            return true;
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

                    foreach (var err in _errorHandlers)
                    {
                        var handled = await err(context, ex);

                        if (!handled)
                        {
                            context.Cancel();
                        }
                    }
                }
            }

            return false;
        }

        private class Middleware
        {
            public OwinPipelineStage Stage { get; set; }
            public Func<IOwinContext, Task> Handler { get; set; }
        }
    }
}