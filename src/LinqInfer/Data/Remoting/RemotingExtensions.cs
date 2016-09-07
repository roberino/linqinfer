using LinqInfer.Learning.Features;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        }

        /// <summary>
        /// Sends extracted data to a server endpoint
        /// </summary>
        public static async Task SendTo<T>(this FeatureProcessingPipline<T> pipeline, Uri endpoint) where T : class
        {
            ValidateUri(endpoint);

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

        public static IVectorTransferServer CreateRemoteService(this Uri endpoint, Func<DataBatch, bool> messageHandler, bool startService = true)
        {
            ValidateUri(endpoint);

            var server = _defaultServers.GetOrAdd(endpoint.Host + ':' + endpoint.Port, e => new VectorTransferServer(null, endpoint.Port, endpoint.Host));

            server.AddHandler(endpoint.PathAndQuery, messageHandler);

            if (startService && server.Status == ServerStatus.Stopped)
            {
                server.Start();
            }

            return server;
        }

        private static void ValidateUri(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (uri.Scheme != "tcp")
            {
                throw new ArgumentException("Only TCP scheme supported e.g. tcp://host:3211");
            }
        }
    }
}