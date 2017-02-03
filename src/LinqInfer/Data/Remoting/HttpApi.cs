using System;

namespace LinqInfer.Data.Remoting
{
    internal class HttpApi : HttpApiBuilder, IHttpApi
    {
        private readonly IOwinApplication _serverHost;

        public HttpApi(IObjectSerialiser serialiser, int port, string host = "localhost")
            : this(serialiser, new HttpApplicationHost(null, port, host))
        {
        }

        public HttpApi(IObjectSerialiser serialiser, IOwinApplication host) : base(serialiser, host, host.BaseEndpoint)
        {
            _serverHost = host;
        }

        public event EventHandler<EventArgsOf<ServerStatus>> StatusChanged
        {
            add { _serverHost.StatusChanged += value; }
            remove { _serverHost.StatusChanged -= value; }
        }

        public Uri BaseEndpoint
        {
            get
            {
                return _serverHost.BaseEndpoint;
            }
        }

        public ServerStatus Status
        {
            get
            {
                return _serverHost.Status;
            }
        }

        public void Start()
        {
            _serverHost.Start();
        }

        public void Stop(bool wait = false)
        {
            _serverHost.Stop(wait);
        }

        public void Dispose()
        {
            _serverHost.Dispose();
        }
    }
}