using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IFluentNetworkBuilder
    {
        IFluentNetworkBuilder ConfigureModule(Action<ModuleFactory> moduleConfig);

        IFluentNetworkBuilder AddHiddenLayer(int? layerSize = null, ActivatorExpression activator = null, ILossFunction lossFunction = null, WeightUpdateRule weightUpdateRule = null, Range? initialWeightRange = null, bool parallelProcess = false);
        
        IFluentNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config);
        IFluentNetworkBuilder ConfigureOutputLayer(ActivatorExpression activator, ILossFunction lossFunction, Range? initialWeightRange = null, WeightUpdateRule updateRule = null);
        IFluentNetworkBuilder TransformOutput(Func<int, ISerialisableDataTransformation> transformationFactory);
    }
}