using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Text.VectorExtraction
{
    internal class TextVectorExtractor : IFloatingPointFeatureExtractor<IEnumerable<IToken>>
    {
        private readonly IList<IFeature> _features;
        private readonly IDictionary<string, int> _words;
        private int _normalisingFrequency;

        internal TextVectorExtractor()
        {
            _words = new Dictionary<string, int>();
            _features = new List<IFeature>();
            _normalisingFrequency = 1;
        }

        internal TextVectorExtractor(IEnumerable<string> words, int normalisingFrequency)
        {
            int i = 0;

            _normalisingFrequency = normalisingFrequency;

            _words = words
                .ToDictionary(w => w, _ => i++);

            _features = new List<IFeature>();

            SetupFeatures();
        }

        public IFloatingPointFeatureExtractor<T> CreateObjectTextVectoriser<T>(Func<T, IEnumerable<IToken>> tokeniser) where T : class
        {
            return new ObjectTextVectorExtractor<T>(tokeniser, _words.Keys, _normalisingFrequency);
        }

        public int VectorSize
        {
            get
            {
                return _words.Count;
            }
        }

        public bool IsNormalising
        {
            get
            {
                return true;
            }
        }

        public IEnumerable<IFeature> FeatureMetadata
        {
            get
            {
                return _features;
            }
        }

        public ColumnVector1D ExtractColumnVector(IEnumerable<IToken> tokens)
        {
            return new ColumnVector1D(ExtractVector(tokens));
        }

        public double[] ExtractVector(IEnumerable<IToken> tokens)
        {
            var vectorRaw = new int[VectorSize];

            foreach (var token in tokens)
            {
                int i = 0;

                if (_words.TryGetValue(token.Text, out i))
                {
                    vectorRaw[i]++;
                }
            }

            var nf = (double)_normalisingFrequency;

            return vectorRaw
                .Select(v => v == 0 ? 0d :
                    Math.Log((Math.Min(v + 1, nf)) / nf * 10d
                    , 10))
                .ToArray();
        }

        public double[] NormaliseUsing(IEnumerable<IEnumerable<IToken>> samples)
        {
            return Enumerable.Range(0, VectorSize).Select(n => 1d).ToArray();
        }

        public void Save(Stream output)
        {
            var ds = DictionarySerialiserFactory.ForInstance(_words);

            ds.Write(_words, output);

            using (var writer = new BinaryWriter(output))
            {
                writer.Write(_normalisingFrequency);
            }
        }

        public void Load(Stream input)
        {
            var ds = DictionarySerialiserFactory.ForInstance(_words);

            var words = ds.Read(input);

            foreach (var w in words)
            {
                _words[w.Key] = w.Value;
            }

            using (var reader = new BinaryReader(input))
            {
                _normalisingFrequency = reader.ReadInt32();
            }

            SetupFeatures();
        }

        private void SetupFeatures()
        {
            if (_features.Count > 0) throw new InvalidOperationException();

            foreach (var f in _words.Select(w =>
                  (IFeature)new Feature() { DataType = TypeCode.String, Index = w.Value, Key = w.Key, Label = w.Key, Model = Maths.Probability.DistributionModel.Unknown }))
            {
                _features.Add(f);
            }
        }
    }
}