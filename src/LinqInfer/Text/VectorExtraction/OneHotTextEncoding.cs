using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqInfer.Utility.Expressions;

namespace LinqInfer.Text.VectorExtraction
{
    class OneHotTextEncoding<T> : IVectorFeatureExtractor<T>
    {
        readonly Expression<Func<T, string[]>> _objectToStringTransformExpression;
        readonly Func<T, string[]> _objectToStringTransform;

        OneHotTextEncoding(IOneHotEncoding<string> encoding, Expression<Func<T, string[]>> objectToStringTransform)
        {
            _objectToStringTransformExpression = objectToStringTransform;
            _objectToStringTransform = objectToStringTransform.Compile();
            Encoder = encoding;
            FeatureMetadata = Feature.CreateDefaults(encoding.IndexTable.Select(x => x.Key), FeatureVectorModel.Categorical);
        }

        public OneHotTextEncoding(ISemanticSet vocabulary, Expression<Func<T, string[]>> objectToStringTransform)
            : this(new OneHotEncoding<string>(new HashSet<string>(vocabulary.Words)), objectToStringTransform)
        {
        }

        public IOneHotEncoding<string> Encoder { get; }

        public int VectorSize => Encoder.VectorSize;

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public bool CanEncode(T obj) => true;

        public IVector ExtractIVector(T obj)
        {
            return ExtractOneOfNVector(_objectToStringTransform(obj));
        }

        public IVector ExtractOneOfNVector(string[] tokens)
        {
            return Encoder.Encode(tokens);
        }

        public PortableDataDocument ExportData()
        {
            var doc = Encoder.ExportData();

            doc.SetType(this);
            doc.Properties["Expression"] = _objectToStringTransformExpression.ExportExpression();

            return doc;
        }

        public static OneHotTextEncoding<T> Create(PortableDataDocument doc)
        {
            var encoding = OneHotEncoding<string>.ImportData(doc);

            var expression = doc.Properties["Expression"].AsExpression<T, string[]>();

            var textEncoding = new OneHotTextEncoding<T>(encoding, expression);

            return textEncoding;
        }
    }
}