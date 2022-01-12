using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    interface IMultilayerNetwork : INetworkModel, IHasNetworkTopology, ICloneableObject<IMultilayerNetwork>
    {
        void Reset();

        void ForwardPropagate(Action<INetworkSignalFilter> work);

        double BackwardPropagate(IVector targetOutput);
    }
}