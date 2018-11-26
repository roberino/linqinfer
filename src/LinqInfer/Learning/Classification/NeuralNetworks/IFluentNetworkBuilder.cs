using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IFluentNetworkBuilder
    {
        IFluentNetworkBuilder ConfigureModule(Action<ModuleBuilderFactory> moduleConfig);

        IFluentNetworkBuilder AddHiddenLayer(int? layerSize = null, ActivatorExpression activator = null, WeightUpdateRule weightUpdateRule = null, Range? initialWeightRange = null, bool parallelProcess = false);
        
        IFluentNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config);

        IFluentNetworkBuilder ConfigureOutput(NetworkModuleSpecification outputModule, ILossFunction lossFunction, Func<int, ISerialisableDataTransformation> transformationFactory = null);

        IFluentNetworkBuilder ConfigureOutput(ILossFunction lossFunction, Func<int, ISerialisableDataTransformation> transformationFactory = null);
    }
}