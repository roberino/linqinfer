using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class SquareLossFunction : LossFunctionBase
    {
        protected override NetworkError CalculateNormalVector(ColumnVector1D actualOutput, IVector targetOutput, Func<double, double> derivative)
        {
            var error = (targetOutput.ToColumnVector() - actualOutput.ToColumnVector());

            var dw = error.Calculate(actualOutput, (e, o) => e * derivative(o));

            return new NetworkError()
            {
                Loss = error.Sq().Sum,
                DerivativeError = dw
            };
        }

        protected override NetworkError CalculateOneOfN(ColumnVector1D actualOutput, OneOfNVector targetOutput, Func<double, double> derivative)
        {
            var error = actualOutput.Clone(true);

            error.Apply((y, i) =>
            {
                if (i == targetOutput.ActiveIndex)
                {
                    return (1 - y) * derivative(y);
                }
                return -y * derivative(y);
            });

            return new NetworkError()
            {
                Loss = error.Sq().Sum,
                DerivativeError = error
            };
        }
    }
}