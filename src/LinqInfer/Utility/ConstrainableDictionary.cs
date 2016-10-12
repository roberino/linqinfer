using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Utility
{
    public class ConstrainableDictionary<K, T> : IDictionary<K, T>
    {
        private readonly IDictionary<K, T> _innerDictionary;
        private readonly IList<Tuple<Func<K, T, bool>, string>> _valueConstraints;

        public ConstrainableDictionary(IDictionary<K, T> data = null)
        {
            _innerDictionary = data ?? new Dictionary<K, T>();
            _valueConstraints = new List<Tuple<Func<K, T, bool>, string>>();
        }

        public ConstrainableDictionary(Func<T, bool> constraint)
        {
            _innerDictionary = new Dictionary<K, T>();
            _valueConstraints = new List<Tuple<Func<K, T, bool>, string>>();

            AddContraint(constraint);
        }

        /// <summary>
        /// Ensures a value set for a specific key is of a specified type.
        /// </summary>
        /// <typeparam name="T1">The base type allowed</typeparam>
        /// <param name="key">The key</param>
        /// <param name="allowNull">True if null values are allowed</param>
        public void EnforceType<T1>(K key, bool allowNull = false)
        {
            if (typeof(T) != typeof(object) && typeof(T).IsAssignableFrom(typeof(T1)))
            {
                throw new ArgumentException("Invalid type constraint - " + typeof(T1).FullName);
            }

            AddContraint((k, v) =>
            {
                if (k.Equals(key))
                {
                    if (v == null) return allowNull;

                    return (typeof(T1).IsAssignableFrom(v.GetType()));
                }
                return true;
            }, "Invalid value type");
        }

        /// <summary>
        /// Prevents a key / value pair from being altered
        /// </summary>
        /// <param name="key"></param>
        public void LockKey(K key)
        {
            AddContraint((k, v) => !k.Equals(key), "This property is readonly - " + key);
        }

        /// <summary>
        /// Adds a constraint which will be evaluated against a key / value when added / set.
        /// A false value will invalidate the operation, throwing an <see cref="ArgumentException"/>.
        /// </summary>
        public void AddContraint(Func<T, bool> constraint, string message = null)
        {
            _valueConstraints.Add(new Tuple<Func<K, T, bool>, string>((k, v) => constraint(v), message));
        }

        /// <summary>
        /// Adds a constraint which will be evaluated when a value is added / set.
        /// A false value will invalidate the operation, throwing an <see cref="ArgumentException"/>.
        /// </summary>
        public void AddContraint(Func<K, T, bool> constraint, string message = null)
        {
            _valueConstraints.Add(new Tuple<Func<K, T, bool>, string>(constraint, message));
        }

        public T this[K key]
        {
            get
            {
                return _innerDictionary[key];
            }
            set
            {
                Validate(key, value);

                _innerDictionary[key] = value;
            }
        }

        public int Count
        {
            get
            {
                return _innerDictionary.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return _innerDictionary.IsReadOnly;
            }
        }

        public ICollection<K> Keys
        {
            get
            {
                return _innerDictionary.Keys;
            }
        }

        public ICollection<T> Values
        {
            get
            {
                return _innerDictionary.Values;
            }
        }

        public void Add(KeyValuePair<K, T> item)
        {
            Validate(item.Key, item.Value);

            _innerDictionary.Add(item);
        }

        public void Add(K key, T value)
        {
            Validate(key, value);

            _innerDictionary.Add(key, value);
        }

        public void Clear()
        {
            _innerDictionary.Clear();
        }

        public bool Contains(KeyValuePair<K, T> item)
        {
            return _innerDictionary.Contains(item);
        }

        public bool ContainsKey(K key)
        {
            return _innerDictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<K, T>[] array, int arrayIndex)
        {
            _innerDictionary.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<K, T>> GetEnumerator()
        {
            return _innerDictionary.GetEnumerator();
        }

        public bool Remove(KeyValuePair<K, T> item)
        {
            return _innerDictionary.Remove(item);
        }

        public bool Remove(K key)
        {
            return _innerDictionary.Remove(key);
        }

        public bool TryGetValue(K key, out T value)
        {
            return _innerDictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerDictionary.GetEnumerator();
        }

        private void Validate(K key, T value)
        {
            var violations = _valueConstraints.Where(c => !c.Item1(key, value)).ToList();

            if (violations.Any())
            {
                var exs = violations.Select(v => new ArgumentException(v.Item2 ?? "Constraint violation")).ToList();

                if (exs.Count == 1) throw exs.First();

                throw new AggregateException(exs);
            }
        }
    }
}