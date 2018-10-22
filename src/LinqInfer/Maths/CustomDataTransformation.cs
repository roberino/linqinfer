using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using LinqInfer.Data.Serialisation;
using LinqInfer.Utility.Expressions;

namespace LinqInfer.Maths
{
    class CustomDataTransformation : ISerialisableDataTransformation
    {
        readonly Expression<Func<IVector, IVector>> _transformationExpression;
        readonly Func<IVector, IVector> _compiledFunc;

        public CustomDataTransformation(int inputSize, int outputSize, Expression<Func<IVector, IVector>> transformationExpression)
        {
            _transformationExpression = transformationExpression;
            _compiledFunc = transformationExpression.Compile();
            InputSize = inputSize;
            OutputSize = outputSize;
        }

        public int InputSize {get;}

        public int OutputSize {get;}

        public IVector Apply(IVector vector)
        {
            return _compiledFunc(vector);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetType(this);
            doc.SetPropertyFromExpression(() => InputSize);
            doc.SetPropertyFromExpression(() => InputSize);
            doc.Properties["Transformation"] = _transformationExpression.ExportAsString();

            return doc;
        }

        public static ISerialisableDataTransformation Create(PortableDataDocument doc)
        {
            var inputSize = doc.PropertyOrDefault(nameof(InputSize), 0);
            var outputSize = doc.PropertyOrDefault(nameof(OutputSize), 0);
            var transform = doc.Properties["Transformation"].AsExpression<IVector, IVector>();

            return new CustomDataTransformation(inputSize, outputSize, transform);
        }
    }
}
