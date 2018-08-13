using System;

namespace LinqInfer.Maths
{
    class DelegateVectorTransformation : IVectorTransformation
    {
        readonly Func<Vector, Vector> _transformation;

        public DelegateVectorTransformation(int inputVectorSize, Func<Vector, Vector> transformation)
        {
            InputSize = inputVectorSize;

            _transformation = transformation;

            OutputSize = _transformation(new Vector(new double[inputVectorSize])).Size;
        }

        public DelegateVectorTransformation(int inputVectorSize, Func<double[], double[]> transformation)
            : this(inputVectorSize, new Func<Vector, Vector>(v => new Vector(transformation(v.GetUnderlyingArray()))))
        {
        }

        public int InputSize { get; private set; }

        public int OutputSize { get; private set; }

        public IVector Apply(IVector vector)
        {
            return _transformation(vector.ToColumnVector());
        }
    }
}