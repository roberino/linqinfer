using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Nn
{
    internal class MultilayerNetwork
    {
        private readonly int _inputVectorSize;
        private readonly Func<int, INeuron> _neuronFactory;
        private INetworkSignalFilter _rootLayer;

        public MultilayerNetwork(int inputVectorSize, int[] neuronSizes, ActivatorFunc activator = null, Func<int, INeuron> neuronFactory = null)
        {
            _inputVectorSize = inputVectorSize;
            _neuronFactory = neuronFactory;

            Activator = activator ?? Activators.Sigmoid();
            
            Initialise(neuronSizes);
        }

        internal MultilayerNetwork(int inputVectorSize, ActivatorFunc activator = null, Func<int, INeuron> neuronFactory = null)
        {
            _inputVectorSize = inputVectorSize;
            _neuronFactory = neuronFactory;

            Activator = activator ?? Activators.Sigmoid();
        }

        public void Initialise(params int[] neuronSizes)
        {
            if (neuronSizes == null) neuronSizes = new[] { _inputVectorSize };

            INetworkSignalFilter next = null;

            int lastN = 0;

            foreach (var n in neuronSizes.Where(x => x > 0))
            {
                var prev = next;

                if (prev == null)
                {
                    next = new NetworkLayer(_inputVectorSize, _inputVectorSize, Activator, _neuronFactory);
                    _rootLayer = next;
                }
                else
                {
                    next = new NetworkLayer(lastN, n, Activator, _neuronFactory);
                    prev.Successor = next;
                }

                lastN = n;
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
            if (_rootLayer == null) throw new InvalidOperationException("Not initialised");

            var res = _rootLayer.Process(input);

            Console.WriteLine("{0} => {1}", input.ToCsv(2), res.ToCsv(2));

            return res;
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