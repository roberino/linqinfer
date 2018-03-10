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

        public static ILossFunction CrossEntropy { get; } = new CrossEntropyLossFunction();

        public static ILossFunction Square { get; } = new SquareLossFunction();
    }
}