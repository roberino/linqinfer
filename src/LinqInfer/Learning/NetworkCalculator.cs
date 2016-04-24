using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning
{
    internal static class NetworkCalculator
    {
        public static float CalculateDistance(float[] inputVector, float[] weights)
        {
            Validate(inputVector, weights);

            double d = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                d += Math.Pow(weights[i] - inputVector[i], 2f);
            }

            return (float)d;
        }

        public static float[] AdjustWeights(float[] inputVector, float[] weights, float learningRate)
        {
            Validate(inputVector, weights);

            //using linq: weights = weights.Zip(inputVector, (w, m) => w + learningRate * m - w).ToArray();

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = weights[i] + learningRate * (inputVector[i] - weights[i]);
            }

            return weights;
        }

        private static void Validate(float[] inputVector, float[] weights)
        {
            Contract.Assert(weights != null && inputVector != null);
            Contract.Assert(weights.Length == inputVector.Length);
        }
    }
}
