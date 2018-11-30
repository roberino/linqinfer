using System;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class ModuleBuilderFactory
    {
        readonly RecurrentNetworkBuilder _networkBuilder;

        internal ModuleBuilderFactory(RecurrentNetworkBuilder networkBuilder)
        {
            _networkBuilder = networkBuilder;
        }

        public NetworkModuleSpecification Module(VectorAggregationType aggregationType)
        {
            var module = new NetworkModuleSpecification(_networkBuilder.CreateId())
            {
                InputOperator = aggregationType
            };

            _networkBuilder.AddModule(module);

            return module;
        }

        public NetworkLayerSpecification Layer(int layerSize, ActivatorExpression activator)
        {
            var layerSpec = new NetworkLayerSpecification(_networkBuilder.CreateId(), layerSize, activator);

            _networkBuilder.AddModule(layerSpec);

            return layerSpec;
        }

        public NetworkOutputSpecification Output(NetworkModuleSpecification moduleSpecification, int outputSize, ILossFunction lossFunction = null, Func<int, ISerialisableDataTransformation> transformation = null)
        {
            return new NetworkOutputSpecification(moduleSpecification, outputSize, lossFunction)
            {
                OutputTransformation = transformation?.Invoke(outputSize)
            };
        }

        public NetworkOutputSpecification Output(NetworkLayerSpecification layerSpecification, ILossFunction lossFunction = null, Func<int, ISerialisableDataTransformation> transformation = null)
        {
            return new NetworkOutputSpecification(layerSpecification, lossFunction)
            {
                OutputTransformation = transformation?.Invoke(layerSpecification.LayerSize)
            };
        }
    }
}