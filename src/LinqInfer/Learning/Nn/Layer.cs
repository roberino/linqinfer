using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Nn
{
    internal class Layer : INetworkSignalFilter
    {
        private readonly IList<INeuron> _neurons;

        public Layer(int inputVectorSize, int neuronCount, Func<int, INeuron> neuronFactory = null)
        {
            var nf = neuronFactory ?? (n => new NeuronBase(n));
            _neurons = Enumerable.Range(1, neuronCount).Select(n => nf(inputVectorSize)).ToList();
        }

        public virtual ColumnVector1D Process(ColumnVector1D input)
        {
            var output = _neurons.Select(n => n.Calculate(input));

            var outputVect = new ColumnVector1D(output.ToArray());

            if (Successor == null) return outputVect;

            return Successor.Process(outputVect);
        }

        public INetworkSignalFilter Successor { get; set; }
    }
}