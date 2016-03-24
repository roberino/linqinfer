using LinqInfer.Learning.Features;
using LinqInfer.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Storage
{
    internal class DataSampleFeatureMap : IFloatingPointFeatureExtractor<DataItem>
    {
        private readonly DataSample _sample;
        private readonly float[] _maxSample;
        private readonly int[] _selectedFeatures;

        public DataSampleFeatureMap(DataSample sample, int[] selectedFeatures = null)
        {
            _sample = sample;
            _selectedFeatures = selectedFeatures;

            try
            {
                var fieldsLookup = _sample.Metadata.Fields.Where(f => f.Index.HasValue && f.FieldUsage == FieldUsageType.Feature).ToDictionary(f => f.Index.Value);

                if (selectedFeatures != null)
                {
                    int c = 0;
                    Labels = selectedFeatures.Select(i => fieldsLookup[i]).OrderBy(f => f.Index.Value).ToDictionary(f => f.Label, f => c++);
                }
                else
                {
                    Labels = fieldsLookup.ToDictionary(f => f.Value.Label, f => f.Key);
                }
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException("Invalid feature index array");
            }

            _maxSample = _sample.SampleData.Select(d => d.AsColumnVector()).MaxOfEachDimension().ToSingleArray();
        }

        public IDictionary<string, int> Labels { get; private set; }

        public int VectorSize
        {
            get
            {
                return Labels.Count;
            }
        }

        public float[] CreateNormalisingVector(DataItem sample = null)
        {
            return _maxSample;
        }

        public float[] ExtractVector(DataItem obj)
        {
            if (obj == null) return _maxSample;

            var arr = obj.AsColumnVector().ToSingleArray();

            if (arr.Length == VectorSize) return arr;

            return _selectedFeatures.Select(i => arr[i]).ToArray();
        }
    }
}
