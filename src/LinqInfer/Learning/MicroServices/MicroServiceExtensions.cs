using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths.Geometry;
using LinqInfer.Maths.Graphs;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Learning.MicroServices
{
    public static class MicroServiceExtensions
    {
        /// <summary>
        /// Creates a classifier service which accepts an object (of type TInput) as the request input (POST)
        /// and returns a set of classification results.
        /// </summary>
        /// <typeparam name="TClass">The result class</typeparam>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="apiBuilder">An API application builder</param>
        /// <param name="classifier">A classifier</param>
        /// <param name="routePath">An optional route path (by default the path /classifiers/{name of classifier} will be used)</param>
        /// <returns></returns>
        public static IHttpApiBuilder CreateClassifierService<TClass, TInput>(this IHttpApiBuilder apiBuilder, IObjectClassifier<TClass, TInput> classifier, string routePath = null)
        {
            if (TypeExtensions.IsAnonymous<TInput>()) throw new NotSupportedException("Anonymous types not supported - " + typeof(TInput).Name);

            apiBuilder
                .Bind(routePath ?? ("/classifiers/" + GetName<TInput>()), Verb.Post)
                .To<TInput, IEnumerable<ClassifyResult<TClass>>>(x => Task.FromResult(classifier.Classify(x)));

            return apiBuilder;
        }

        public static IOwinApplication CreateGraphExportService(this IOwinApplication application, Func<IOwinContext, Rectangle, Task<WeightedGraph<string, double>>> graph, string routePath)
        {
            var route = new UriRoute(application.BaseEndpoint, routePath, Verb.Get);

            application.AddComponent(async c =>
            {
                if (route.Mapper.IsMatch(c))
                {
                    var pathParts = c.RequestUri.PathAndQuery.Split('?');
                    var w = 100;
                    var h = 100;

                    if (pathParts.Length > 1 && (pathParts[1].Contains("&width=") || pathParts[1].Contains("&height=")))
                    {
                        var query = pathParts[1]
                            .Split('&')
                            .Select(q => q.Split('='))
                            .Select(x => new { key = x[0], val = x.Length > 1 ? x[1] : null })
                            .GroupBy(x => x.key)
                            .ToDictionary(k => k.Key, v => v.Select(a => a.val).FirstOrDefault());

                        string strval;

                        if (query.TryGetValue("width", out strval)) int.TryParse(strval, out w);
                        if (query.TryGetValue("height", out strval)) int.TryParse(strval, out h);
                    }

                    var output = c.Response.CreateTextResponse(Encoding.UTF8, "text/xml");

                    var xml = await (await graph(c, new Rectangle()
                    {
                        Width = w,
                        Height = h
                    })).ExportAsGexfAsync();

                    xml.Save(output);
                }
            });

            return application;
        }

        public static IOwinApplication CreateNetworkTopologyExportService(this IOwinApplication application, IHasNetworkTopology network, string routePath)
        {
            return CreateGraphExportService(application, (c,d) =>
            {
                return network.ExportNetworkTopologyAsync(d.Width, d.Height);
            }, routePath);
        }

        private static string GetName<T>()
        {
            var typeName = typeof(T).Name.ToLower();

            return new string(typeof(T).Name.ToLower().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        }
    }
}