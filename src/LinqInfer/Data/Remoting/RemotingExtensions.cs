using LinqInfer.Learning.Features;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public static class RemotingExtensions
    {
        private static ConcurrentDictionary<string, IVectorTransferServer> _defaultServers;

        static RemotingExtensions()
        {
            _defaultServers = new ConcurrentDictionary<string, IVectorTransferServer>();

            Process.GetCurrentProcess().Exited += (s, e) =>
            {
                Shutdown();
            };
        }

        /// <summary>
        /// Sends extracted data to a server endpoint
        /// </summary>
        public static async Task SendTo<T>(this FeatureProcessingPipline<T> pipeline, Uri endpoint) where T : class
        {
            Util.ValidateTcpUri(endpoint);

            using (var client = new VectorTransferClient(null, endpoint.Port, endpoint.Host))
            {
                var handle = await client.BeginTransfer(endpoint.PathAndQuery);

                foreach (var batch in pipeline.ExtractBatches())
                {
                    await handle.Send(batch.Select(b => b.Vector));
                }

                await handle.End();
            }
        }

        public static IUriRoute CreateRoute(this Uri endpoint, string routeTemplate, Verb verbs = Verb.All)
        {
            return new UriRoute(endpoint, routeTemplate, verbs);
        }

        public static IOwinApplication CreateHttpApplication(this Uri endpoint)
        {
            Util.ValidateHttpUri(endpoint);

            return new HttpApplicationHost(null, endpoint.Port, endpoint.Host);
        }

        public static IHttpApi CreateHttpApi(this Uri endpoint, IObjectSerialiser serialiser)
        {
            Util.ValidateHttpUri(endpoint);

            return new HttpApi(serialiser, endpoint.Port, endpoint.Host);
        }

        public static IVectorTransferServer CreateRemoteService(this Uri endpoint, Func<DataBatch, Stream, bool> messageHandler, bool startService = true)
        {
            return CreateRemoteService(new UriRoute(endpoint), messageHandler, startService);
        }

        public static IVectorTransferServer CreateRemoteService(this IUriRoute route, Func<DataBatch, Stream, bool> messageHandler = null, bool startService = true)
        {
            var endpoint = route.BaseUri;

            Util.ValidateTcpUri(endpoint);

            var key = endpoint.Host + ':' + endpoint.Port;
            var server = _defaultServers.GetOrAdd(endpoint.Host + ':' + endpoint.Port, e => new VectorTransferServer(null, endpoint.Port, endpoint.Host));

            if (server.Status == ServerStatus.Disposed)
            {
                _defaultServers[key] = server = new VectorTransferServer(null, endpoint.Port, endpoint.Host);
            }

            if (messageHandler != null) server.AddHandler(route, (d, r) => messageHandler(d, r.Content));

            if (startService && server.Status == ServerStatus.Stopped)
            {
                server.Start();
            }

            return server;
        }

        internal static void Shutdown()
        {
            foreach(var server in _defaultServers.ToList())
            {
                try
                {
                    server.Value.Dispose();
                }
                catch
                {
                }

                IVectorTransferServer r;

                _defaultServers.TryRemove(server.Key, out r);
            }
        }
    }
}