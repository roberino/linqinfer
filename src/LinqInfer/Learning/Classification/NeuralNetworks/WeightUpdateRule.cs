using System;
using System.Linq.Expressions;
using LinqInfer.Utility;
using LinqInfer.Utility.Expressions;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public static class WeightUpdateRules
    {
        public static WeightUpdateRule Simple()
        {
            return WeightUpdateRule.Create(
                p => p.CurrentWeightValue +
                     p.CurrentLearningRate * p.CurrentWeightValue *
                     p.Error);
        }

        public static WeightUpdateRule Default()
        {
            return WeightUpdateRule.Create(
                p => p.CurrentWeightValue +
                     (p.CurrentLearningRate * ((p.CurrentMomentum * p.CurrentWeightValue) +
                                               ((1.0 - p.CurrentMomentum) * (p.Error * p.PreviousLayerOutput)))));
        }
    }

    public sealed class WeightUpdateRule
    {
        private double _learningRate;
        private double _momentum;
        private readonly Expression<Func<WeightUpdateParameters, double>> _rule;
        private readonly Func<WeightUpdateParameters, double> _compiledRule;

        private WeightUpdateRule(double learningRate, double momentum, Expression<Func<WeightUpdateParameters, double>> expression)
        {
            _learningRate = learningRate;
            _momentum = momentum;
            _rule = expression;
            _compiledRule = expression.Compile();
        }

        public static WeightUpdateRule Create(Expression<Func<WeightUpdateParameters, double>> expression)
        {
            return new WeightUpdateRule(0.05, 0.1, expression);
        }

        public static WeightUpdateRule Create(string expression)
        {
            return Create(expression.AsExpression<WeightUpdateParameters, double>());
        }

        public void Initialise(LearningParameters learningParameters)
        {
            _learningRate = learningParameters.LearningRate;
            _momentum = learningParameters.Momentum;
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
            updateParams.CurrentLearningRate = _learningRate;
            updateParams.CurrentMomentum = _momentum;

            return _compiledRule(updateParams);
        }

        public string Export()
        {
            return _rule.ExportAsString();
        }
    }
}