using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Nn
{
    internal class Network
    {
        private readonly INetworkSignalFilter _rootLayer;

        public Network(int inputVectorSize, int[] neuronSizes = null, Func<int, INeuron> neuronFactory = null)
        {
            if (neuronSizes == null) neuronSizes = new[] { inputVectorSize };

            INetworkSignalFilter next = null;

            foreach(var n in neuronSizes)
            {
                var prev = next;

                next = new Layer(inputVectorSize, n, neuronFactory);

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

        public ColumnVector1D Calculate(ColumnVector1D input)
        {
            return _rootLayer.Process(input);
        }
    }
}