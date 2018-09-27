using System;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Data.Remoting
{
    class HttpApi : HttpApiBuilder, IHttpApi
    {
        readonly IOwinApplication _serverHost;

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