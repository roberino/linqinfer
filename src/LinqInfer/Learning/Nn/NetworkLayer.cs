using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Nn
{
    [Serializable]
    internal class NetworkLayer : ILayer
    {
        private readonly IList<INeuron> _neurons;

        public NetworkLayer(int inputVectorSize, int neuronCount, ActivatorFunc activator, Func<int, INeuron> neuronFactory = null)
        {
            var nf = neuronFactory ?? (n => new NeuronBase(n));
            _neurons = Enumerable.Range(1, neuronCount).Select(n =>
            {
                var nx = nf(inputVectorSize);
                nx.Activator = activator.Activator;
                return nx;
            }).ToList();
        }

        public virtual ColumnVector1D Process(ColumnVector1D input)
        {
            var output = _neurons.Select(n => n.Evaluate(input));

            var outputVect = new ColumnVector1D(output.ToArray());

            if (Successor == null) return outputVect;

            return Successor.Process(outputVect);
        }

        public ColumnVector1D ForEachNeuron(Func<INeuron, int, double> func)
        {
            int i = 0;
            var result = new ColumnVector1D(_neurons.Select(n => func(n, i++)).ToArray());
            OnCalculate(result);
            return result;
        }

        public IEnumerable<T> ForEachNeuron<T>(Func<INeuron, T> func)
        {
            return _neurons.Select(n => func(n));
        }

        public INetworkSignalFilter Successor { get; set; }

        public int Size
        {
            get
            {
                return _neurons.Count;
            }
        }

        public INeuron this[int index]
        {
            get
            {
                return _neurons[index];
            }
        }

        private void OnCalculate(ColumnVector1D vector)
        {
            var ev = Calculation;

            if (ev != null) ev.Invoke(this, new ColumnVector1DEventArgs(vector));
        }

        public event EventHandler<ColumnVector1DEventArgs> Calculation;
    }
}