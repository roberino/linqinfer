using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Features;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.Remoting
{
    public static class Extensions
    {
        public static async Task<IObjectClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this FeatureProcessingPipline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            Uri serverEndpoint,
            bool saveRemotely = false,
            float errorTolerance = 0.1f,
            params int[] hiddenLayers) where TInput : class where TClass : IEquatable<TClass>
        {
            using (var client = new RemoteClassifierTrainingClient(serverEndpoint))
            {
                var nn = await client.CreateClassifier(pipeline, classf, saveRemotely, errorTolerance, hiddenLayers);

                return nn.Value;
            }
        }

        public static IServer CreateClassifierTrainingServer(this Uri serverEndpoint, IBlobStore storage)
        {
            return new RemoteClassifierTrainingServer(serverEndpoint, storage);
        }
    }
}