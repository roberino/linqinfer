using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IConvolutionalNetworkBuilder
    {
        /// <summary>
        /// Configures the parameters used for training
        /// </summary>
        IConvolutionalNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config);

        /// <summary>
        /// Adds a hidden neuron layer
        /// </summary>
        IConvolutionalNetworkBuilder AddHiddenLayer(int layerSize, ActivatorExpression activator = null,
            WeightUpdateRule weightUpdateRule = null, Range? initialWeightRange = null);

        /// <summary>
        /// Configures the loss function, output layer and any final transformations
        /// </summary>
        INetworkBuilder ConfigureOutput(ILossFunction lossFunction,
            Func<int, ISerialisableDataTransformation> transformationFactory = null,
            ActivatorExpression activator = null,
            WeightUpdateRule weightUpdateRule = null,
            Range? initialWeightRange = null);

        /// <summary>
        /// Adds default behaviour when not specified
        /// </summary>
        INetworkBuilder ApplyDefaults();
    }
}