using LinqInfer.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents a 1 dimensional column vector
    /// </summary>
    public class ColumnVector1D : Vector, IEquatable<ColumnVector1D>, ICloneableObject<ColumnVector1D>
    {
        private Lazy<double> _euclideanLength;

        internal ColumnVector1D(Vector vector) : base(vector, false)
        {
        }

        public ColumnVector1D(double[] values) : base(values)
        {
        }

        public ColumnVector1D(float[] values) : this(values.Select(x => (double)x).ToArray())
        {
        }

        /// <summary>
        /// Returns a 1 column matrix
        /// </summary>
        public Matrix AsMatrix()
        {
            return new Matrix(_values.Select(v => new double[] { v }));
        }

        /// <summary>
        /// Concatinates two vectors together into one
        /// </summary>
        public ColumnVector1D Concat(ColumnVector1D other)
        {
            var arr = new double[_values.Length + other._values.Length];

            Array.Copy(_values, arr, _values.Length);
            Array.Copy(other._values, 0, arr, _values.Length, other._values.Length);

            return new ColumnVector1D(arr);
        }

        /// <summary>
        /// Splits a vector into two parts
        /// e.g. [1,2,3,4,5] split at 2 = [1,2] + [3,4,5]
        /// </summary>
        /// <param name="index">The index where the vector will be split</param>
        public new ColumnVector1D[] Split(int index)
        {
            Contract.Assert(index > 0 && index < _values.Length - 1);

            var arr1 = new double[index];
            var arr2 = new double[_values.Length - index];

            Array.Copy(_values, 0, arr1, 0, index);
            Array.Copy(_values, index, arr2, 0, arr2.Length);

            return new[]
            {
                new ColumnVector1D(arr1),
                new ColumnVector1D(arr2),
            };
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

            return Math.Sqrt(dist);
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
        /// Normalises each element over the sum of all values.
        /// </summary>
        /// <returns>A new normalised vector</returns>
        public ColumnVector1D Normalise()
        {
            var t = Sum();
            return new ColumnVector1D(_values.Select(x => x / t).ToArray());
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
        /// Returns the square of the vector
        /// </summary>
        public ColumnVector1D Sq()
        {
            return this * this;
        }

        /// <summary>
        /// Creates a new column vector with the supplied values
        /// </summary>
        public static ColumnVector1D Create(params double[] values)
        {
            return new ColumnVector1D(values);
        }

        public static ColumnVector1D operator -(ColumnVector1D v1, ColumnVector1D v2)
        {
            Contract.Requires(v1.Size == v2.Size);

            int i = 0;

            return new ColumnVector1D(v1._values.Select(x => x - v2._values[i++]).ToArray());
        }

        public static ColumnVector1D operator +(ColumnVector1D v1, ColumnVector1D v2)
        {
            Contract.Requires(v1.Size == v2.Size);

            int i = 0;

            return new ColumnVector1D(v1._values.Select(x => x + v2._values[i++]).ToArray());
        }

        public static ColumnVector1D operator *(ColumnVector1D v1, ColumnVector1D v2)
        {
            Contract.Requires(v1.Size == v2.Size);

            int i = 0;

            return new ColumnVector1D(v1._values.Select(x => x * v2._values[i++]).ToArray());
        }

        public static ColumnVector1D operator /(ColumnVector1D v1, ColumnVector1D v2)
        {
            Contract.Requires(v1.Size == v2.Size);

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
        /// Returns a column vector from a previous exported vector.
        /// </summary>
        /// <param name="bytes">An array of bytes</param>
        /// <returns>A new column vector</returns>
        public static new ColumnVector1D FromByteArray(byte[] bytes)
        {
            var values = new double[bytes.Length / sizeof(double)];
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            return new ColumnVector1D(values);
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

        protected override void Refresh()
        {
            _euclideanLength = new Lazy<double>(() => Math.Sqrt(_values.Select(x => x * x).Sum()));
        }
    }
}