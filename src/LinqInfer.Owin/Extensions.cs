using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using System;

namespace LinqInfer.Owin
{
    public static class Extensions
    {
        public static IOwinApplication CreateOwinApplication(this Uri baseEndpoint)
        {
            ValidateHttpUri(baseEndpoint);

            return new OwinApplicationHost(baseEndpoint);
        }

        public static IHttpApi CreateHttpApi(this Uri baseEndpoint, IObjectSerialiser serialiser = null)
        {
            ValidateHttpUri(baseEndpoint);

            var hostApp = CreateOwinApplication(baseEndpoint);

            return new HttpApi(serialiser ?? new JsonObjectSerialiser(), hostApp);
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