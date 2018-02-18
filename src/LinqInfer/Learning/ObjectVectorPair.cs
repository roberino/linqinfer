using LinqInfer.Maths;
using System;
using System.Diagnostics;

namespace LinqInfer.Learning
{
    [DebuggerDisplay("Obj={Value} Data={Vector}")]
    public class ObjectVectorPair<T>
    {
        private Lazy<IVector> _vector;
        private Lazy<ColumnVector1D> _colVector;

        public ObjectVectorPair(T value, double[] attributes) : this(value, new ColumnVector1D(attributes))
        {
        }

        public ObjectVectorPair(T value, IVector attributes) : this(value, _ => attributes)
        {
        }

        public ObjectVectorPair(T value, Func<T, IVector> converter)
        {
            _vector = new Lazy<IVector>(() => converter(Value));
            _colVector = new Lazy<ColumnVector1D>(() => _vector.Value.ToColumnVector());
            Value = value;
        }

        public T Value { get; }

        public IVector Vector => _vector.Value;

        public ColumnVector1D ColumnVector => _colVector.Value;
    }
}