﻿using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Classification.Remoting
{
    internal class RemoteClassifierTrainingServer : IDisposable
    {
        private readonly IDictionary<string, IRawClassifierTrainingContext<NetworkParameters>> _trainingContexts;

        private readonly IVectorTransferServer _server;
        private readonly IBlobStore _blobStore;
        private readonly MultilayerNetworkTrainingContextFactory<string> _trainingContextFactory;
        private readonly ICategoricalOutputMapper<string> _outputMapper;

        public RemoteClassifierTrainingServer(Uri uri, IBlobStore blobStore)
        {
            _blobStore = blobStore;

            _server = uri.CreateRemoteService(Process, false);

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

        private bool Process(DataBatch batch, Stream response)
        {
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
                foreach (var inputOutputPair in batch.Vectors.Select(v => v.Split(ctx.Parameters.InputVectorSize)))
                {
                    ctx.Train(inputOutputPair[1], inputOutputPair[0]);
                }

                if (!batch.KeepAlive)
                {
                    if (batch.PropertyOrDefault("SaveOutput", false))
                        _blobStore.Store(batch.Id, ctx.Output);

                    ctx.Output.Save(response);
                }
            }

            return true;
        }

        public void Dispose()
        {
            _blobStore.Dispose();
            _server.Dispose();
        }
    }
}