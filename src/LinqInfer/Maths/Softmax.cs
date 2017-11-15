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

        public IVector Apply(IVector vector)
        {
            return Calculate(vector.ToColumnVector());
        }

        // x => exp(x) / sum(exp(x))
        public ColumnVector1D Calculate(ColumnVector1D inputs)
        {
            var shifted = new ColumnVector1D(inputs.Shift(inputs.Max));

            var exp = shifted.Exp();

            return exp / exp.Sum();
        }
    }
}