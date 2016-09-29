using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqInfer.Learning.Classification.Remoting
{
    internal class RemoteClassifierTrainingServer : IServer
    {
        private readonly IDictionary<string, IRawClassifierTrainingContext<NetworkParameters>> _trainingContexts;

        private readonly IVectorTransferServer _server;
        private readonly IBlobStore _blobStore;
        private readonly MultilayerNetworkTrainingContextFactory<string> _trainingContextFactory;
        private readonly ICategoricalOutputMapper<string> _outputMapper;

        public ServerStatus Status
        {
            get
            {
                return _server.Status;
            }
        }

        public Uri BaseEndpoint
        {
            get
            {
                return _server.BaseEndpoint;
            }
        }

        public RemoteClassifierTrainingServer(Uri uri, IBlobStore blobStore)
        {
            _blobStore = blobStore;

            _server = uri.CreateRemoteService(null, false);

            _server.AddHandler(new UriRoute(uri, null, Verb.Create), Process);
            _server.AddHandler(new UriRoute(uri, null, Verb.Get), ListKeys);
            _server.AddHandler(new UriRoute(uri, "status", Verb.Get), ServerStatus);
            _server.AddHandler(new UriRoute(uri, "{key}", Verb.Delete), Delete);
            _server.AddHandler(new UriRoute(uri, "{key}", Verb.Get), Restore);

            _outputMapper = new OutputMapperFactory<double, string>().Create(new string[] { "" });

            _trainingContextFactory = new MultilayerNetworkTrainingContextFactory<string>(_outputMapper);

            _trainingContexts = new Dictionary<string, IRawClassifierTrainingContext<NetworkParameters>>();
        }

        public void Start()
        {
            _server.Start();
        }

        public void Stop()
        {
            _server.Stop();
        }

        private bool ListKeys(DataBatch batch, TcpResponse tcpResponse)
        {
            var writer = tcpResponse.CreateTextResponse();

            var keys = _blobStore.ListKeys<MultilayerNetwork>().Result;

            foreach (var key in keys)
            {
                writer.WriteLine(Util.ConvertProtocol(new Uri(_server.BaseEndpoint, key), tcpResponse.Header.TransportProtocol).ToString());
            }

            return false;
        }

        private bool ServerStatus(DataBatch batch, TcpResponse tcpResponse)
        {
            tcpResponse.CreateTextResponse().Write(_server.Status.ToString());
            return false;
        }

        private bool Restore(DataBatch batch, TcpResponse tcpResponse)
        {
            var response = tcpResponse.Content;

            var key = batch.Properties["key"];

            var task = _blobStore.Transfer<MultilayerNetwork>(key, response);

            task.Wait();

            return true;
        }

        private bool Delete(DataBatch batch, TcpResponse tcpResponse)
        {
            var response = tcpResponse.Content;

            var key = batch.Properties["key"];

            var deleted = _blobStore.Delete<MultilayerNetwork>(key);

            batch.Properties["Deleted"] = deleted.ToString();

            batch.Save(response);

            return deleted;
        }

        private bool Process(DataBatch batch, TcpResponse tcpResponse)
        {
            var response = tcpResponse.Content;

            IRawClassifierTrainingContext<NetworkParameters> ctx;

            lock (_trainingContexts)
            {
                if (!_trainingContexts.TryGetValue(batch.Id, out ctx))
                {
                    var layerSizes = batch.Properties["LayerSizes"].Split(',').Select(s => int.Parse(s)).ToArray();
                    var networkParams = new NetworkParameters(layerSizes);
                    _trainingContexts[batch.Id] = ctx = _trainingContextFactory.Create(networkParams);
                }
            }

            lock (batch.Id)
            {
                if (batch.Vectors.Any())
                {
                    var iterations = batch.PropertyOrDefault("Iterations", 1);

                    foreach (var i in Enumerable.Range(0, iterations))
                    {
                        foreach (var inputOutputPair in batch
                            .Vectors
                            .Select(v => v.Split(ctx.Parameters.InputVectorSize))
                            .RandomOrder()
                            )
                        {
                            ctx.Train(inputOutputPair[1], inputOutputPair[0]);
                        }
                    }
                }

                if (!batch.KeepAlive)
                {
                    if (batch.PropertyOrDefault("SaveOutput", false))
                    {
                        var network = (MultilayerNetwork)ctx.Output;

                        foreach (var prop in batch.Properties)
                        {
                            network.Properties[prop.Key] = prop.Value;
                        }

                        var name = batch.PropertyOrDefault("Name", batch.Id);

                        _blobStore.Store(name, network);
                    }

                    ctx.Output.Save(response);

                    _trainingContexts.Remove(batch.Id);
                }
                else
                {
                    if (batch.SendResponse)
                    {
                        WriteSummaryResponse(ctx, response);
                    }
                }
            }

            return true;
        }

        private void WriteSummaryResponse(IRawClassifierTrainingContext<NetworkParameters> ctx, Stream response)
        {
            var summary = new BinaryVectorDocument();

            summary.Properties["RateOfErrorChange"] = ctx.RateOfErrorChange.GetValueOrDefault(1).ToString();
            summary.Properties["AverageError"] = ctx.AverageError.ToString();

            summary.Save(response);
        }

        public void Dispose()
        {
            _blobStore.Dispose();
            _server.Dispose();
        }
    }
}