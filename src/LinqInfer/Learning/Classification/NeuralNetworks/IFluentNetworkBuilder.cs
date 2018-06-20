using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IFluentNetworkBuilder
    {
        IFluentNetworkBuilder AddHiddenLayer(LayerSpecification layer);
        IFluentNetworkBuilder AddHiddenLayer(Func<LearningParameters, LayerSpecification> layerFactory);
        IFluentNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config);
        IFluentNetworkBuilder ConfigureLearningParameters(LearningParameters learningParameters);
        IFluentNetworkBuilder ConfigureOutputLayer(IActivatorFunction activator, ILossFunction lossFunction, Range? initialWeightRange = null, Func<LearningParameters, IWeightUpdateRule> updateRule = null);
        IFluentNetworkBuilder TransformOutput(Func<int, ISerialisableDataTransformation> transformationFactory);
    }
}