using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace LinqInfer.Data.Sampling
{
    internal class DataSampleFeatureMap : IFloatingPointFeatureExtractor<DataItem>
    {
        private readonly DataSample _sample;
        private readonly int[] _selectedFeatures;
        private double[] _maxSample;

        public DataSampleFeatureMap(DataSample sample, int[] selectedFeatures = null)
        {
            _sample = sample;
            _selectedFeatures = selectedFeatures;

            try
            {
                int c = 0;

                var fieldsLookup = _sample
                    .Metadata
                    .Fields
                    .Where(f => f.Index.HasValue && f.FieldUsage == FieldUsageType.Feature)
                    .OrderBy(f => f.Index.Value)
                    .ToDictionary(f => c++);

                if (selectedFeatures != null)
                {
                    c = 0;
                    IndexLookup = selectedFeatures.Select(i => fieldsLookup[i]).OrderBy(f => f.Index.Value).ToDictionary(f => f.Label, f => c++);
                }
                else
                {
                    IndexLookup = fieldsLookup.ToDictionary(f => f.Value.Label, f => f.Key);
                    _selectedFeatures = IndexLookup.Select(x => x.Value).ToArray();
                }

                FeatureMetadata = IndexLookup                    
                    .Select(l =>
                    new Feature()
                    {
                        Key = l.Key,
                        Label = l.Key,
                        Index = l.Value,
                        DataType = fieldsLookup[l.Value].DataType,
                        Model = fieldsLookup[l.Value].DataModel,
                    })
                   .ToList();
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException("Invalid feature index array");
            }

            NormaliseUsing(_sample.SampleData);
        }

        public bool IsNormalising { get { return true; } }

        public IDictionary<string, int> IndexLookup { get; private set; }

        public int VectorSize
        {
            get
            {
                return IndexLookup.Count;
            }
        }

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

        public double[] CreateNormalisingVector(DataItem sample = null)
        {
            return _maxSample;
        }

        public double[] NormaliseUsing(IEnumerable<DataItem> samples)
        {
            if(samples.Any())
                _maxSample = samples.Select(d => d.AsColumnVector()).MaxOfEachDimension().ToDoubleArray();

            return _maxSample;
        }

        public double[] ExtractVector(DataItem obj)
        {
            if (obj == null) return _maxSample;

            var arr = obj.AsColumnVector().ToDoubleArray();

            if (arr.Length == VectorSize) return arr;

            return _selectedFeatures.Select(i => arr[i]).ToArray();
        }

        public ColumnVector1D ExtractColumnVector(DataItem obj)
        {
            if (obj == null) return new ColumnVector1D(_maxSample);

            var arr = obj.AsColumnVector();

            if (arr.Size == VectorSize) return arr;

            return new ColumnVector1D(_selectedFeatures.Select(i => arr[i]).ToArray());
        }

        public void Save(Stream output)
        {
            var doc = new BinaryVectorDocument();

            if (_maxSample != null)
                doc.Vectors.Add(new ColumnVector1D(_maxSample));

            doc.Save(output);
        }

        public void Load(Stream input)
        {
            var doc = new BinaryVectorDocument();

            doc.Load(input);

            if (doc.Vectors.Any())
            {
                var nv = doc.Vectors.First();

                _maxSample = nv.ToDoubleArray();
            }
        }
    }
}