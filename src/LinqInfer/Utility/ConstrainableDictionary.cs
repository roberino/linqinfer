using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Utility
{
    public class ConstrainableDictionary<K, T> : IDictionary<K, T>
    {
        readonly IDictionary<K, T> _innerDictionary;
        readonly IList<Tuple<Func<K, T, bool>, string>> _valueConstraints;

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
            if (typeof(T) != typeof(object) && typeof(T).GetTypeInf().IsAssignableFrom(typeof(T1)))
            {
                throw new ArgumentException("Invalid type constraint - " + typeof(T1).FullName);
            }

            AddContraint((k, v) =>
            {
                if (k.Equals(key))
                {
                    if (v == null) return allowNull;

                    return (typeof(T1).GetTypeInf().IsAssignableFrom(v.GetType()));
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

                OnKeyUpdated(key);
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

            OnKeyUpdated(item.Key);
        }

        public void Add(K key, T value)
        {
            Validate(key, value);

            _innerDictionary.Add(key, value);

            OnKeyUpdated(key);
        }

        public void Clear()
        {
            foreach (var kv in _innerDictionary)
            {
                Validate(kv.Key, kv.Value);
            }
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
            if (_innerDictionary.Remove(item))
            {
                OnKeyUpdated(item.Key);
                return true;
            }
            return false;
        }

        public bool Remove(K key)
        {
            if (_innerDictionary.Remove(key))
            {
                OnKeyUpdated(key);
                return true;
            }
            return false;
        }

        public bool TryGetValue(K key, out T value)
        {
            return _innerDictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerDictionary.GetEnumerator();
        }

        /// <summary>
        /// Invoked whenever a value is updated for a key
        /// </summary>
        protected virtual void OnKeyUpdated(K key)
        {
        }

        void Validate(K key, T value)
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