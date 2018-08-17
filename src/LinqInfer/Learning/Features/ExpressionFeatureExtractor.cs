using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility.Expressions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Features
{
    class ExpressionFeatureExtractor<T> : BaseFeatureExtractor<T>
    {
        readonly Expression<Func<T, IVector>> _vectorExpression;

        public ExpressionFeatureExtractor(Expression<Func<T, IVector>> vectorExpression, int vectorSize,
            IFeature[] metadata = null)
            : base(vectorExpression.Compile(), vectorSize, metadata)
        {
            _vectorExpression = vectorExpression;
        }

        public override PortableDataDocument ExportData()
        {
            var doc = base.ExportData();

            doc.Properties["Extractor"] = _vectorExpression.ExportAsString();

            return doc;
        }

        public static ExpressionFeatureExtractor<T> Create(PortableDataDocument data)
        {
            var attribs = data.LoadFeatureAttributes();
            
            var exp = data.Properties["Extractor"].AsExpression<T, IVector>();
            
            return attribs.features.Any()
                ? new ExpressionFeatureExtractor<T>(exp, attribs.vectorSize, attribs.features)
                : new ExpressionFeatureExtractor<T>(exp, attribs.vectorSize);
        }
    }
}