using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification
{
    public interface IAssistedLearningProcessor
    {
        void AdjustLearningRate(Func<double, double> rateAdjustment);
        double Train(IVector inputVector, IVector output);
    }
}