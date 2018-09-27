using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class DiscretizedSet<V, W, T> : IEnumerable<IGrouping<int, T>>
    {
        readonly IDictionary<int, List<T>> _values;
        Lazy<IDictionary<int, int>> _binCounts;
        readonly Func<int, V> _widthConverter;

        internal DiscretizedSet(V min, V max, W width, Func<int, V> widthConverter)
        {
            Min = min;
            Max = max;
            Width = width;

            _widthConverter = widthConverter;
            _values = new Dictionary<int, List<T>>();
        }

        /// <summary>
        /// Returns the minimum value of the set
        /// </summary>
        public V Min { get; }

        /// <summary>
        /// Returns the maximum value of the set
        /// </summary>
        public V Max { get; }

        /// <summary>
        /// Returns the width of each bin
        /// </summary>
        public W Width { get; }

        /// <summary>
        /// Returns a dictionary of bins and value counts
        /// </summary>
        public IDictionary<int, int> Bins
        {
            get
            {
                if (_binCounts == null)
                {
                    SetupBinCounts();
                }

                return _binCounts.Value;
            }
        }

        /// <summary>
        /// Returns the total number of items in the set
        /// </summary>
        public int Total
        {
            get
            {
                return _values.Sum(x => x.Value.Count);
            }
        }

        internal void CreateBins(IEnumerable<int> bins)
        {
            foreach(var bin in bins)
            {
                List<T> values;

                if (!_values.TryGetValue(bin, out values))
                {
                    _values[bin] = values = new List<T>();
                }
            }
        }

        internal void AddValue(int bin, T value)
        {
            List<T> values;

            if (!_values.TryGetValue(bin, out values))
            {
                _values[bin] = values = new List<T>();
            }

            values.Add(value);

            _binCounts = null;
        }

        void SetupBinCounts()
        {
            _binCounts = new Lazy<IDictionary<int, int>>(() =>
            {
                return _values.ToDictionary(v => v.Key, v => v.Value.Count);
            });
        }

        public IDictionary<V, IList<T>> ToDictionary()
        {
            return _values.ToDictionary(k => _widthConverter(k.Key), v => (IList<T>)v.Value);
        }

        public IEnumerator<IGrouping<int, T>> GetEnumerator()
        {
            return _values
                .Select(x => new ValueGroup<int>(x.Value) { Key = x.Key })
                .Cast<IGrouping<int, T>>()
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class ValueGroup<K> : List<T>, IGrouping<K, T>
        {
            public ValueGroup(IEnumerable<T> values)
            {
                AddRange(values);
            }
            public K Key { get; set; }
        }
    }
}
