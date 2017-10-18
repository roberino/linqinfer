using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;

namespace LinqInfer.Learning.Features
{
    internal class ObjectFeatureExtractor<T> : IFloatingPointFeatureExtractor<T> where T : class
    {
        private readonly IFloatingPointFeatureExtractor<T> _innerExtractor;

        public ObjectFeatureExtractor() : this(typeof(T)) { }

        private ObjectFeatureExtractor(Type actualType)
        {
            _innerExtractor = new ObjectFeatureExtractorFactory().CreateFeatureExtractor<T>(actualType, null);
        }

        public IEnumerable<IFeature> FeatureMetadata
        {
            get
            {
                return _innerExtractor.FeatureMetadata;
            }
        }

        public int VectorSize
        {
            get
            {
                return _innerExtractor.VectorSize;
            }
        }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            return _innerExtractor.ExtractColumnVector(obj);
        }

        public IVector ExtractIVector(T obj)
        {
            return ExtractColumnVector(obj);
        }

        public double[] ExtractVector(T obj)
        {
            return _innerExtractor.ExtractVector(obj);
        }

        public void Load(Stream input)
        {
            _innerExtractor.Load(input);
        }

        public void Save(Stream output)
        {
            _innerExtractor.Save(output);
        }
    }
}