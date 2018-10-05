using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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

        public static double GetTupleProduct((double x, double y) tuple)
        {
            return tuple.x * tuple.y;
        }

        public static async Task<double> GetValueX15Async(double x)
        {
            await Task.Delay(2);
            return x * 15;
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