using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    public interface IAssistedLearningProcessor
    {
        void AdjustLearningRate(Func<double, double> rateAdjustment);

        double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData, Func<int, double, bool> haltingFunction);

        double Train(IVector inputVector, IVector output);
    }
}