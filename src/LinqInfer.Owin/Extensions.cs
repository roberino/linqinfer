using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Owin
{
    public static class Extensions
    {
        public static IOwinApplication CreateOwinApplication(this Uri baseEndpoint, bool bufferResponse = false)
        {
            ValidateHttpUri(baseEndpoint);

            //return new OwinApplicationHost(baseEndpoint, (u, a) => WebApp.Start(new StartOptions(u.ToString())
            //{
            //    ServerFactory = "Microsoft.Owin.Host.HttpListener"
            //}, a), bufferResponse);

            return new OwinApplicationHost(baseEndpoint, (u, a) => WebApp.Start(u.ToString(), a), bufferResponse);
        }

        public static IAppBuilder Run(this IAppBuilder appBuilder, IOwinApiBuilder apiBuilder)
        {
            return apiBuilder.RegisterMiddleware(appBuilder);
        }

        public static IHttpApiBuilder CreateHttpApi(this IAppBuilder appBuilder, Uri baseEndpoint, IObjectSerialiser serialiser = null)
        {
            ValidateHttpUri(baseEndpoint);

            var hostApp = new OwinApplicationHost(baseEndpoint, null, false);

            hostApp.RegisterMiddleware(appBuilder);
                        
            return new HttpApiBuilder(serialiser ?? new JsonObjectSerialiser(), hostApp, baseEndpoint);
        }

        public static IHttpApi CreateHttpApi(this Uri baseEndpoint, IObjectSerialiser serialiser = null)
        {
            ValidateHttpUri(baseEndpoint);

            var hostApp = CreateOwinApplication(baseEndpoint);

            return new HttpApi(serialiser ?? new JsonObjectSerialiser(), hostApp);
        }

        public static IOwinApiBuilder CreateHttpApiBuilder(this Uri baseEndpoint, IObjectSerialiser serialiser = null)
        {
            ValidateHttpUri(baseEndpoint);
            
            return new OwinApiMiddleware(serialiser ?? new JsonObjectSerialiser(), baseEndpoint);
        }

        public static T AllowOrigin<T>(this T application, Uri origin, bool setDefaultAccessControlIfMissing = true) where T : IOwinApplication
        {
            application.AddComponent(c =>
            {
                const string originHeader = "Access-Control-Allow-Origin";

                var originUri = origin.Scheme + Uri.SchemeDelimiter + origin.Host + ':' + origin.Port;
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
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("Invalid scheme: " + uri.Scheme);
            }
        }
    }
}