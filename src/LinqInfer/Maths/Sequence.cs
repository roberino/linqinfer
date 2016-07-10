using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents a fixed sequence of values which can be equality compared.
    /// </summary>
    public sealed class Sequence<T> : IEnumerable<T>, IEquatable<IEnumerable<T>>, IStructuralEquatable where T : IEquatable<T>
    {
        private readonly T[] _values;

        /// <summary>
        /// Creates a new sequence of a given size
        /// </summary>
        /// <param name="size">The size of the sequence</param>
        public Sequence(int size)
        {
            Contract.Assert(size > 0);

            _values = new T[size];
        }

        /// <summary>
        /// Creates a new sequence using the supplied values
        /// </summary>
        public Sequence(IEnumerable<T> values)
        {
            Contract.Assert(values != null);

            var values0 = values.ToArray();

            Contract.Assert(values0.Length > 0);

            _values = values0;
        }


        /// <summary>
        /// Returns a new sequence which is this sequence concatenated with the next item and the first item removed
        /// </summary>
        /// <param name="nextItem"></param>
        /// <returns></returns>
        public Sequence<T> Permutate(T nextItem)
        {
            return new Sequence<T>(_values.Skip(1).Concat(new[] { nextItem }));
        }

        /// <summary>
        /// Gets or sets a value by index
        /// </summary>
        public T this[int index]
        {
            get
            {
                return _values[index];
            }
            set
            {
                _values[index] = value;
            }
        }

        /// <summary>
        /// Returns the first value in the sequence
        /// </summary>
        public T First
        {
            get
            {
                return _values[0];
            }
        }

        /// <summary>
        /// Returns the last value in the sequence
        /// </summary>
        public T Last
        {
            get
            {
                return _values[_values.Length - 1];
            }
        }

        /// <summary>
        /// Transforms each value in the sequence using a function
        /// </summary>
        public Sequence<T> Transform(Func<T, T> transformation)
        {
            return new Sequence<T>(_values.Select(transformation));
        }

        /// <summary>
        /// Returns the size of the sequence
        /// </summary>
        public int Count
        {
            get
            {
                return _values.Length;
            }
        }

        /// <summary>
        /// Returns true if the other enumeration of objects is structually equal to this sequence of values
        /// </summary>
        public bool Equals(IEnumerable<T> other)
        {
            if (other == null) return false;

            if (other is Sequence<T>)
            {
                if (ReferenceEquals(this, other)) return true;
                if (((Sequence<T>)other).Count != Count) return false;
            }
            else
            {
                if (other.Count() != Count) return false;
            }

            return other.Zip(this, (x, y) => x.Equals(y)).All(v => v);
        }

        public override int GetHashCode()
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(_values);

            //int h = 1;


            //return _values.Aggregate(h, (v, t) =>
            //{
            //    v &= t.GetHashCode();

            //    return v;
            //});
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IEnumerable<T>);
        }

        public IEnumerator<T> GetEnumerator()
        {

            return _values.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public bool Equals(object other, IEqualityComparer comparer)
        {
            return Equals(other as Sequence<T>);
        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            return comparer.GetHashCode(_values);
        }
    }
}