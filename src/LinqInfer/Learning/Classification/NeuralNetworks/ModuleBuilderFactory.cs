using System;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class ModuleBuilderFactory
    {
        readonly FluentNetworkBuilder _networkBuilder;

        internal ModuleBuilderFactory(FluentNetworkBuilder networkBuilder)
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

        public void Output(NetworkModuleSpecification moduleSpecification, ILossFunction lossFunction = null, Func<int, ISerialisableDataTransformation> transformation = null)
        {
            _networkBuilder.ConfigureOutput(moduleSpecification, lossFunction, transformation);
        }
    }
}