using LinqInfer.Data;
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

        public static IApplicationBuilder UseTextServices(this IApplicationBuilder aspNetCoreBuilder, IVirtualFileStore fileStore, Uri baseUri = null)
        {
            aspNetCoreBuilder.CreateHttpApi(baseUri ?? new Uri("http://0.0.0.0/")).UseTextServices(fileStore);

            return aspNetCoreBuilder;
        }

        public static IHttpApiBuilder UseTextServices(this IHttpApiBuilder apiBuilder, DirectoryInfo dataDir = null, ICache cache = null)
        {
            new TextServices(dataDir, cache).Register(apiBuilder);

            return apiBuilder;
        }

        public static IHttpApiBuilder UseTextServices(this IHttpApiBuilder apiBuilder, IVirtualFileStore storage, ICache cache = null)
        {
            new TextServices(storage, cache).Register(apiBuilder);

            return apiBuilder;
        }
    }
}