﻿using System;
using System.Linq;
using System.Linq.Expressions;
using LinqInfer.Utility.Expressions;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public class ActivatorExpression
    {
        readonly Expression<Func<double, double>> _activatorExpression;
        readonly Expression<Func<double, double>> _derivativeExpression;
        readonly Lazy<Func<double, double>> _activator;
        readonly Lazy<Func<double, double>> _derivative;

        public ActivatorExpression(string name,
            Expression<Func<double, double>> activatorExpression,
            Expression<Func<double, double>> derivativeExpression)
        {
            Name = name;
            _activatorExpression = activatorExpression;
            _derivativeExpression = derivativeExpression;
            _activator = new Lazy<Func<double, double>>(() => _activatorExpression.Compile());
            _derivative = new Lazy<Func<double, double>>(() => _derivativeExpression.Compile());
        }

        public string Name  {get;}

        public Func<double, double> Derivative => _derivative.Value;

        public Func<double, double> Activator => _activator.Value;

        public string Export()
        {
            return $"{Name}: #{_activatorExpression.ExportAsString()} #{_derivativeExpression.ExportAsString()}";
        }

        public override string ToString() => Name;

        public static ActivatorExpression Parse(string data)
        {
            var i = data.IndexOf(':');
            var name = data.Substring(0, i).Trim();
            var exprParts = data.Substring(i + 1);
            var exp = exprParts.Split('#').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

            var act = exp.First().AsExpression<double, double>();
            var der = exp.Last().AsExpression<double, double>();

            return new ActivatorExpression(name, act, der);
        }
    }
}