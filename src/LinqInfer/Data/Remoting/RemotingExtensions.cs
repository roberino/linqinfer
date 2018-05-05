using System;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Data.Remoting
{
    public static class RemotingExtensions
    {
        /// <summary>
        /// Creates a URI route which can be used to map URIs to handlers
        /// </summary>
        /// <param name="endpoint">The base endpoint (URL)</param>
        /// <param name="routeTemplate">A template specifying the format of the URI (e.g. /my-path/{param1})</param>
        /// <param name="verbs">The acceptable verbs</param>
        /// <param name="predicate">An optional predicate which will filter out certain contexts</param>
        /// <returns>A <see cref="IUriRoute"/></returns>
        public static IUriRoute CreateRoute(this Uri endpoint, string routeTemplate, Verb verbs = Verb.All, Func<IOwinContext, bool> predicate = null, bool bindToAnyHost = false)
        {
            return new UriRoute(endpoint, routeTemplate, verbs, predicate)
            {
                BindToAnyHost = bindToAnyHost
            };
        }

        public static IHttpApi CreateHttpApi(this IOwinApplication app, IObjectSerialiser serialiser)
        {
            return new HttpApi(serialiser, app);
        }
    }
}