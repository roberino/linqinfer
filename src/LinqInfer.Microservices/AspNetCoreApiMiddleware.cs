﻿using LinqInfer.Data.Remoting;
using LinqInfer.Data.Serialisation;
using Microsoft.AspNetCore.Builder;
using System;

namespace LinqInfer.Microservices
{
    internal class AspNetCoreApiMiddleware : HttpApiBuilder, IOwinApiBuilder
    {
        public AspNetCoreApiMiddleware(IObjectSerialiser serialiser, Uri baseEndpoint) : base(serialiser, new AspNetApplicationHost(baseEndpoint, null, false), baseEndpoint)
        {
        }

        public IApplicationBuilder RegisterMiddleware(IApplicationBuilder appBuilder)
        {
            ((AspNetApplicationHost)_host).RegisterMiddleware(appBuilder);
            return appBuilder;
        }
    }
}