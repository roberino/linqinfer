using LinqInfer.Maths;

namespace LinqInfer.Learning
{
    public class ObjectVector<T>
    {
        public ObjectVector(T value, double[] attributes)
        {
            VirtualVector = new ColumnVector1D(attributes);
            Value = value;
        }

        public ObjectVector(T value, IVector attributes)
        {
            VirtualVector = attributes;
            Value = value;
        }

        public T Value { get; }

        public IVector VirtualVector { get; }

        public ColumnVector1D Vector => VirtualVector.ToColumnVector();
    }
}