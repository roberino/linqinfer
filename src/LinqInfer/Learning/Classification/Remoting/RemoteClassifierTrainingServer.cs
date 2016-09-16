﻿using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                        _blobStore.Store(batch.Id, ctx.Output);
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