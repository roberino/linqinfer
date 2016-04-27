using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Nn
{
    internal class MultilayerNetworkClassifier<TClass> : IClassifier<TClass, double>
    {
        private readonly MultilayerNetwork _network;
        private readonly Func<ColumnVector1D, IEnumerable<ClassifyResult<TClass>>> _outputMapper;

        public MultilayerNetworkClassifier(
            MultilayerNetwork network,
            Func<ColumnVector1D, IEnumerable<ClassifyResult<TClass>>> outputMapper)
        {
            _network = network;
            _outputMapper = outputMapper;
        }

        public ClassifyResult<TClass> Classify(double[] vector)
        {
            var output = _network.Evaluate(new ColumnVector1D(vector));

            return _outputMapper(output).OrderByDescending(x => x.Score).FirstOrDefault();
        }

        public IEnumerable<ClassifyResult<TClass>> FindPossibleMatches(double[] vector)
        {
            var output = _network.Evaluate(new ColumnVector1D(vector));

            return _outputMapper(output).OrderByDescending(x => x.Score).ToList();
        }

        public override string ToString()
        {
            return string.Format("{0}=>{1}", _network, typeof(TClass).Name);
        }
    }
}