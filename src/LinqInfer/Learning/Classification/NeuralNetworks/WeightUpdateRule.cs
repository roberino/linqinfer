using System;
using System.Linq.Expressions;
using LinqInfer.Utility;
using LinqInfer.Utility.Expressions;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public static class WeightUpdateRules
    {
        public static IWeightUpdateRule Simple()
        {
            return WeightUpdateRule.Create(
                p => p.CurrentWeightValue +
                                p.CurrentLearningRate * p.CurrentWeightValue *
                                 p.Error);
        }

        public static IWeightUpdateRule Default(double momentum)
        {
            return WeightUpdateRule.Create(
                p => p.CurrentWeightValue +
                     (p.CurrentLearningRate * ((momentum * p.CurrentWeightValue) +
                                       ((1.0 - momentum) * (p.Error * p.PreviousLayerOutput)))));
        }
    }

    public sealed class WeightUpdateRule : IWeightUpdateRule
    {
        private double _learningRate;
        private readonly Expression<Func<WeightUpdateParameters, double>> _rule;

        private Func<WeightUpdateParameters, double> _compiledRule;

        private WeightUpdateRule(Expression<Func<WeightUpdateParameters, double>> expression)
        {
            _rule = expression;
            _compiledRule = expression.Compile();
        }

        public static IWeightUpdateRule Create(Expression<Func<WeightUpdateParameters, double>> expression)
        {
            return new WeightUpdateRule(expression);
        }

        public static IWeightUpdateRule Create(string expression)
        {
            return new WeightUpdateRule(expression.AsExpression<WeightUpdateParameters, double>());
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

            return _compiledRule(updateParams);
        }

        public string Export()
        {
            return _rule.ExportAsString();
        }
    }
}