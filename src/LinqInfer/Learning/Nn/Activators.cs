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
                Activator = SigmoidF(alpha),
                Derivative = SigmoidDerivative(alpha),
                Parameter = alpha,
                Create = (p) => Sigmoid(p)
            };
        }

        private static Func<double, double> SigmoidF(double alpha = 1)
        {
            return x => (1 / (1 + Math.Exp(-alpha * x)));
        }

        private static Func<double, double> SigmoidDerivative(double alpha = 1)
        {
            return x => (alpha * x * (1 - x));
        }
    }
}