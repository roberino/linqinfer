using LinqInfer.Data;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Maths
{
    public class Softmax : ISerialisableDataTransformation
    {
        internal Softmax()
        {
        }

        public Softmax(int vectorSize)
        {
            InputSize = vectorSize;
        }

        public int InputSize { get; private set; }

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

        public void FromDataDocument(PortableDataDocument doc)
        {
            InputSize = doc.PropertyOrDefault(() => InputSize, 0);
        }

        public PortableDataDocument ToDataDocument()
        {
            var doc = new PortableDataDocument();

            doc.SetType(this);
            doc.SetPropertyFromExpression(() => InputSize);

            return doc;
        }
    }
}