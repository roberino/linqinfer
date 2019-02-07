using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public interface IRecurrentNetworkBuilder
    {
        IRecurrentNetworkBuilder ConfigureLearningParameters(Action<TrainingParameters> config);

        INetworkBuilder ConfigureModules(Func<ModuleBuilderFactory, NetworkOutputSpecification> moduleConfig);
    }
}