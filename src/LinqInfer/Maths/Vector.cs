﻿using LinqInfer.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.IO;
using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents vector
    /// </summary>
    public class Vector : IEnumerable<double>, IMutableVector, IJsonExportable
    {
        protected readonly double[] _values;

        internal Vector(Vector vector, bool deepClone = true)
        {
            _values = deepClone ? vector._values.ToArray() : vector._values;
            Refresh();
        }

        public Vector(int size)
        {
            _values = new double[size];
            Refresh();
        }

        public Vector(params double[] values)
        {
            ArgAssert.AssertNonNull(values, nameof(values));

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
        /// <returns>A new <see cref="Vector"/></returns>
        public static Vector VectorOfOnes(int size)
        {
            return UniformVector(size, 1d);
        }

        /// <summary>
        /// Returns a vector where each element is a uniform value
        /// </summary>
        /// <param name="size">The size of the vector</param>
        /// <param name="value">The value of each element</param>
        /// <returns>A new <see cref="Vector"/></returns>
        public static Vector UniformVector(int size, double value)
        {
            return new Vector(Enumerable.Range(0, size).Select(n => value).ToArray());
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
        /// Returns the vector size. This remains constant for the lifetime of the vector.
        /// </summary>
        public int Size => _values.Length;

        /// <summary>
        /// Returns the average of all values
        /// </summary>
        public double Mean => _values.Average();

        /// <summary>
        /// Returns the maximum of all values
        /// </summary>
        public double Max => _values.Max();

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
        /// Returns the sum of all values
        /// </summary>
        public double Sum => _values.Sum();

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
        /// Shifts each value by substracting the provided value
        /// </summary>
        public Vector Shift(double value)
        {
            return new Vector(_values.Select(v => v - value).ToArray());
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

        /// <summary>
        /// Returns the sample or population variance of the values
        /// </summary>
        /// <param name="isSampleData">True to treat as a sample and average over the number of items - 1</param>
        /// <returns>A double</returns>
        public double Variance(bool isSampleData = true)
        {
            var mean = Mean;

            var total = 0d;

            for (int i = 0; i < _values.Length; i++)
            {
                var v = _values[i] - mean;
                total += (v * v);
            }

            return total / (_values.Length - (isSampleData ? 1 : 0));
        }

        /// <summary>
        /// Runs a function over the values in each vector,
        /// returning a new vector
        /// i.e. add : (v1, v2) => v1 + v2
        /// </summary>
        public Vector Calculate(IVector other, Func<double, double, double> calculation)
        {
            ArgAssert.AssertEquals(other.Size, Size, nameof(Size));

            var result = new double[Size];

            for (int i = 0; i < _values.Length; i++)
            {
                result[i] = calculation(_values[i], other[i]);
            }

            return new Vector(result);
        }

        internal Vector[] CrossCalculate(Func<double, double[], double[]> func, params IVector[] others)
        {
            foreach (var v in others) ArgAssert.AssertEquals(Size, v.Size, nameof(Size));

            return CrossCalculate(func, others.Length, others);
        }

        internal Vector[] CrossCalculate(IVector other, Func<double, double, double[]> func, int numberOfVectorsReturned)
        {
            var results = new List<double[]>(numberOfVectorsReturned);

            for (int j = 0; j < numberOfVectorsReturned; j++)
            {
                results.Add(new double[Size]);
            }

            for (int i = 0; i < _values.Length; i++)
            {
                var res = func(this[i], other[i]);

                for (int j = 0; j < numberOfVectorsReturned; j++)
                {
                    results[j][i] = res[j];
                }
            }

            return results.Select(r => new Vector(r)).ToArray();
        }

        internal Vector[] CrossCalculate(Func<double, double[], double[]> func, int numberOfVectorsReturned, params IVector[] others)
        {
            var buffer = new double[others.Length];

            var results = new List<double[]>(numberOfVectorsReturned);

            for (int j = 0; j < numberOfVectorsReturned; j++)
            {
                results.Add(new double[Size]);
            }

            for (int i = 0; i < _values.Length; i++)
            {
                for (int j = 0; j < others.Length; j++)
                {
                    buffer[j] = others[j][i];
                }

                var res = func(this[i], buffer);

                for (int j = 0; j < numberOfVectorsReturned; j++)
                {
                    results[j][i] = res[j];
                }
            }

            return results.Select(r => new Vector(r)).ToArray();
        }

        public double DotProduct(Vector other)
        {
            var v = this * other;

            return v.Sum;
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
            ArgAssert.AssertEquals(v1.Size, v2.Size, nameof(v1.Size));

            var newValues = new double[v1.Size];
            var v1a = v1._values;
            var v2a = v2._values;

            for (int i = 0; i < v1a.Length; i++)
            {
                newValues[i] = v1a[i] * v2a[i];
            }

            return new ColumnVector1D(newValues);
        }

        public static Vector operator /(Vector v1, Vector v2)
        {
            return v1.Divide(v2, ZeroDivideBehaviour.ReturnNan);
        }

        public Vector Divide(Vector v2, ZeroDivideBehaviour zeroDivideBehaviour)
        {
            Contract.Requires(Size == v2.Size);

            var divider = zeroDivideBehaviour.CreateDivider();

            var newValues = new double[Size];
            var v1a = _values;
            var v2a = v2._values;

            for (int i = 0; i < v1a.Length; i++)
            {
                newValues[i] = divider(v1a[i], v2a[i]);
            }

            return new ColumnVector1D(newValues);
        }

        public static Vector operator /(Vector v1, double y)
        {
            return new Vector(v1._values.Select(x => x / y).ToArray());
        }

        public static Vector operator *(Vector v1, double y)
        {
            return new Vector(v1._values.Select(x => x * y).ToArray());
        }

        /// <summary>
        /// Multiplies the vector by the matrix
        /// </summary>
        public IVector MultiplyBy(Matrix matrix)
        {
            return matrix * this;
        }

        public IVector MultiplyBy(IVector vector)
        {
            if (vector is Vector v)
            {
                return v * this;
            }

            return vector.MultiplyBy(this);
        }

        public IVector HorizontalMultiply(IMatrix matrix)
        {
            // 1, 2, 3 * x a
            //           y b
            //           z c
            // = [1x + 2y + 3z, 1a + 2b + 3c]

            var result = new double[matrix.Width];
            var j = 0;

            foreach (var row in matrix.Rows)
            {
                var rowVals = row.ToColumnVector().GetUnderlyingArray();

                for (var i = 0; i < rowVals.Length; i++)
                {
                    result[i] += _values[j] * rowVals[i];
                }

                j++;
            }

            return new Vector(result);
        }

        public double DotProduct(IVector vector)
        {
            return MultiplyBy(vector).Sum;
        }

        public virtual ColumnVector1D ToColumnVector()
        {
            return this as ColumnVector1D ?? new ColumnVector1D(this);
        }

        public override string ToString()
        {
            return _values
                .Select(v => string.Format("|{0}|\n", v))
                .Aggregate(new StringBuilder(), (s, v) => s.Append(v))
                .ToString();
        }

        /// <summary>
        /// Converts the vector to a Base64 string
        /// </summary>
        public string ToBase64()
        {
            return Convert.ToBase64String(ToByteArray());
        }

        /// <summary>
        /// Converts the vector from a Base64 string
        /// </summary>
        public static Vector FromBase64(string data)
        {
            return FromByteArray(Convert.FromBase64String(data));
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
        /// Returns the values as a JSON array
        /// </summary>
        /// <returns>A JSON string</returns>
        public string ToJson()
        {
            using (var writer = new StringWriter())
            {
                WriteJson(writer);

                return writer.ToString();
            }
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
            if (precision == int.MaxValue)
            {
                return string.Join(delimitter.ToString(), _values.Select(v => v.ToString("R")));
            }

            return string.Join(delimitter.ToString(), _values.Select(v => Math.Round(v, precision).ToString()));
        }

        /// <summary>
        /// Returns a vector from a list of comma separated values (e.g. 1.4, 23.234, 223.2)
        /// </summary>
        public static Vector FromCsv(string csv, char delimitter = ',')
        {
            Contract.Ensures(csv != null);

            return new Vector(csv.Split(delimitter).Select(v => double.Parse(v.Trim())).ToArray());
        }

        /// <summary>
        /// Writes the data as a JSON array
        /// </summary>
        public void WriteJson(TextWriter output)
        {
            output.Write("[" + ToCsv(',', int.MaxValue) + "]");
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
            return obj is Vector ? Equals(new ColumnVector1D((Vector)obj)) : Equals(obj as IVector);
        }

        /// <summary>
        /// Returns true if an other vector is structually equal to this vector
        /// </summary>
        public bool Equals(IVector other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            if (Size != other.Size) return false;

            if (other is Vector)
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(_values, ((Vector)other)._values);
            }

            return other.Equals(this);
        }

        internal void Overwrite(IEnumerable<double> values)
        {
            int i = 0;

            foreach (var value in values)
            {
                _values[i++] = value;
            }
        }

        internal void Overwrite(double[] values)
        {
            Array.Copy(values, _values, _values.Length);
        }

        internal double[] GetUnderlyingArray()
        {
            return _values;
        }

        internal void DetachEvents()
        {
            Modified = null;
        }

        protected virtual void Refresh()
        {
            Modified?.Invoke(this, EventArgs.Empty);
        }
    }
}