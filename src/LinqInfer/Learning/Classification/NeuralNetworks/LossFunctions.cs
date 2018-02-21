using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public static class LossFunctions
    {
        public static ILossFunction Parse(string functionName)
        {
            switch (functionName)
            {
                case nameof(DefaultLossFunction):
                    return Default;
                case nameof(CrossEntropyLossFunction):
                    return CrossEntropy;
            }

            throw new NotSupportedException(functionName);
        }

        public static readonly ILossFunction CrossEntropy = new CrossEntropyLossFunction();

        public static readonly ILossFunction Default = new DefaultLossFunction();

        private class CrossEntropyLossFunction : ILossFunction
        {
            public ErrorAndLoss Calculate(double actualOutput, double targetOutput)
            {
                var loss = targetOutput * Math.Log(actualOutput);
                var e = targetOutput - actualOutput;

                return new ErrorAndLoss() { Loss = loss, PredictionError = e };
            }
        }

        private class DefaultLossFunction : ILossFunction
        {
            public ErrorAndLoss Calculate(double actualOutput, double targetOutput)
            {
                var e = targetOutput - actualOutput;

                return new ErrorAndLoss() { Loss = e * e, PredictionError = e };
            }
        }
    }
}