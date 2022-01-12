using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class RecurrentNetworkBuilder : INetworkBuilder, IRecurrentNetworkBuilder
    {
        readonly IList<NetworkModuleSpecification> _modules;
        readonly TrainingParameters trainingParams;
        readonly int _inputVectorSize;

        int _currentId;

        NetworkOutputSpecification _output;

        RecurrentNetworkBuilder(int inputVectorSize)
        {
            _inputVectorSize = ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));
            _currentId = 0;
            trainingParams = new TrainingParameters();
            _modules = new List<NetworkModuleSpecification>();
        }

        public static IRecurrentNetworkBuilder Create(int inputVectorSize) => new RecurrentNetworkBuilder(inputVectorSize);

        public void AddModule(NetworkModuleSpecification networkModule)
        {
            _modules.Add(networkModule);
        }

        public int CreateId() => ++_currentId;

        public IRecurrentNetworkBuilder ConfigureLearningParameters(Action<TrainingParameters> config)
        {
            var lp = trainingParams.Clone(true);

            config(lp);

            lp.Validate();

            config(trainingParams);

            return this;
        }

        public INetworkBuilder ConfigureModules(Func<ModuleBuilderFactory, NetworkOutputSpecification> moduleConfig)
        {
            var factory = new ModuleBuilderFactory(this);

            _output = moduleConfig(factory);

            return this;
        }

        public IClassifierTrainingContext<INetworkModel> Build()
        {
            var spec = new NetworkSpecification(
                trainingParams,
                _inputVectorSize,
                _output,
                _modules.ToArray());

            return new MultilayerNetworkTrainingContext(new MultilayerNetwork(spec));
        }
    }
}