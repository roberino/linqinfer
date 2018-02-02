using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public static class Activators
    {
        public static IEnumerable<ActivatorFunc> All()
        {
            yield return Sigmoid();
            yield return Threshold();
            // yield return HyperbolicTangent();
        }

        public static ActivatorFunc Create(string name, double parameter)
        {
            var type = typeof(Activators).GetTypeInfo();

            return (ActivatorFunc)type
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name == name && m.GetParameters().Length == 1)
                .FirstOrDefault()
                .Invoke(null, new object[] { parameter });
        }

        public static ActivatorFunc None(double s = 1)
        {
            return new ActivatorFunc()
            {
                Name = "None",
                Activator = x => s * x,
                Derivative = x => 0,
                Parameter = s,
                Create = (p) => None(p)
            };
        }

        public static ActivatorFunc Sigmoid(double alpha = 2)
        {
            return new ActivatorFunc()
            {
                Name = "Sigmoid",
                Activator = x => (1 / (1 + Math.Exp(-alpha * x))),
                Derivative = x => (alpha * x * (1 - x)),
                Parameter = alpha,
                Create = (p) => Sigmoid(p)
            };
        }

        public static ActivatorFunc HyperbolicTangent(double alpha = 2)
        {
            var e = Math.E;
            return new ActivatorFunc()
            {
                Name = "Tanh",
                Activator = x => (Math.Pow(e, x) - Math.Pow(e, -x)) / (Math.Pow(e, x) + Math.Pow(e, -x)),
                Derivative = x => 0, // TODO
                Parameter = alpha,
                Create = (p) => Sigmoid(p)
            };
        }

        public static ActivatorFunc Threshold(double threshold = 0.5)
        {
            return new ActivatorFunc()
            {
                Name = "Threshold",
                Activator = x => x > threshold ? 1 : 0,
                Derivative = x => 0,
                Parameter = threshold,
                Create = (p) => Threshold(p)
            };
        }
    }
}