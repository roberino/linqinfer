using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.Remoting
{
    internal class RemoteClassifierTrainingClient : IDisposable
    {
        private readonly Uri _serverEndpoint;
        private readonly VectorTransferClient _client;

        public RemoteClassifierTrainingClient(Uri serverEndpoint)
        {
            Util.ValidateUri(serverEndpoint);

            _serverEndpoint = serverEndpoint;

            _client = new VectorTransferClient(null, _serverEndpoint.Port, _serverEndpoint.Host);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task<bool> Delete(string key)
        {
            using (var txHandle = await _client.BeginTransfer(new Uri(_serverEndpoint, "delete").PathAndQuery))
            {
                var res = await txHandle.End(new
                {
                    TargetKey = key
                });

                var doc = new BinaryVectorDocument(res);

                return doc.PropertyOrDefault("Deleted", false);
            }
        }

        public async Task<IObjectClassifier<TClass, TInput>> RestoreClassifier<TInput, TClass>(
            string key,
            TInput exampleInstance = null,
            TClass exampleClass = default(TClass))
           where TInput : class
           where TClass : IEquatable<TClass>
        {
            using (var txHandle = await _client.BeginTransfer(_serverEndpoint.PathAndQuery))
            {
                var data = await txHandle.End(new
                {
                    Key = key
                });

                try
                {
                    var network = new MultilayerNetwork(data);
                    var feClob = network.Properties["InputExtractor"];
                    var featureExtractor = DataExtensions.FromTypeAnnotatedClob<IFloatingPointFeatureExtractor<TInput>>(feClob);
                    var outputMapperClob = network.Properties["OutputMapper"];
                    var outputMapper = DataExtensions.FromTypeAnnotatedClob<ICategoricalOutputMapper<TClass>>(outputMapperClob);
                    var clsf = new MultilayerNetworkObjectClassifier<TClass, TInput>(featureExtractor, outputMapper, network);

                    return clsf;
                }
                finally
                {
                    data.Dispose();
                }
            }
        }

        public async Task<KeyValuePair<string, IObjectClassifier<TClass, TInput>>> CreateClassifier<TInput, TClass>(
            FeatureProcessingPipline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            bool remoteSave = false,
            float errorTolerance = 0.1f,
            params int[] hiddenLayers)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            var outputMapper = new OutputMapperFactory<TInput, TClass>().Create(pipeline.Data, classf);
            var classfc = classf.Compile();

            using (var txHandle = await _client.BeginTransfer(_serverEndpoint.PathAndQuery))
            {
                var layers = hiddenLayers == null ? new[] { pipeline.VectorSize, outputMapper.VectorSize } : new[] { pipeline.VectorSize }.Concat(hiddenLayers).Concat(new[] { outputMapper.VectorSize }).ToArray();

                var np = new NetworkParameters(layers);

                bool hasConverged = false;

                int i = 0;

                while (!hasConverged && i < 100)
                {
                    foreach (var batch in pipeline.ExtractBatches())
                    {
                        var doc = new BinaryVectorDocument();

                        foreach (var obVec in batch)
                        {
                            var cls = classfc(obVec.Value);
                            var output = outputMapper.ExtractColumnVector(cls);

                            doc.Vectors.Add(obVec.Vector.Concat(output));
                        }

                        doc.Properties["VectorSize"] = pipeline.VectorSize.ToString();
                        doc.Properties["LayerSizes"] = string.Join(",", np.LayerSizes);

                        await txHandle.Send(doc);
                    }

                    float averageError;
                    float rateOfChange;

                    using (var response = await txHandle.Receive())
                    {
                        var summary = new BinaryVectorDocument(response);

                        averageError = summary.PropertyOrDefault("AverageError", 0f);
                        rateOfChange = summary.PropertyOrDefault("RateOfErrorChange", 0f);
                    }

                    DebugOutput.Log("Average error = {0}", averageError);

                    hasConverged = averageError < errorTolerance || rateOfChange < 0.01f;
                    i++;
                }

                var finalResponse = await txHandle.End(new
                {
                    SaveOutput = remoteSave,
                    OutputMapper = outputMapper.ToClob(),
                    InputExtractor = pipeline.FeatureExtractor.ToClob()
                });

                try
                {
                    var network = new MultilayerNetwork(finalResponse);

                    var clsf = new MultilayerNetworkObjectClassifier<TClass, TInput>(pipeline.FeatureExtractor, outputMapper, network);

                    return new KeyValuePair<string, IObjectClassifier<TClass, TInput>>(txHandle.Id, clsf);
                }
                finally
                {
                    finalResponse.Dispose();
                }
            }
        }
    }
}