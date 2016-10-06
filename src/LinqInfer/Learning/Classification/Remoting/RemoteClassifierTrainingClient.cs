using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.Remoting
{
    internal class RemoteClassifierTrainingClient : IRemoteClassifierTrainingClient
    {
        private readonly Uri _serverEndpoint;
        private readonly VectorTransferClient _client;

        public RemoteClassifierTrainingClient(Uri serverEndpoint)
        {
            Util.ValidateUri(serverEndpoint);

            _serverEndpoint = serverEndpoint;

            _client = new VectorTransferClient(null, _serverEndpoint.Port, _serverEndpoint.Host);
        }

        public int Timeout { get { return _client.Timeout; } set { _client.Timeout = value; } }

        public void Dispose()
        {
            if (_client != null) _client.Dispose();
        }

        public async Task<bool> Delete(Uri uri)
        {
            Contract.Requires(uri != null);

            using (var txHandle = await _client.BeginTransfer(uri.PathAndQuery, Verb.Delete))
            {
                var res = await txHandle.End(new { });

                var doc = new BinaryVectorDocument(res);

                return doc.PropertyOrDefault("Deleted", false);
            }
        }

        public async Task<IObjectClassifier<TClass, TInput>> RestoreClassifier<TInput, TClass>(
            Uri uri,
            IFloatingPointFeatureExtractor<TInput> uninitialisedFeatureExtractor = null,
            TClass exampleClass = default(TClass))
           where TInput : class
           where TClass : IEquatable<TClass>
        {
            Contract.Requires(uri != null);

            using (var txHandle = await _client.BeginTransfer(uri.PathAndQuery, Verb.Get))
            {
                var data = await txHandle.End(new { });

                if (uninitialisedFeatureExtractor == null)
                {
                    uninitialisedFeatureExtractor = new ObjectFeatureExtractorFactory().CreateFeatureExtractor<TInput>();
                }

                try
                {
                    var network = new MultilayerNetwork(data);
                    var feClob = network.Properties["InputExtractor"];
                    var featureExtractor = DataExtensions.FromClob(feClob, t => uninitialisedFeatureExtractor);
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

        public Task<KeyValuePair<Uri, IObjectClassifier<TClass, TInput>>> ExtendClassifier<TInput, TClass>(
            ITrainingSet<TInput, TClass> trainingSet,
            Uri uri,
            float errorTolerance = 0.1f)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            Contract.Requires(uri != null);

            var name = uri.PathAndQuery.Split('/').Where(s => !string.IsNullOrEmpty(s)).Last();

            return CreateClassifier(trainingSet, true, name, errorTolerance);
        }

        public async Task<KeyValuePair<Uri, IObjectClassifier<TClass, TInput>>> CreateClassifier<TInput, TClass>(
            ITrainingSet<TInput, TClass> trainingSet,
            bool remoteSave = false,
            string name = null,
            float errorTolerance = 0.1f,
            params int[] hiddenLayers)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            Contract.Requires(trainingSet != null);

            using (var txHandle = await _client.BeginTransfer(_serverEndpoint.PathAndQuery, Verb.Create))
            {
                var layers = hiddenLayers == null ? new[] { trainingSet.FeaturePipeline.VectorSize, trainingSet.OutputMapper.VectorSize } : new[] { trainingSet.FeaturePipeline.VectorSize }.Concat(hiddenLayers).Concat(new[] { trainingSet.OutputMapper.VectorSize }).ToArray();

                var np = new NetworkParameters(layers);

                bool hasConverged = false;

                int i = 0;

                while (!hasConverged && i < 100)
                {
                    foreach (var batch in trainingSet.ExtractInputOutputVectorBatches())
                    {
                        var doc = new BinaryVectorDocument();

                        foreach (var vectorPair in batch)
                        {
                            doc.Vectors.Add(vectorPair.Item1.Concat(vectorPair.Item2));
                        }

                        doc.Properties["VectorSize"] = trainingSet.FeaturePipeline.VectorSize.ToString();
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

                var outputName = name ?? txHandle.Id;

                var finalResponse = await txHandle.End(new
                {
                    SaveOutput = remoteSave,
                    Name = outputName,
                    OutputMapper = trainingSet.OutputMapper.ToClob(),
                    InputExtractor = trainingSet.FeaturePipeline.FeatureExtractor.ToClob()
                });

                try
                {
                    var network = new MultilayerNetwork(finalResponse);

                    var clsf = new MultilayerNetworkObjectClassifier<TClass, TInput>(trainingSet.FeaturePipeline.FeatureExtractor, trainingSet.OutputMapper, network);

                    return new KeyValuePair<Uri, IObjectClassifier<TClass, TInput>>(new Uri(_serverEndpoint, outputName), clsf);
                }
                finally
                {
                    finalResponse.Dispose();
                }
            }
        }
    }
}