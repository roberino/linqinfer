using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public static class LossFunctions
    {
        public static ILossFunction Parse(string functionName)
        {
            switch (functionName)
            {
                case nameof(SquareLossFunction):
                    return Square;
                case nameof(CrossEntropyLossFunction):
                    return CrossEntropy;
            }

            throw new NotSupportedException(functionName);
        }

        public static readonly ILossFunction CrossEntropy = new CrossEntropyLossFunction();

        public static readonly ILossFunction Square = new SquareLossFunction();

        private class CrossEntropyLossFunction : ILossFunction
        {
            public ErrorAndLossVectors Calculate(IVector actualOutput, IVector targetOutput)
            {
                var loss = targetOutput.MultiplyBy(actualOutput.ToColumnVector().Log());

                return new ErrorAndLossVectors()
                {
                    Loss = loss.ToColumnVector().Negate(),
                    PredictionError = targetOutput.ToColumnVector() - actualOutput.ToColumnVector()
                };
            }

            public ErrorAndLoss Calculate(double actualOutput, double targetOutput)
            {
                var loss = -targetOutput * Math.Log(actualOutput);
                var e = targetOutput - actualOutput;

                return new ErrorAndLoss() { Loss = loss, PredictionError = e };
            }
        }

        private class SquareLossFunction : ILossFunction
        {
            public ErrorAndLossVectors Calculate(IVector actualOutput, IVector targetOutput)
            {
                var error = (targetOutput.ToColumnVector() - actualOutput.ToColumnVector());

                return new ErrorAndLossVectors()
                {
                    Loss = error.Sq(),
                    PredictionError = error
                };
            }

            public ErrorAndLoss Calculate(double actualOutput, double targetOutput)
            {
                var e = targetOutput - actualOutput;

                return new ErrorAndLoss() { Loss = e * e, PredictionError = e };
            }
        }
    }
}