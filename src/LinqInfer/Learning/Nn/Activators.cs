using System;

namespace LinqInfer.Learning.Nn
{
    public static class Activators
    {
        public static Func<double, double> Sigmoid(double alpha = 2)
        {
            return x => (1 / (1 + Math.Exp(-alpha * x)));
        }
    }
}