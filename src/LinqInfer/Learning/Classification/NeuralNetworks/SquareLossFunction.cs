using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class SquareLossFunction : LossFunctionBase
    {
        protected override ErrorAndLossVectors CalculateNormalVector(ColumnVector1D actualOutput, IVector targetOutput, Func<double, double> derivative)
        {
            var error = (targetOutput.ToColumnVector() - actualOutput.ToColumnVector());

            var dw = error.Calculate(actualOutput, (e, o) => e * derivative(o));

            return new ErrorAndLossVectors()
            {
                Loss = error.Sq(),
                DerivativeError = dw
            };
        }

        protected override ErrorAndLossVectors CalculateOneOfN(ColumnVector1D actualOutput, OneOfNVector targetOutput, Func<double, double> derivative)
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

            return new ErrorAndLossVectors()
            {
                Loss = error.Sq(),
                DerivativeError = error
            };
        }
    }
}