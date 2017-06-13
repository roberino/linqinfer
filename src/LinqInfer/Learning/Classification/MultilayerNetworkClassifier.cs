using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetworkClassifier<TClass> : IFloatingPointClassifier<TClass>, IBinaryPersistable where TClass : IEquatable<TClass>
    {
        private ICategoricalOutputMapper<TClass> _outputMapper;
        private MultilayerNetwork _network;

        public MultilayerNetworkClassifier(
            ICategoricalOutputMapper<TClass> outputMapper,
            MultilayerNetwork network)
        {
            _network = network;
            _outputMapper = outputMapper;
        }

        public MultilayerNetworkClassifier(Stream input)
        {
            Load(input);
        }

        public ICategoricalOutputMapper<TClass> OutputMapper { get { return _outputMapper; } }

        public MultilayerNetwork Network { get { return _network; } }

        public ClassifyResult<TClass> ClassifyAsBestMatch(double[] vector)
        {
            var output = _network.Evaluate(new ColumnVector1D(vector));

            return _outputMapper.Map(output).OrderByDescending(x => x.Score).FirstOrDefault();
        }

        public IEnumerable<ClassifyResult<TClass>> Classify(double[] vector)
        {
            var output = _network.Evaluate(new ColumnVector1D(vector));

            return _outputMapper.Map(output).OrderByDescending(x => x.Score).ToList();
        }

        public override string ToString()
        {
            return string.Format("{0}=>{1}", _network, typeof(TClass).Name);
        }

        public void Save(Stream output)
        {
            _outputMapper.Save(output);
            _network.Save(output);
        }

        public void Load(Stream input)
        {
            if (_outputMapper == null)
                _outputMapper = new OutputMapper<TClass>(input);
            else
                _outputMapper.Load(input);
            _network = new MultilayerNetwork(input);
        }
    }
}