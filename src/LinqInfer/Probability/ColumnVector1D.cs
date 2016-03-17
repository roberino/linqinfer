using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Probability
{
    public class ColumnVector1D : IEnumerable<double>
    {
        private readonly double[] _values;
        private readonly Lazy<double> _euclideanLength;

        public ColumnVector1D(double[] values)
        {
            _values = values;
            _euclideanLength = new Lazy<double>(() => Math.Sqrt(_values.Select(x => x * x).Sum()));
        }

        public double this[int i]
        {
            get
            {
                return _values[i];
            }
        }

        public double Sum()
        {
            return _values.Sum();
        }

        public ColumnVector1D Normalise()
        {
            var t = Sum();
            return new ColumnVector1D(_values.Select(x => x / t).ToArray());
        }

        public double[] ToDoubleArray()
        {
            var arr = new double[_values.Length];

            Array.Copy(_values, arr, _values.Length);

            return arr;
        }

        public float[] ToSingleArray()
        {
            return _values.Select(v => (float)v).ToArray();
        }

        public double EuclideanLength
        {
            get
            {
                return _euclideanLength.Value;
            }
        }

        public int Size
        {
            get
            {
                return _values.Length;
            }
        }

        public ColumnVector1D Sq()
        {
            return this * this;
        }

        public IEnumerator<double> GetEnumerator()
        {
            return _values.Cast<double>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public static ColumnVector1D Create(params double[] values)
        {
            return new ColumnVector1D(values);
        }

        public static ColumnVector1D operator -(ColumnVector1D v1, ColumnVector1D v2)
        {
            Contract.Assert(v1.Size == v2.Size);

            int i = 0;

            return new ColumnVector1D(v1._values.Select(x => x - v2._values[i++]).ToArray());
        }

        public static ColumnVector1D operator +(ColumnVector1D v1, ColumnVector1D v2)
        {
            Contract.Assert(v1.Size == v2.Size);

            int i = 0;

            return new ColumnVector1D(v1._values.Select(x => x + v2._values[i++]).ToArray());
        }

        public static ColumnVector1D operator *(ColumnVector1D v1, ColumnVector1D v2)
        {
            Contract.Assert(v1.Size == v2.Size);

            int i = 0;

            return new ColumnVector1D(v1._values.Select(x => x * v2._values[i++]).ToArray());
        }

        public static ColumnVector1D operator /(ColumnVector1D v1, ColumnVector1D v2)
        {
            Contract.Assert(v1.Size == v2.Size);

            int i = 0;

            return new ColumnVector1D(v1._values.Select(x => x / v2._values[i++]).ToArray());
        }

        public static ColumnVector1D operator /(ColumnVector1D v1, double y)
        {
            return new ColumnVector1D(v1._values.Select(x => x / y).ToArray());
        }

        public static ColumnVector1D operator *(ColumnVector1D v1, double y)
        {
            return new ColumnVector1D(v1._values.Select(x => x * y).ToArray());
        }
    }
}
