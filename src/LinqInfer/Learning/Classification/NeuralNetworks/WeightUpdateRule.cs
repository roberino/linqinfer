﻿using System;
using System.Linq.Expressions;
using LinqInfer.Utility;
using LinqInfer.Utility.Expressions;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public static class WeightUpdateRules
    {
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
        double _learningRate;
        double _momentum;
        readonly Expression<Func<WeightUpdateParameters, double>> _rule;
        readonly Func<WeightUpdateParameters, double> _compiledRule;

        WeightUpdateRule(double learningRate, double momentum, Expression<Func<WeightUpdateParameters, double>> expression)
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

        public static WeightUpdateRule Parse(string expression)
        {
            return Create(expression.AsExpression<WeightUpdateParameters, double>());
        }

        public void Initialise(TrainingParameters trainingParameters)
        {
            _learningRate = trainingParameters.LearningRate;
            _momentum = trainingParameters.Momentum;
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