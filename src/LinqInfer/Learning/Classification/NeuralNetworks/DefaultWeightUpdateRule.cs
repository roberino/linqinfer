using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class DefaultWeightUpdateRule : IWeightUpdateRule
    {
        private readonly double _momentum;
        private double _learningRate;

        public DefaultWeightUpdateRule(double learningRate, double momentum)
        {
            _learningRate = ArgAssert.AssertBetweenZeroAndOneUpperInclusive(learningRate, nameof(learningRate));
            _momentum = ArgAssert.AssertBetweenZeroAndOneUpperInclusive(momentum, nameof(momentum));
        }

        public static IWeightUpdateRule Create(double? learningRate = null, double? momentum = null)
        {
            return new DefaultWeightUpdateRule(learningRate.GetValueOrDefault(LearningParameters.DefaultLearningRate), momentum.GetValueOrDefault(0.1));
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
                (_learningRate * ((_momentum * updateParams.CurrentWeightValue) +
                ((1.0 - _momentum) * (updateParams.Error * updateParams.PreviousLayerOutput))));
        }

        public string Export()
        {
            return new FunctionFormatter().Format(this, _learningRate, _momentum);
        }

        public override string ToString()
        {
            return Export();
        }
    }
}