using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents a 1 dimensional column vector
    /// </summary>
    public class Vector : IEnumerable<double>, IEquatable<Vector>
    {
        protected readonly double[] _values;

        internal Vector(Vector vector, bool deepClone = true)
        {
            _values = deepClone ? vector._values.ToArray() : vector._values;
        }

        public Vector(int size)
        {
            _values = new double[size];
        }

        public Vector(double[] values)
        {
            _values = values;
            Refresh();
        }

        public Vector(float[] values) : this(values.Select(x => (double)x).ToArray())
        {
        }

        /// <summary>
        /// Returns a vector where each element is 1
        /// </summary>
        /// <param name="size">The size of the vector</param>
        /// <returns>A new <see cref="ColumnVector1D"/></returns>
        public static Vector VectorOfOnes(int size)
        {
            return new Vector(Enumerable.Range(0, size).Select(n => 1d).ToArray());
        }

        /// <summary>
        /// Fires when underlying data changes
        /// </summary>
        public event EventHandler Modified;

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
        /// Returns a dictionary of indexes and values
        /// </summary>
        public IDictionary<int, double> IndexedValues
        {
            get
            {
                int i = 0;
                return _values.ToDictionary(_ => i++, v => v);
            }
        }

        /// <summary>
        /// Applies a function over all values in the vector, modifying each value.
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
        /// Applies a function over all values in the vector, modifying each value.
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
        /// Returns the sum of all values.
        /// </summary>
        /// <returns>A double</returns>
        public double Sum()
        {
            return _values.Sum();
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
        /// Returns the vector size. This remains constant for the lifetime of the vector.
        /// </summary>
        public int Size
        {
            get
            {
                return _values.Length;
            }
        }

        /// <summary>
        /// Splits a vector into two parts
        /// e.g. [1,2,3,4,5] split at 2 = [1,2] + [3,4,5]
        /// </summary>
        /// <param name="index">The index where the vector will be split</param>
        public Vector[] Split(int index)
        {
            Contract.Assert(index > 0 && index < _values.Length - 1);

            var arr1 = new double[index];
            var arr2 = new double[_values.Length - index];

            Array.Copy(_values, 0, arr1, 0, index);
            Array.Copy(_values, index, arr2, 0, arr2.Length);

            return new[]
            {
                new Vector(arr1),
                new Vector(arr2),
            };
        }

        public double DotProduct(Vector other)
        {
            var v = this * other;

            return v.Sum();
        }

        public bool IsOrthogonalTo(Vector other)
        {
            return DotProduct(other) == 0;
        }

        public IEnumerator<double> GetEnumerator()
        {
            return _values.Cast<double>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public static Vector operator -(Vector v1, Vector v2)
        {
            Contract.Requires(v1.Size == v2.Size);

            int i = 0;

            return new Vector(v1._values.Select(x => x - v2._values[i++]).ToArray());
        }

        public static Vector operator +(Vector v1, Vector v2)
        {
            Contract.Requires(v1.Size == v2.Size);

            int i = 0;

            return new Vector(v1._values.Select(x => x + v2._values[i++]).ToArray());
        }

        public static Vector operator *(Vector v1, Vector v2)
        {
            Contract.Requires(v1.Size == v2.Size);

            int i = 0;

            return new Vector(v1._values.Select(x => x * v2._values[i++]).ToArray());
        }

        public static Vector operator /(Vector v1, Vector v2)
        {
            Contract.Requires(v1.Size == v2.Size);

            int i = 0;

            return new Vector(v1._values.Select(x => x / v2._values[i++]).ToArray());
        }

        public static Vector operator /(Vector v1, double y)
        {
            return new Vector(v1._values.Select(x => x / y).ToArray());
        }

        public static Vector operator *(Vector v1, double y)
        {
            return new Vector(v1._values.Select(x => x * y).ToArray());
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
        public static Vector FromByteArray(byte[] bytes)
        {
            var values = new double[bytes.Length / sizeof(double)];
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            return new Vector(values);
        }

        /// <summary>
        /// Returns the values as a comma separated string of values
        /// </summary>
        /// <param name="precision">The numeric precision of each member</param>
        /// <returns>A CSV string</returns>
        public string ToCsv(int precision = 8)
        {
            return ToCsv(',', precision);
        }

        /// <summary>
        /// Returns the values as a character separated string of values
        /// </summary>
        /// <param name="delimitter">The character used to delimit the values</param>
        /// <param name="precision">The numeric precision of each member</param>
        /// <returns>A CSV string</returns>
        public string ToCsv(char delimitter, int precision = 8)
        {
            return string.Join(delimitter.ToString(), _values.Select(v => Math.Round(v, precision).ToString()));
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
        public bool Equals(Vector other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            if (Size != other.Size) return false;

            int i = 0;

            return _values.All(x => x == other._values[i++]);
        }

        internal double[] GetUnderlyingArray()
        {
            return _values;
        }

        protected virtual void Refresh()
        {
            var ev = Modified;

            if (ev != null) ev.Invoke(this, EventArgs.Empty);
        }
    }
}