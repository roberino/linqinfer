using System;

namespace LinqInfer.Learning.Nn
{
    public static class Activators
    {
        public static ActivatorFunc Sigmoid(double alpha = 2)
        {
            return new ActivatorFunc()
            {
                Name = "Sigmoid",
                Activator = x => (1 / (1 + Math.Exp(-alpha * x))),
                Derivative = x => (alpha * x * (1 - x)),
                Parameter = alpha,
                Create = (p) => Sigmoid(p)
            };
        }

        public static ActivatorFunc Threshold(double threshold = 0.5)
        {
            return new ActivatorFunc()
            {
                Name = "Threshold",
                Activator = x => x > threshold ? 1 : 0,
                Derivative = x => 0,
                Parameter = threshold,
                Create = (p) => Threshold(p)
            };
        }
    }
}