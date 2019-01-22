using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Features
{
    public class CategoricalFeatureExtractor<TInput, TCategory> : IFloatingPointFeatureExtractor<TInput>, IHasCategoricalEncoding<TCategory>
    {
        readonly Func<TInput, TCategory> _categorySelector;
        readonly Expression<Func<TInput, TCategory>> _categorySelectorExp;

        internal CategoricalFeatureExtractor(Expression<Func<TInput, TCategory>> categorySelector,
            ISet<TCategory> categories)
            : this(categorySelector, new OneHotEncoding<TCategory>(categories))
        {
        }

        internal CategoricalFeatureExtractor(Expression<Func<TInput, TCategory>> categorySelector, int maxVectorSize)
            : this(categorySelector, new OneHotEncoding<TCategory>(maxVectorSize))
        {
        }

        CategoricalFeatureExtractor(Expression<Func<TInput, TCategory>> categorySelector, OneHotEncoding<TCategory> encoder)
        {
            _categorySelectorExp = categorySelector;
            _categorySelector = categorySelector.Compile();
            Encoder = encoder;
        }

        public int VectorSize => Encoder.VectorSize;

        public IEnumerable<IFeature> FeatureMetadata =>
            Encoder.IndexTable.Select(category => new Feature()
            {
                Index = category.Value,
                Key = $"key{category.Value}",
                Label = category.Key.ToString(),
                Model = FeatureVectorModel.Categorical
            });

        public IOneHotEncoding<TCategory> Encoder { get; }

        public IVector ExtractIVector(TInput obj)
        {
            return Encoder.Encode(_categorySelector(obj));
        }

        public double[] ExtractVector(TInput obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public PortableDataDocument ExportData()
        {
            var doc = Encoder.ExportData();

            doc.SetType(this);

            doc.Properties["CategorySelector"] = _categorySelectorExp.ExportAsString();

            return doc;
        }

        public static CategoricalFeatureExtractor<TInput, TCategory> Create(PortableDataDocument data)
        {
            var enc = OneHotEncoding<TCategory>.ImportData(data);

            var exp = data.Properties["CategorySelector"].AsExpression<TInput, TCategory>();

            return new CategoricalFeatureExtractor<TInput, TCategory>(exp, enc);
        }
    }
}