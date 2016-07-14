using LinqInfer.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents a 1 dimensional column vector
    /// </summary>
    [Serializable]
    public class ColumnVector1D : IEnumerable<double>, IEquatable<ColumnVector1D>, ICloneableObject<ColumnVector1D>
    {
        private readonly double[] _values;
        private Lazy<double> _euclideanLength;

        public ColumnVector1D(double[] values)
        {
            _values = values;
            Refresh();
        }

        public ColumnVector1D(float[] values) : this(values.Select(x => (double)x).ToArray())
        {
        }

        /// <summary>
        /// Returns a value by index
        /// </summary>
        /// <param name="i">The index (base 0)</param>
        /// <returns>A double value</returns>
        public double this[int i]
        {
            get
            {
                return _values[i];
            }
        }

        /// <summary>
        /// Applies a function of the values in the vector, modifying each value.
        /// </summary>
        /// <param name="func">A function to transform the value (takes the original value as input)</param>
        public void Apply(Func<double, double> func)
        {
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = func(_values[i]);
            }
            Refresh();
        }

        /// <summary>
        /// Applies a function of the values in the vector, modifying each value.
        /// </summary>
        /// <param name="func">A function to transform the value (takes the original value and index as input)</param>
        public void Apply(Func<double, int, double> func)
        {
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = func(_values[i], i);
            }
            Refresh();
        }

        /// <summary>
        /// Returns the Euclidean distance of this vector from another vector.
        /// </summary>
        /// <param name="input">The other vector</param>
        /// <returns>A double value</returns>
        public double Distance(ColumnVector1D input)
        {
            return Distance(input._values);
        }

        /// <summary>
        /// Returns the Euclidean distance of this vector from another vector.
        /// </summary>
        /// <param name="input">The other vector</param>
        /// <returns>A double value</returns>
        public double Distance(double[] input)
        {
            double dist = 0;

            for (int i = 0; i < _values.Length; i++)
            {
                var d = _values[i] - input[i];
                dist += d * d;
            }

            return dist;
        }

        /// <summary>
        /// Returns an enumeration of vectors ranging from this vector to the specified vector over a 
        /// specified number of bins.
        /// </summary>
        /// <param name="to">The upper range</param>
        /// <param name="binCount">The number of bins</param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the sum of all values.
        /// </summary>
        /// <returns>A double</returns>
        public double Sum()
        {
            return _values.Sum();
        }

        /// <summary>
        /// Normalises each element over the sum of all values.
        /// </summary>
        /// <returns>A new normalised vector</returns>
        public ColumnVector1D Normalise()
        {
            var t = Sum();
            return new ColumnVector1D(_values.Select(x => x / t).ToArray());
        }

        /// <summary>
        /// Returns a copy of the values as a double array.
        /// </summary>
        /// <returns>A new double array</returns>
        public double[] ToDoubleArray()
        {
            var arr = new double[_values.Length];

            Array.Copy(_values, arr, _values.Length);

            return arr;
        }

        /// <summary>
        /// Returns a copy of the values as a single array.
        /// </summary>
        /// <returns>A new single array</returns>
        public float[] ToSingleArray()
        {
            return _values.Select(v => (float)v).ToArray();
        }

        /// <summary>
        /// Returns the Euclidean length of the values.
        /// </summary>
        public double EuclideanLength
        {
            get
            {
                return _euclideanLength.Value;
            }
        }

        /// <summary>
        /// Returns the vector size. This remains constant for the lifetime of the vector.
        /// </summary>
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

        /// <summary>
        /// Converts and exports the values to a byte array (for easy storage).
        /// </summary>
        /// <returns>An array of bytes</returns>
        public byte[] ToByteArray()
        {
            var bytes = new byte[_values.Length * sizeof(double)];
            Buffer.BlockCopy(_values, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Returns a column vector from a previous exported vector.
        /// </summary>
        /// <param name="bytes">An array of bytes</param>
        /// <returns>A new column vector</returns>
        public static ColumnVector1D FromByteArray(byte[] bytes)
        {
            var values = new double[bytes.Length / sizeof(double)];
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            return new ColumnVector1D(values);
        }

        public string ToCsv(int precision = 8)
        {
            return string.Join(",", _values.Select(v => Math.Round(v, precision).ToString()));
        }

        public override int GetHashCode()
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(_values);
        }

        /// <summary>
        /// Returns true if an other object is structually equal to this object
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as ColumnVector1D);
        }

        /// <summary>
        /// Returns true if an other vector is structually equal to this vector
        /// </summary>
        public bool Equals(ColumnVector1D other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            if (Size != other.Size) return false;

            if (_euclideanLength.IsValueCreated && other._euclideanLength.IsValueCreated && EuclideanLength != other.EuclideanLength) return false;

            int i = 0;

            return _values.All(x => x == other._values[i++]);
        }

        /// <summary>
        /// Removes values at the specific indexes, returning a new column vector.
        /// </summary>
        /// <param name="indexes">The zero based indexes to remove</param>
        /// <returns>A new <see cref="ColumnVector1D"/></returns>
        public ColumnVector1D RemoveValuesAt(params int[] indexes)
        {
            return new ColumnVector1D(Enumerable.Range(0, _values.Length).Except(indexes).Select(i => _values[i]).ToArray());
        }

        /// <summary>
        /// Clones the vector, returning a new vector containing the same values
        /// </summary>
        /// <param name="deep">Not applicable to this object type</param>
        /// <returns>A new <see cref="ColumnVector1D"/></returns>
        public ColumnVector1D Clone(bool deep)
        {
            return new ColumnVector1D(ToDoubleArray());
        }

        /// <summary>
        /// Clones the object
        /// </summary>
        public object Clone()
        {
            return Clone(true);
        }

        private void Refresh()
        {
            _euclideanLength = new Lazy<double>(() => Math.Sqrt(_values.Select(x => x * x).Sum()));
        }
    }
}