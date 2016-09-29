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
        public static IRemoteClassifierTrainingClient CreateMultilayerNeuralNetworkClient(this Uri serverEndpoint)
        {
            return new RemoteClassifierTrainingClient(serverEndpoint);
        }

        public static IServer CreateMultilayerNeuralNetworkServer(this Uri serverEndpoint, IBlobStore storage)
        {
            return new RemoteClassifierTrainingServer(serverEndpoint, storage);
        }

        public static async Task<IObjectClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this FeatureProcessingPipline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            Uri serverEndpoint,
            bool saveRemotely = false,
            string name = null,
            float errorTolerance = 0.1f,
            params int[] hiddenLayers) where TInput : class where TClass : IEquatable<TClass>
        {
            using (var client = new RemoteClassifierTrainingClient(serverEndpoint))
            {
                var trainingSet = new TrainingSet<TInput, TClass>(pipeline, classf);
                var nn = await client.CreateClassifier(trainingSet, saveRemotely, name, errorTolerance, hiddenLayers);

                return nn.Value;
            }
        }
    }
}