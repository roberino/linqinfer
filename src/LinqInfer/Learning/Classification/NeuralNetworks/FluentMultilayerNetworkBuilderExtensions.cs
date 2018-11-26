using System;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    static class FluentMultilayerNetworkBuilderExtensions
    {
        /// <summary>
        /// Builds a softmax network configuration
        /// with a single hidden layer
        /// </summary>
        public static IFluentNetworkBuilder ConfigureSoftmaxNetwork(this IFluentNetworkBuilder builder, int hiddenLayerSize, Action<LearningParameters> learningConfig = null)
        {
            return builder.ParallelProcess()
                .ConfigureLearningParameters(p =>
                {
                    learningConfig?.Invoke(p);
                })
                .AddHiddenLinearLayer(hiddenLayerSize, WeightUpdateRules.Default())
                .AddSoftmaxOutput();
        }

        public static IFluentNetworkBuilder AddHiddenSigmoidLayer(this IFluentNetworkBuilder specificationBuilder, int layerSize)
        {
            if (layerSize == 0) return specificationBuilder;

            return specificationBuilder.
                AddHiddenLayer(layerSize, Activators.Sigmoid(1));
        }

        public static IFluentNetworkBuilder AddHiddenLinearLayer(this IFluentNetworkBuilder specificationBuilder, int layerSize, WeightUpdateRule updateRule = null)
        {
            if (layerSize == 0) return specificationBuilder;

            updateRule = updateRule ?? WeightUpdateRules.Default();

            return specificationBuilder.
                AddHiddenLayer(
                    layerSize,
                    Activators.None(),
                    updateRule,
                    NetworkLayerSpecification.DefaultInitialWeightRange);
        }

        public static IFluentNetworkBuilder AddSoftmaxOutput(this IFluentNetworkBuilder specificationBuilder)
        {
            return specificationBuilder
                .ConfigureOutput(LossFunctions.CrossEntropy, Softmax.Factory);
        }

        public static IFluentNetworkBuilder ParallelProcess(this IFluentNetworkBuilder specificationBuilder)
        {
            return ((FluentNetworkBuilder)specificationBuilder).ConfigureLayers(l => l.ParallelProcess = true);
        }

        public static IClassifierTrainingContext<INetworkModel> Build(this IFluentNetworkBuilder specificationBuilder)
        {
            return ((FluentNetworkBuilder)specificationBuilder).Build();
        }
    }
}