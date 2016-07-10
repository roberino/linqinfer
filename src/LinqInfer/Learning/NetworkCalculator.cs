using LinqInfer.Maths;
using System.Diagnostics.Contracts;

namespace LinqInfer.Learning
{
    internal static class NetworkCalculator
    {
        public static double[] AdjustWeights(ColumnVector1D inputVector, double[] weights, float learningRate)
        {
            Validate(inputVector, weights);

            //using linq: weights = weights.Zip(inputVector, (w, m) => w + learningRate * m - w).ToArray();

            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = weights[i] + learningRate * (inputVector[i] - weights[i]);
            }

            return weights;
        }

        private static void Validate(ColumnVector1D inputVector, double[] weights)
        {
            Contract.Assert(weights != null && inputVector != null);
            Contract.Assert(weights.Length == inputVector.Size);
        }
    }
}
