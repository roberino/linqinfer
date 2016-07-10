using LinqInfer.Maths;

namespace LinqInfer.Learning
{
    public class ObjectVector<T>
    {
        public ObjectVector(T value, double[] attributes)
        {
            Vector = new ColumnVector1D(attributes);
            Value = value;
        }

        public ObjectVector(T value, ColumnVector1D attributes)
        {
            Vector = attributes;
            Value = value;
        }

        public T Value { get; private set; }

        public ColumnVector1D Vector { get; private set; }
    }
}