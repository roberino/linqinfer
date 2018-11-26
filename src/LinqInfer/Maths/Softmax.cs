using System;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Maths
{
    public class Softmax : ISerialisableDataTransformation
    {
        public Softmax(int vectorSize)
        {
            InputSize = vectorSize;
        }

        public static Func<int, ISerialisableDataTransformation> Factory => n => new Softmax(n);

        public int InputSize { get; }

        public int OutputSize => InputSize;

        public IVector Apply(IVector vector)
        {
            return Calculate(vector.ToColumnVector());
        }

        // x => exp(x) / sum(exp(x))
        public ColumnVector1D Calculate(ColumnVector1D inputs)
        {
            var shifted = new ColumnVector1D(inputs.Shift(inputs.Max));

            var exp = shifted.Exp();

            return exp / exp.Sum;
        }

        public static ISerialisableDataTransformation Create(PortableDataDocument doc)
        {
            return new Softmax(doc.PropertyOrDefault(nameof(InputSize), 0));
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetType(this);
            doc.SetPropertyFromExpression(() => InputSize);

            return doc;
        }
    }
}