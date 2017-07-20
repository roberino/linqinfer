using LinqInfer.Data.Remoting;
using Microsoft.AspNetCore.Builder;
using System;
using System.IO;

namespace LinqInfer.Microservices.Text
{
    public static class Extensions
    {
        public static IApplicationBuilder UseTextServices(this IApplicationBuilder aspNetCoreBuilder, DirectoryInfo dataDir = null, Uri baseUri = null)
        {
            aspNetCoreBuilder.CreateHttpApi(baseUri ?? new Uri("http://0.0.0.0/")).UseTextServices(dataDir);

            return aspNetCoreBuilder;
        }
        public static IHttpApiBuilder UseTextServices(this IHttpApiBuilder apiBuilder, DirectoryInfo dataDir = null)
        {
            new TextServices(dataDir).Register(apiBuilder);

            return apiBuilder;
        }
    }
}