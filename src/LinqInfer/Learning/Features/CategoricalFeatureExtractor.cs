using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using LinqInfer.Utility.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Features
{
    public class CategoricalFeatureExtractor<TInput, TCategory> : IFloatingPointFeatureExtractor<TInput>
    {
        readonly OneHotEncoding<TCategory> _encoder;
        readonly Func<TInput, TCategory> _categorySelector;
        readonly Expression<Func<TInput, TCategory>> _categorySelectorExp;

        internal CategoricalFeatureExtractor(Expression<Func<TInput, TCategory>> categorySelector, IEnumerable<IFeature> features, ISet<TCategory> categories = null)
            :this (categorySelector, features, new OneHotEncoding<TCategory>(categories ?? new HashSet<TCategory>()))
        {
        }

        CategoricalFeatureExtractor(Expression<Func<TInput, TCategory>> categorySelector, IEnumerable<IFeature> features, OneHotEncoding<TCategory> encoder)
        {
            _encoder = encoder;
            _categorySelectorExp = categorySelector;
            _categorySelector = categorySelector.Compile();
            FeatureMetadata = features;
        }

        public int VectorSize => _encoder.VectorSize;

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public IVector ExtractIVector(TInput obj)
        {
            return _encoder.Encode(_categorySelector(obj));
        }

        public double[] ExtractVector(TInput obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            var encDoc = _encoder.ExportData();

            doc.SetType(this);

            doc.Properties["CategorySelector"] = _categorySelectorExp.ExportAsString();

            doc.AppendFeatureAttributes(FeatureMetadata.ToArray(), VectorSize);

            doc.Children.Add(encDoc);

            return doc;
        }

        public static CategoricalFeatureExtractor<TInput, TCategory> Create(PortableDataDocument data)
        {
            var enc = OneHotEncoding<TCategory>.ImportData(data.Children.First());

            var features = data.LoadFeatureAttributes();

            var exp = data.Properties["CategorySelector"].AsExpression<TInput, TCategory>();

            return new CategoricalFeatureExtractor<TInput, TCategory>(exp, features.features, enc);
        }
    }
}