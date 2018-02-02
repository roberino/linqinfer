using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    internal class NetworkParameterFactory
    {
        private readonly int _vectorSize;
        private readonly int _outputSize;
        private readonly double _learningRate;

        public NetworkParameterFactory(int vectorSize, int outputSize, double learningRate = 0.1)
        {
            _vectorSize = vectorSize;
            _outputSize = outputSize;
            _learningRate = learningRate;
        }

        public IEnumerable<NetworkParameters> GenerateParameters(ActivatorFunc activator)
        {
            // d | d(2d + 1) | (2d + 1) | o

            int sizeOfGenePool = 6;

            var d = _vectorSize;

            var l = new int[sizeOfGenePool][];

            var o = _outputSize;

            l[0] = L(d, d * (2 * d + 1), (2 * d + 1), o);
            l[1] = L(d, d * 2, d * 2, o);
            l[2] = L(d, d * 4, d * 2, o);
            l[3] = L(d, d * (2 * d + 1), (2 * d + 1), o);
            l[4] = L(d, o);
            l[5] = L(d, d * 4, o);

            return Enumerable.Range(0, sizeOfGenePool).Select(n => new NetworkParameters(l[n], activator)
            {
                LearningRate = _learningRate
            });
        }
        private static int[] L(params int[] n)
        {
            return n;
        }
    }
}