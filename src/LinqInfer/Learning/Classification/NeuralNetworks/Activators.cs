using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using LinqInfer.Utility.Expressions;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public class ActivatorExpression
    {
        private readonly Expression<Func<double, double>> _activatorExpression;
        private readonly Expression<Func<double, double>> _derivativeExpression;
        private readonly Lazy<Func<double, double>> _activator;
        private readonly Lazy<Func<double, double>> _derivative;

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
            var parts = data.Split(':');
            var exp = parts[1].Split('#').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

            var act = exp.First().AsExpression<double, double>();
            var der = exp.Last().AsExpression<double, double>();

            return new ActivatorExpression(parts[0].Trim(), act, der);
        }
    }

    public static class Activators
    {
        public static IEnumerable<IActivatorFunction> All()
        {
            yield return Sigmoid();
            yield return Threshold();
            yield return HyperbolicTangent();
        }

        public static IActivatorFunction Create(string name, double parameter)
        {
            var type = typeof(Activators).GetTypeInfo();

            return (IActivatorFunction)type
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == name && m.GetParameters().Length == 1)
                .Invoke(null, new object[] { parameter });
        }

        public static IActivatorFunction None(double s = 1)
        {
            return new ActivatorFunc()
            {
                Name = nameof(None),
                Activator = x => x,
                Derivative = x => 1,
                Parameter = s,
                Create = (p) => None(p)
            };
        }

        public static ActivatorExpression SigmoidA(double alpha = 2)
        {
            return new ActivatorExpression(nameof(Sigmoid),
                x => (1 / (1 + Math.Exp(-alpha * x))),
                x => (alpha * x * (1 - x)));
        }

        public static IActivatorFunction Sigmoid(double alpha = 2)
        {
            return new ActivatorFunc()
            {
                Name = nameof(Sigmoid),
                Activator = x => (1 / (1 + Math.Exp(-alpha * x))),
                Derivative = x => (alpha * x * (1 - x)),
                Parameter = alpha,
                Create = (p) => Sigmoid(p)
            };
        }

        public static IActivatorFunction HyperbolicTangent(double nv = 0)
        {
            var e = Math.E;
            return new ActivatorFunc()
            {
                Name = nameof(HyperbolicTangent),
                Activator = x => (Math.Pow(e, x) - Math.Pow(e, -x)) / (Math.Pow(e, x) + Math.Pow(e, -x)),
                Derivative = x => Math.Pow(2, 2 / (Math.Pow(e, x) + Math.Pow(e, -x))),
                Parameter = 0,
                Create = (p) => HyperbolicTangent(p)
            };
        }

        public static IActivatorFunction Threshold(double threshold = 0.5)
        {
            return new ActivatorFunc()
            {
                Name = nameof(Threshold),
                Activator = x => x > threshold ? 1 : 0,
                Derivative = x => 0,
                Parameter = threshold,
                Create = (p) => Threshold(p)
            };
        }
    }
}