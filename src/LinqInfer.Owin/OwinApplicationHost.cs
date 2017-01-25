using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Owin
{
    internal class OwinApplicationHost : IOwinApplication
    {
        private readonly Uri _baseEndpoint;
        private readonly List<Middleware> _handlers;
        private readonly List<Func<IOwinContext, Exception, Task<bool>>> _errorHandlers;
        private readonly OwinContextConverter _contextConverter;
        private readonly Func<Uri, Action<IAppBuilder>, IDisposable> _onStartup;

        private ServerStatus _status;
        private IDisposable _server;

        public OwinApplicationHost(Uri baseEndpoint, Func<Uri, Action<IAppBuilder>, IDisposable> onStartup)
        {
            _baseEndpoint = baseEndpoint;
            _onStartup = onStartup;
            _status = ServerStatus.Stopped;
            _handlers = new List<Middleware>();
            _errorHandlers = new List<Func<IOwinContext, Exception, Task<bool>>>();
            _contextConverter = new OwinContextConverter();
        }

        public Uri BaseEndpoint
        {
            get
            {
                return _baseEndpoint;
            }
        }

        public ServerStatus Status
        {
            get
            {
                return _status;
            }
        }

        public event EventHandler<EventArgsOf<ServerStatus>> StatusChanged;

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

        public void Dispose()
        {
            if (_status != ServerStatus.Disposed)
            {
                try
                {
                    if (_server != null) _server.Dispose();
                }
                finally
                {
                    _status = ServerStatus.Disposed;
                }

                ChangeStatus(ServerStatus.Disposed);
            }
        }

        public async Task ProcessContext(IOwinContext context)
        {
            foreach (var handler in _handlers.OrderBy(s => s.Stage))
            {
                try
                {
                    await handler.Handler(context);

                    var cancelObj = context["owin.CallCancelled"];

                    if (cancelObj is bool && ((bool)cancelObj))
                    {
                        // DebugOutput.Log("Request aborted");
                        break;
                    }
                    if (cancelObj is CancellationToken && ((CancellationToken)cancelObj).IsCancellationRequested)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // DebugOutput.Log(ex);

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
        }

        public void Start()
        {
            if (_server != null || _status == ServerStatus.Disposed)
            {
                throw new InvalidOperationException();
            }

            _status = ServerStatus.Connecting;

            _server = _onStartup(_baseEndpoint, RegisterMiddleware);

            ChangeStatus(ServerStatus.Running);
        }

        public void Stop(bool wait = false)
        {
            try
            {
                _server.Dispose();
            }
            finally
            {
                _server = null;
                _status = ServerStatus.Stopped;
            }

            ChangeStatus(ServerStatus.Stopped);
        }

        private ServerStatus ChangeStatus(ServerStatus status)
        {
            _status = status;

            var ev = StatusChanged;

            if (ev != null)
            {
                ev.Invoke(this, new EventArgsOf<ServerStatus>(status));
            }

            return status;
        }

        internal void RegisterMiddleware(IAppBuilder builder)
        {
            builder.Run(c =>
            {
                var context = _contextConverter.Convert(c);

                return ProcessContext(context);
            });
        }

        protected class Middleware
        {
            public OwinPipelineStage Stage { get; set; }
            public Func<IOwinContext, Task> Handler { get; set; }
        }
    }
}