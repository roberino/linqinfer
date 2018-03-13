using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class CrossEntropyLossFunction : LossFunctionBase
    {
        protected override ErrorAndLossVectors CalculateNormalVector(ColumnVector1D actualOutput, IVector targetOutput, Func<double, double> derivative)
        {
            //var result = ao.CrossCalculate(targetOutput, (y, t) =>
            //{
            //    var e = t - y;

            //    return new[]
            //    {
            //        -(t * Math.Log(y)),
            //        e * derivative(y)
            //    };
            //}, 2);

            //return new ErrorAndLossVectors()
            //{
            //    Loss = result[0],
            //    DerivativeError = result[1]
            //};

            var loss = targetOutput.MultiplyBy(actualOutput.ToColumnVector().Log());
            var error = targetOutput.ToColumnVector() - actualOutput.ToColumnVector();
            var dw = error.Calculate(actualOutput, (e, o) => e * derivative(o));

            return new ErrorAndLossVectors()
            {
                Loss = loss.ToColumnVector().Negate().Sum,
                DerivativeError = dw
            };
        }

        protected override ErrorAndLossVectors CalculateOneOfN(ColumnVector1D actualOutput, OneOfNVector targetOutput, Func<double, double> derivative)
        {
            double logLoss = 0;

            var error = actualOutput.Clone(true);

            error.Apply((y, i) =>
            {
                if (i == targetOutput.ActiveIndex)
                {
                    logLoss = -Math.Log(y);
                    return (1 - y) * derivative(y);
                }
                return -y * derivative(y);
            });

            return new ErrorAndLossVectors()
            {
                Loss = logLoss,
                DerivativeError = error
            };
        }
    }
}