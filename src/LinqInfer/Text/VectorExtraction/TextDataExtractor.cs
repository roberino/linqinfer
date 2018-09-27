using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Text.VectorExtraction
{
    class TextDataExtractor : IFloatingPointFeatureExtractor<IEnumerable<IToken>>, IExportableAsDataDocument, IImportableFromDataDocument
    {
        const int EXTENDED_FEATURE_COUNT = 4;
        readonly IList<IFeature> _features;
        readonly IDictionary<string, int> _words;

        bool _normalise;
        int[] _normalisingFrequencies;
        int _normalisingFrequencyDefault;

        internal TextDataExtractor(bool normalise = true)
        {
            _words = new Dictionary<string, int>();
            _features = new List<IFeature>();
            _normalisingFrequencies = Enumerable.Range(1, VectorSize).Select(_ => 1).ToArray();
            _normalisingFrequencyDefault = 1000;
            _normalise = normalise;
        }

        internal TextDataExtractor(IEnumerable<string> words, int normalisingFrequency, bool normalise = true) : this(words, Enumerable.Range(1, words.Count() + EXTENDED_FEATURE_COUNT).Select(_ => normalisingFrequency).ToArray(), normalise)
        {
        }

        internal TextDataExtractor(IEnumerable<string> words, int[] normalisingFrequencies, bool normalise = true)
        {
            int i = 0;

            _normalisingFrequencies = normalisingFrequencies;
            _normalisingFrequencyDefault = normalisingFrequencies.Sum();
            _normalise = normalise;

            _words = words
                .ToDictionary(w => w, _ => i++);

            _features = new List<IFeature>();

            SetupFeatures();
        }

        public IFloatingPointFeatureExtractor<T> CreateObjectTextVectoriser<T>(Func<T, IEnumerable<IToken>> tokeniser) where T : class
        {
            return _normalise ? new ObjectTextDataExtractor<T>(tokeniser, _words.Keys, _normalisingFrequencies) : new ObjectTextDataExtractor<T>(tokeniser, _words.Keys);
        }

        public int VectorSize
        {
            get
            {
                return _words.Count + EXTENDED_FEATURE_COUNT;
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

        public IVector ExtractIVector(IEnumerable<IToken> tokens)
        {
            return ExtractColumnVector(tokens);
        }

        public double[] ExtractVector(IEnumerable<IToken> tokens)
        {
            var vectorRaw = ExtractVectorDenormal(tokens);

            if (!_normalise) return vectorRaw;
            
            int i = 0;

            return vectorRaw
                .Select(v => new
                {
                    value = v,
                    nf = GetNormalisingFrequency(i++)
                })
                .Select(v => v.value == 0 ? 0d :
                    Math.Log((Math.Min(v.value + 1, v.nf) / v.nf * 9d + 1)
                    , 10))
                .ToArray();
        }

        int GetNormalisingFrequency(int index)
        {
            var x = _normalise ? (index < _normalisingFrequencies.Length) ? _normalisingFrequencies[index] : _normalisingFrequencyDefault : 1;

            return x == 0 ? 1 : x;
        }

        double[] ExtractVectorDenormal(IEnumerable<IToken> tokens)
        {
            var vectorRaw = new double[VectorSize];

            foreach (var token in tokens)
            {
                int i = 0;

                if (_words.TryGetValue(token.Text, out i))
                {
                    vectorRaw[i]++;
                }
                else
                {
                    switch (token.Type)
                    {
                        case TokenType.Word:
                            vectorRaw[VectorSize - 1] += 1;
                            break;
                        case TokenType.Symbol:
                            vectorRaw[VectorSize - 2] += 1;
                            break;
                        case TokenType.Number:
                            vectorRaw[VectorSize - 3] += 1;
                            break;
                        case TokenType.SentenceEnd:
                            vectorRaw[VectorSize - 4] += 1;
                            break;
                    }
                }
            }

            return vectorRaw;
        }

        public double[] NormaliseUsing(IEnumerable<IEnumerable<IToken>> samples)
        {
            _normalisingFrequencies = samples.Select(s => new ColumnVector1D(ExtractVectorDenormal(s))).MaxOfEachDimension().Select(v => (int)v).ToArray();
            _normalise = true;

            return _normalisingFrequencies.Select(d => (double)d).ToArray(); // Enumerable.Range(0, VectorSize).Select(n => 1d).ToArray();
        }

        public void Save(Stream output)
        {
            ExportData().Save(output);
        }

        public void Load(Stream input)
        {
            var doc = new PortableDataDocument(input);

            ImportData(doc);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            foreach(var word in _words)
            {
                doc.Properties[word.Key] = word.Value.ToString();
            }

            doc.Properties["_Type"] = "text";
            doc.Properties["_NormalisingFrequencyDefault"] = _normalisingFrequencyDefault.ToString();

            doc.Vectors.Add(new ColumnVector1D(_normalisingFrequencies.Select(f => (double)f).ToArray()));

            return doc;
        }

        public void ImportData(PortableDataDocument doc)
        {
            foreach (var prop in doc.Properties.Where(p => !p.Key.StartsWith("_")))
            {
                int v;

                if (int.TryParse(prop.Value, out v))
                {
                    _words[prop.Key] = v;
                }
            }

            _normalisingFrequencyDefault = doc.PropertyOrDefault("_NormalisingFrequencyDefault", 1);

            _normalisingFrequencies = Enumerable.Range(0, _words.Count + EXTENDED_FEATURE_COUNT).Select(_ => 1).ToArray();

            if (doc.Vectors.Any())
            {
                int i = 0;

                foreach (var f in doc.Vectors.First().ToColumnVector())
                {
                    _normalisingFrequencies[i++] = (int)f;
                }
            }

            SetupFeatures(true);
        }

        void SetupFeatures(bool ignoreExisting = false)
        {
            if (!ignoreExisting && _features.Count > 0) throw new InvalidOperationException();

            if (ignoreExisting) _features.Clear();

            foreach (var f in _words.Select(w =>
                  (IFeature)new Feature() { DataType = TypeCode.String, Index = w.Value, Key = w.Key, Label = w.Key, Model = FeatureVectorModel.Magnitudinal }))
            {
                _features.Add(f);
            }

            _features.Add(new Feature() { DataType = TypeCode.String, Index = _features.Count, Key = "_UWC", Label = "Unknown Word Count" });
            _features.Add(new Feature() { DataType = TypeCode.String, Index = _features.Count, Key = "_SC", Label = "Symbol Count" });
            _features.Add(new Feature() { DataType = TypeCode.String, Index = _features.Count, Key = "_NC", Label = "Number Count" });
            _features.Add(new Feature() { DataType = TypeCode.String, Index = _features.Count, Key = "_SE", Label = "Sentence End" });
        }
    }
}