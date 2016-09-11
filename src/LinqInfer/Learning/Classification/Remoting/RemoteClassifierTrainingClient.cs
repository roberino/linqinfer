using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
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

        public async Task<KeyValuePair<string, IObjectClassifier<TClass, TInput>>> Send<TInput, TClass>(
            FeatureProcessingPipline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            float errorTolerance = 0.1f) 
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            var outputMapper = new OutputMapperFactory<TInput, TClass>().Create(pipeline.Data, classf);
            var classfc = classf.Compile();

            var txHandle = await _client.BeginTransfer(_serverEndpoint.PathAndQuery);
            var np = new NetworkParameters(pipeline.VectorSize, outputMapper.VectorSize);

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

            var response = await txHandle.End();

            var nn = new MultilayerNetwork(response);

            var clsf = new MultilayerNetworkObjectClassifier<TClass, TInput>(pipeline.FeatureExtractor, outputMapper, nn);

            return new KeyValuePair<string, IObjectClassifier<TClass, TInput>>(
                txHandle.Id, clsf);
        }
    }
}
