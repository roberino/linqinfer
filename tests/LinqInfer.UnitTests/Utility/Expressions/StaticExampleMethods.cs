using System;
using System.Linq.Expressions;

namespace LinqInfer.UnitTests.Utility.Expressions
{
    public static class StaticExampleMethods
    {
        public static double GetPiX(int x)
        {
            return Math.PI * x;
        }

        public static double GetXOrZero(double input, Expression<Func<double, bool>> condition)
        {
            return condition.Compile().Invoke(input) ? input : 0d;
        }
    }
}