using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.AspNetCore
{
    public static class Extensions
    {
        public static IOwinApplication CreateAspNetApplication(this Uri baseEndpoint, bool blockOnStart = true, bool bufferResponse = false)
        {
            ValidateHttpUri(baseEndpoint);

            return new AspNetApplicationHost(baseEndpoint, (u, a) =>
            {
                var builder = new WebHostBuilder()
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .Configure(a);

		if(u.Host == "0.0.0.0") builder.UseUrls("http://*:" + u.Port);
		else builder.UseUrls(u.ToString());

                var host = builder.Build();

                if (blockOnStart)
                {
                    host.Run();
                }
                else
                {
                    host.Start();
                }

                return host;
            }, bufferResponse);
        }

        public static IApplicationBuilder Run(this IApplicationBuilder appBuilder, IOwinApiBuilder apiBuilder)
        {
            return apiBuilder.RegisterMiddleware(appBuilder);
        }

        public static IHttpApiBuilder CreateHttpApi(this IApplicationBuilder appBuilder, Uri baseEndpoint, IObjectSerialiser serialiser = null)
        {
            ValidateHttpUri(baseEndpoint);

            var hostApp = new AspNetApplicationHost(baseEndpoint, null, false);

            hostApp.RegisterMiddleware(appBuilder);
                        
            return new HttpApiBuilder(serialiser ?? new JsonObjectSerialiser(), hostApp, baseEndpoint);
        }

        public static IHttpApi CreateHttpApi(this Uri baseEndpoint, IObjectSerialiser serialiser = null)
        {
            ValidateHttpUri(baseEndpoint);

            var hostApp = CreateAspNetApplication(baseEndpoint);

            return new HttpApi(serialiser ?? new JsonObjectSerialiser(), hostApp);
        }

        public static IOwinApiBuilder CreateHttpApiBuilder(this Uri baseEndpoint, IObjectSerialiser serialiser = null)
        {
            ValidateHttpUri(baseEndpoint);
            
            return new AspNetCoreApiMiddleware(serialiser ?? new JsonObjectSerialiser(), baseEndpoint);
        }

        public static T AllowOrigin<T>(this T application, Uri origin, bool setDefaultAccessControlIfMissing = true) where T : IOwinApplication
        {
            application.AddComponent(c =>
            {
                const string originHeader = "Access-Control-Allow-Origin";

                var originUri = origin.Scheme + "//" + origin.Host + ':' + origin.Port;
                var headers = c.Response.Header.Headers;

                if (setDefaultAccessControlIfMissing && !headers.Any(h => h.Key.StartsWith("Access-Control-Allow")))
                {
                    headers["Access-Control-Allow-Credentials"] = new[] { "true" };
                    headers["Access-Control-Allow-Methods"] = new[] { "GET, POST, PUT, DELETE, OPTIONS" };
                    headers["Access-Control-Allow-Headers"] = new[] { "Content-Type, X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5,  Date, X-Api-Version, X-File-Name" };
                }

                if (headers.ContainsKey(originHeader))
                {
                    headers[originHeader] = headers[originHeader].Concat(new[] { originUri }).ToArray();
                }
                else
                {
                    headers[originHeader] = new[] { originUri };
                }

                return Task.FromResult(true);
            }, OwinPipelineStage.Authenticate);

            return application;
        }

        private static void ValidateHttpUri(Uri uri)
        {
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                throw new ArgumentException("Invalid scheme: " + uri.Scheme);
            }
        }
    }
}
