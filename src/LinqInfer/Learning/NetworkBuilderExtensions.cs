using System;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;

namespace LinqInfer.Learning
{
    public static class NetworkBuilderExtensions
    {
        /// <summary>
        /// Configures a long short term memory network (LSTM)
        /// and returns a network builder.
        /// </summary>
        public static INetworkBuilder ConfigureLongShortTermMemoryNetwork(this IRecurrentNetworkBuilder builder, int outputSize)
        {
            return builder.ConfigureModules(mb =>
            {
                var module = mb.Module(VectorAggregationType.Concatinate);

                var mult1 = mb.Module(VectorAggregationType.Multiply);
                var mult2 = mb.Module(VectorAggregationType.Multiply);
                var mult3 = mb.Module(VectorAggregationType.Multiply);
                var sum1 = mb.Module(VectorAggregationType.Add);
                var tanop1 = mb.Module(VectorAggregationType.HyperbolicTangent);

                var sig1 = mb.Layer(outputSize, Activators.Sigmoid());
                var sig2 = mb.Layer(outputSize, Activators.Sigmoid());
                var tan1 = mb.Layer(outputSize, Activators.HyperbolicTangent());
                var sig3 = mb.Layer(outputSize, Activators.Sigmoid());

                module.ConnectTo(sig1, sig2, tan1, sig3);

                sig1.ConnectTo(mult1);

                sig2.ConnectTo(mult2);
                tan1.ConnectTo(mult2);

                mult1.ConnectTo(sum1);
                mult2.ConnectTo(sum1);

                sum1.ConnectTo(tanop1);

                tanop1.ConnectTo(mult3);
                sig3.ConnectTo(mult3);

                module.ReceiveFrom(mult3);
                mult1.ReceiveFrom(sum1);

                return mb.Output(mult3, outputSize);
            });
        }

        /// <summary>
        /// Builds a softmax network configuration
        /// with a single hidden layer
        /// </summary>
        public static INetworkBuilder ConfigureSoftmaxNetwork(this IConvolutionalNetworkBuilder builder, int hiddenLayerSize, Action<TrainingParameters> learningConfig = null)
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