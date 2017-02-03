using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using Owin;
using System;

namespace LinqInfer.Owin
{
    internal class OwinApiMiddleware : HttpApiBuilder, IOwinApiBuilder
    {
        public OwinApiMiddleware(IObjectSerialiser serialiser, Uri baseEndpoint) : base(serialiser, new OwinApplicationHost(baseEndpoint, null, false), baseEndpoint)
        {
        }

        public IAppBuilder RegisterMiddleware(IAppBuilder appBuilder)
        {
            ((OwinApplicationHost)_host).RegisterMiddleware(appBuilder);
            return appBuilder;
        }
    }
}