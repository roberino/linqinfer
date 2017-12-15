using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning
{
    public class ObjectVector<T>
    {
        private Lazy<IVector> _vector;

        public ObjectVector(T value, double[] attributes)
        {
            _vector = new Lazy<IVector>(() => new ColumnVector1D(attributes));
            Value = value;
        }

        public ObjectVector(T value, IVector attributes)
        {
            _vector = new Lazy<IVector>(() => attributes);
            Value = value;
        }

        public ObjectVector(T value, Func<T, IVector> converter)
        {
            _vector = new Lazy<IVector>(() => converter(Value));
            Value = value;
        }

        public T Value { get; }

        public IVector VirtualVector => _vector.Value;

        public ColumnVector1D Vector => VirtualVector.ToColumnVector();
    }
}