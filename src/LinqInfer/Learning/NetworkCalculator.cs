using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning
{
    internal static class NetworkCalculator
    {
        public static double CalculateDistance(double[] inputVector, double[] weights)
        {
            Validate(inputVector, weights);

            double d = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                d += Math.Pow(weights[i] - inputVector[i], 2f);
            }

            return d;
        }

        public static double[] AdjustWeights(double[] inputVector, double[] weights, float learningRate)
        {
            Validate(inputVector, weights);

            //using linq: weights = weights.Zip(inputVector, (w, m) => w + learningRate * m - w).ToArray();

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = weights[i] + learningRate * (inputVector[i] - weights[i]);
            }

            return weights;
        }

        private static void Validate(double[] inputVector, double[] weights)
        {
            Contract.Assert(weights != null && inputVector != null);
            Contract.Assert(weights.Length == inputVector.Length);
        }
    }
}
