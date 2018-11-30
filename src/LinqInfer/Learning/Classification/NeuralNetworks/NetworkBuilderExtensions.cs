using System;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    static class NetworkBuilderExtensions
    {
        /// <summary>
        /// Builds a softmax network configuration
        /// with a single hidden layer
        /// </summary>
        public static INetworkBuilder ConfigureSoftmaxNetwork(this IConvolutionalNetworkBuilder builder, int hiddenLayerSize, Action<LearningParameters> learningConfig = null)
        {
            return builder
                .ConfigureLearningParameters(p =>
                {
                    learningConfig?.Invoke(p);
                })
                .AddHiddenLinearLayer(hiddenLayerSize, WeightUpdateRules.Default())
                .AddSoftmaxOutput();
        }

        public static IConvolutionalNetworkBuilder AddHiddenSigmoidLayer(this IConvolutionalNetworkBuilder specificationBuilder, int layerSize)
        {
            return specificationBuilder.
                AddHiddenLayer(layerSize, Activators.Sigmoid(1));
        }

        public static IConvolutionalNetworkBuilder AddHiddenLinearLayer(this IConvolutionalNetworkBuilder specificationBuilder, int layerSize, WeightUpdateRule updateRule = null)
        {
            updateRule = updateRule ?? WeightUpdateRules.Default();

            return specificationBuilder.
                AddHiddenLayer(
                    layerSize,
                    Activators.None(),
                    updateRule,
                    NetworkLayerSpecification.DefaultInitialWeightRange);
        }

        public static INetworkBuilder AddSoftmaxOutput(this IConvolutionalNetworkBuilder specificationBuilder)
        {
            return specificationBuilder
                .ConfigureOutput(LossFunctions.CrossEntropy, Softmax.Factory);
        }
    }
}