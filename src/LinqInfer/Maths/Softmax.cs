using LinqInfer.Maths;

namespace LinqInfer.Maths
{
    public class Softmax : IVectorTransformation
    {
        public Softmax(int vectorSize)
        {
            InputSize = vectorSize;
        }

        public int InputSize { get; }

        public int OutputSize => InputSize;

        public Vector Apply(Vector vector)
        {
            ColumnVector1D input = vector is ColumnVector1D ? (ColumnVector1D)vector : new ColumnVector1D(vector);

            return Calculate(input);
        }

        // x => exp(x) / sum(exp(x))
        public ColumnVector1D Calculate(ColumnVector1D inputs)
        {
            var exp = inputs.Exp();

            return exp / exp.Sum();
        }
    }
}