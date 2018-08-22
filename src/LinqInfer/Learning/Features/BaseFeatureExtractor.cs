﻿using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class BaseFeatureExtractor<T> : IFloatingPointFeatureExtractor<T>
    {
        const string FeaturePrefix = "_feature_";

        readonly Func<T, IVector> _vectorFunc;
        readonly bool _customFeatureLabels;

        public BaseFeatureExtractor(Func<T, IVector> vectorFunc, int vectorSize, IFeature[] metadata = null)
        {
            _vectorFunc = vectorFunc;
            _customFeatureLabels = metadata != null;

            VectorSize = vectorSize;

            FeatureMetadata = metadata ?? Feature.CreateDefaults(VectorSize);
        }

        public BaseFeatureExtractor(BaseFeatureExtractor<T> other)
        {
            _vectorFunc = other._vectorFunc;
            _customFeatureLabels = other._customFeatureLabels;

            VectorSize = other.VectorSize;

            FeatureMetadata = other.FeatureMetadata;
        }

        public int VectorSize { get; }

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public double[] ExtractVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public IVector ExtractIVector(T obj)
        {
            return _vectorFunc(obj);
        }

        public virtual PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetType(this);

            if (_customFeatureLabels)
            {
                doc.AppendFeatureAttributes(FeatureMetadata.ToArray(), VectorSize);
            }

            doc.SetPropertyFromExpression(() => VectorSize);

            return doc;
        }
    }
}