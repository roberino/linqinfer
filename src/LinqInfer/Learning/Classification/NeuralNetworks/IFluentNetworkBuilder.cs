using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IFluentNetworkBuilder
    {
        IFluentNetworkBuilder AddHiddenLayer(NetworkLayerSpecification networkLayer);
        IFluentNetworkBuilder AddHiddenLayer(Func<LearningParameters, NetworkLayerSpecification> layerFactory);
        IFluentNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config);
        IFluentNetworkBuilder ConfigureLearningParameters(LearningParameters learningParameters);
        IFluentNetworkBuilder ConfigureOutputLayer(ActivatorExpression activator, ILossFunction lossFunction, Range? initialWeightRange = null, WeightUpdateRule updateRule = null);
        IFluentNetworkBuilder TransformOutput(Func<int, ISerialisableDataTransformation> transformationFactory);
    }
}