using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Classification
{
    public interface IAssistedLearningProcessor
    {
        void AdjustLearningRate(Func<double, double> rateAdjustment);
        double Train(ColumnVector1D inputVector, ColumnVector1D output);
    }
}