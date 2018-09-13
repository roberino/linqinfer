using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.UnitTests.Utility.Expressions
{
    public static class StaticExampleMethods
    {
        public static double GetPiX(int x)
        {
            return Math.PI * x;
        }

        public static double[] GetPiArray(int elements)
        {
            return Enumerable.Range(0, elements).Select(n => Math.PI).ToArray();
        }

        public static TestObject[] GetObjectArray(int elements)
        {
            return Enumerable.Range(0, elements).Select(n => new TestObject{ Val = n }).ToArray();
        }

        public static double GetXOrZero(double input, Expression<Func<double, bool>> condition)
        {
            return condition.Compile().Invoke(input) ? input : 0d;
        }
    }

    public class TestObject
    {
        public int Val { get; set; }
    }
}