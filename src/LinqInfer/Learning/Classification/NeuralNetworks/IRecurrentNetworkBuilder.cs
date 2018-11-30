using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IRecurrentNetworkBuilder
    {
        IRecurrentNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config);

        INetworkBuilder ConfigureModules(Func<ModuleBuilderFactory, NetworkOutputSpecification> moduleConfig);
    }
}