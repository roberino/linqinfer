using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IFluentNetworkBuilder
    {
        IFluentNetworkBuilder AddHiddenLayer(LayerSpecification layer);
        IFluentNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config);
        IFluentNetworkBuilder ConfigureLearningParameters(LearningParameters learningParameters);
        IFluentNetworkBuilder ConfigureOutputLayer(IActivatorFunction activator, ILossFunction lossFunction, Range? initialWeightRange = null);
        IFluentNetworkBuilder TransformOutput(Func<int, ISerialisableVectorTransformation> transformationFactory);
    }
}