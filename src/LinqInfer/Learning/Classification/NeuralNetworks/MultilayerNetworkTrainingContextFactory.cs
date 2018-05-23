using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class MultilayerNetworkTrainingContextFactory<TClass> where TClass : IEquatable<TClass>
    {
        private int _currentId;

        public MultilayerNetworkTrainingContextFactory()
        {
            _currentId = 0;
        }

        public IClassifierTrainingContext<NetworkParameters> Create(MultilayerNetwork network)
        {
            return new MultilayerNetworkTrainingContext<NetworkParameters>(() => ++_currentId, network, network.Parameters);
        }

        public IClassifierTrainingContext<NetworkParameters> Create(NetworkParameters parameters)
        {
            return new MultilayerNetworkTrainingContext<NetworkParameters>(() => ++_currentId, new MultilayerNetwork(parameters), parameters);
        }
    }
}