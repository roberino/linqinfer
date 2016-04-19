using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Nn
{
    internal class MultilayerNetwork
    {
        private readonly INetworkSignalFilter _rootLayer;

        public MultilayerNetwork(int inputVectorSize, int[] neuronSizes = null, ActivatorFunc activator = null, Func<int, INeuron> neuronFactory = null)
        {
            Activator = activator ?? Activators.Sigmoid();

            if (neuronSizes == null) neuronSizes = new[] { inputVectorSize };

            INetworkSignalFilter next = null;

            foreach(var n in neuronSizes)
            {
                var prev = next;

                next = new NetworkLayer(inputVectorSize, n, Activator, neuronFactory);

                if(prev == null)
                {
                    _rootLayer = next;
                }
                else
                {
                    prev.Successor = next;
                }
            }
        }

        public ActivatorFunc Activator { get; private set; }

        public ILayer LastLayer { get { return Layers.Reverse().First(); } }

        public IEnumerable<T> ForEachLayer<T>(Func<ILayer, T> func, bool reverse = true)
        {
            return (reverse ? Layers.Reverse() : Layers).Select(l => func(l));
        }

        public ColumnVector1D Evaluate(ColumnVector1D input)
        {
            return _rootLayer.Process(input);
        }

        public IEnumerable<ILayer> Layers
        {
            get
            {
                var next = _rootLayer as ILayer;

                while(next != null)
                {
                    yield return next;

                    next = next.Successor as ILayer;
                }
            }
        }
    }
}