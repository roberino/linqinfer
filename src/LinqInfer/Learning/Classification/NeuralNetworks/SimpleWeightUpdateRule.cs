using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class SimpleWeightUpdateRule : IWeightUpdateRule
    {
        private double _learningRate;

        public SimpleWeightUpdateRule(double learningRate)
        {
            _learningRate = ArgAssert.AssertBetweenZeroAndOneUpperInclusive(learningRate, nameof(learningRate));
        }

        public static IWeightUpdateRule Create(double? learningRate = null)
        {
            return new SimpleWeightUpdateRule(learningRate.GetValueOrDefault(LearningParameters.DefaultLearningRate));
        }

        public double AdjustLearningRate(Func<double, double> rateAdjustment)
        {
            var lr = rateAdjustment(_learningRate);

            ArgAssert.AssertBetweenZeroAndOneUpperInclusive(lr, nameof(rateAdjustment), "Invalid rate adjustment");

            _learningRate = lr;

            return lr;
        }

        public double Execute(WeightUpdateParameters updateParams)
        {
            return updateParams.CurrentWeightValue +
                   (_learningRate * updateParams.CurrentWeightValue * updateParams.Error);
        }

        public string Export()
        {
            return new FunctionFormatter().Format(this, _learningRate);
        }

        public override string ToString()
        {
            return Export();
        }
    }
}