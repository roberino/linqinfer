using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths
{
    public static class VectorFunctions
    {
        public static Func<ColumnVector1D, ColumnVector1D> CreateNormalisingFunction(this IEnumerable<ColumnVector1D> values)
        {
            var max = values.MaxOfEachDimension();

            return x => x / max;
        }

        public static IEnumerable<ColumnVector1D> NormaliseEachDimension(this IEnumerable<ColumnVector1D> values)
        {
            var max = values.MaxOfEachDimension();

            foreach (var v in values)
            {
                yield return v / max;
            }
        }

        public static ColumnVector1D MaxOfEachDimension(this IEnumerable<ColumnVector1D> values)
        {
            if (values.Any())
            {
                return new ColumnVector1D(Enumerable.Range(0, values.First().Size).Select(n => values.Select(v => v[n]).Max()).ToArray());
            }

            throw new ArgumentException();
        }

        public static ColumnVector1D MeanOfEachDimension(this IEnumerable<ColumnVector1D> values)
        {
            if (values.Any())
            {
                return new ColumnVector1D(Enumerable.Range(0, values.First().Size).Select(n => values.Select(v => v[n]).Mean()).ToArray());
            }

            throw new ArgumentException();
        }

        public static ColumnVector1D MinOfEachDimension(this IEnumerable<ColumnVector1D> values)
        {
            if (values.Any())
            {
                return new ColumnVector1D(Enumerable.Range(0, values.First().Size).Select(n => values.Select(v => v[n]).Min()).ToArray());
            }

            throw new ArgumentException();
        }

        internal static Tuple<ColumnVector1D, ColumnVector1D> MinAnMaxOfEachDimension(this IEnumerable<ColumnVector1D> values)
        {
            ColumnVector1D min = null;
            ColumnVector1D max = null;

            foreach (var val in values)
            {
                if (min == null)
                {
                    min = new ColumnVector1D(val.ToDoubleArray());
                    max = new ColumnVector1D(val.ToDoubleArray());
                }
                else
                {
                    min.Apply((x, n) => x > val[n] ? val[n] : x);
                    max.Apply((x, n) => x < val[n] ? val[n] : x);
                }
            }

            if (min == null) throw new ArgumentException();

            return new Tuple<ColumnVector1D, ColumnVector1D>(min, max);
        }

        public static ColumnVector1D Aggregate(this IEnumerable<ColumnVector1D> values, Func<double, double, double> func)
        {
            if (!values.Any()) throw new ArgumentException(nameof(values));

            double[] result = new double[values.First().Size];

            foreach (var value in values)
            {
                var x = value.GetUnderlyingArray();

                for (var i = 0; i < x.Length; i++)
                {
                    result[i] = func(result[i], x[i]);
                }
            }

            return new ColumnVector1D(result);
        }
    }
}
