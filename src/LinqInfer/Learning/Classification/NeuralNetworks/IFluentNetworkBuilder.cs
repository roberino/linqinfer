using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IFluentNetworkBuilder
    {
        IFluentNetworkBuilder AddHiddenLayer(LayerSpecification layer);
        IFluentNetworkBuilder ConfigureLearningParameters(double learningRate, double minimumError);
        IFluentNetworkBuilder ConfigureLearningParameters(LearningParameters learningParameters);
        IFluentNetworkBuilder ConfigureOutputLayer(ActivatorFunc activator, ILossFunction lossFunction, Range? initialWeightRange = null);
        IFluentNetworkBuilder TransformOutput(Func<int, ISerialisableVectorTransformation> transformationFactory);
    }
}