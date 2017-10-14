using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace LinqInfer.Learning.Classification
{
    public class LinearClassifier
    {
        private readonly Matrix _weights;
        private readonly ColumnVector1D _bias;
        private readonly Func<ColumnVector1D> _lossFunction;
        private readonly ColumnVector1D _delta;

        public LinearClassifier(int inputVectorSize, int outputVectorSize, double delta = 1)
        {
            Contract.Ensures(inputVectorSize > 0);
            Contract.Ensures(outputVectorSize > 0);

            _delta = new ColumnVector1D(Vector.UniformVector(_weights.Width, delta));

            _weights = new Matrix(Enumerable.Range(0, inputVectorSize).Select(n => Functions.RandomVector(outputVectorSize, -0.1, 0.1)));
            _bias = Functions.RandomVector(outputVectorSize);
        }

        public int InputVectorSize => _weights.Height;

        public int OutputVectorSize => _weights.Width;

        public ColumnVector1D Evaluate(ColumnVector1D input)
        {
            var output = _weights * input + _bias;

            return output;
        }

        public ColumnVector1D CalculateError(ColumnVector1D input, ColumnVector1D output)
        {
            var score = Evaluate(input);

            var error = (score - output + _delta);

            error.Apply(x => Math.Max(x, 0));

            return error;
        }
    }
}
