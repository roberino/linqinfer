using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public static class Activators
    {
        public static ActivatorExpression FindByName(string name)
        {
            return All().SingleOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<ActivatorExpression> All()
        {
            yield return Sigmoid();
            yield return Threshold();
            yield return HyperbolicTangent();
        }

        public static ActivatorExpression None(double s = 1)
        {
            return new ActivatorExpression(nameof(None), x => x, x => 1);
        } 

        public static ActivatorExpression RectifiedLinearUnit(double t = 0)
        {
            return new ActivatorExpression(nameof(RectifiedLinearUnit),
                x => x > t ? x : t,
                x => x > t ? 1 : 0);
        }

        public static ActivatorExpression Sigmoid(double alpha = 2)
        {
            return new ActivatorExpression(nameof(Sigmoid),
                x => (1 / (1 + Math.Exp(-alpha * x))),
                x => (alpha * x * (1 - x)));
        }

        public static ActivatorExpression HyperbolicTangent(double nv = 0)
        {
            var e = Math.E;
            return new ActivatorExpression(nameof(HyperbolicTangent),
                x => (Math.Pow(e, x) - Math.Pow(e, -x)) / (Math.Pow(e, x) + Math.Pow(e, -x)),
                x => Math.Pow(2, 2 / (Math.Pow(e, x) + Math.Pow(e, -x))));
        }

        public static ActivatorExpression Threshold(double threshold = 0.5)
        {
            return new ActivatorExpression(nameof(Threshold), 
                x => x > threshold ? 1 : 0, 
                x => x > threshold ? 1 : 0);
        }
    }
}