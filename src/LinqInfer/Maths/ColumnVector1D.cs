using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace LinqInfer.Maths
{
    public class ColumnVector1D : IEnumerable<double>, IEquatable<ColumnVector1D>
    {
        private readonly double[] _values;
        private readonly Lazy<double> _euclideanLength;

        public ColumnVector1D(double[] values)
        {
            _values = values;
            _euclideanLength = new Lazy<double>(() => System.Math.Sqrt(_values.Select(x => x * x).Sum()));
        }

        public ColumnVector1D(float[] values) : this(values.Select(x => (double)x).ToArray())
        {
        }

        public double this[int i]
        {
            get
            {
                return _values[i];
            }
        }

        public void Apply(Func<double, double> func)
        {
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = func(_values[i]);
            }
        }

        public double Distance(ColumnVector1D input)
        {
            double d = 0;

            for (int i = 0; i < _values.Length; i++)
            {
                d += Math.Pow(_values[i] - input[i], 2f);
            }

            return d;
        }

        public IEnumerable<ColumnVector1D> Range(ColumnVector1D to, int binCount)
        {
            Contract.Assert(binCount > -1);

            if (binCount == 0) yield break;

            yield return this;

            if (binCount > 1)
            {
                if (binCount > 2)
                {
                    var last = this;
                    var binWidth = (to - this) / (binCount - 1);

                    foreach (var n in Enumerable.Range(0, binCount - 2))
                    {
                        last = last + binWidth;

                        yield return last;
                    }
                }

                yield return to;
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

        public override string ToString()
        {
            return _values
                .Select(v => string.Format("|{0}|\n", v))
                .Aggregate(new StringBuilder(), (s, v) => s.Append(v))
                .ToString();
        }

        public string ToCsv(int precision = 8)
        {
            return string.Join(",", _values.Select(v => System.Math.Round(v, precision).ToString()));
        }

        public override int GetHashCode()
        {
            return _values.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ColumnVector1D);
        }

        public bool Equals(ColumnVector1D other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            if (Size != other.Size) return false;

            if (_euclideanLength.IsValueCreated && other._euclideanLength.IsValueCreated && EuclideanLength != other.EuclideanLength) return false;

            int i = 0;

            return _values.All(x => x == other._values[i++]);
        }
    }
}