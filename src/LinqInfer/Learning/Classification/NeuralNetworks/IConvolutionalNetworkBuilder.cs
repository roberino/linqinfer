using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IConvolutionalNetworkBuilder
    {
        IConvolutionalNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config);

        IConvolutionalNetworkBuilder AddHiddenLayer(int layerSize, ActivatorExpression activator = null,
            WeightUpdateRule weightUpdateRule = null, Range? initialWeightRange = null);

        INetworkBuilder ConfigureOutput(ILossFunction lossFunction,
            Func<int, ISerialisableDataTransformation> transformationFactory = null,
            ActivatorExpression activator = null,
            WeightUpdateRule weightUpdateRule = null,
            Range? initialWeightRange = null);
    }
}