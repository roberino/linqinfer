using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Utility
{
    public class ConstrainableDictionary<K, T> : IDictionary<K, T>
    {
        private readonly IDictionary<K, T> _innerDictionary;
        private readonly IList<Func<T, bool>> _valueConstraints;

        public ConstrainableDictionary(IDictionary<K, T> data = null)
        {
            _innerDictionary = data ?? new Dictionary<K, T>();
            _valueConstraints = new List<Func<T, bool>>();
        }
        public ConstrainableDictionary(Func<T, bool> constraint)
        {
            _innerDictionary = new Dictionary<K, T>();
            _valueConstraints = new List<Func<T, bool>>();

            AddContraint(constraint);
        }

        public void AddContraint(Func<T, bool> constraint)
        {
            _valueConstraints.Add(constraint);
        }

        public T this[K key]
        {
            get
            {
                return _innerDictionary[key];
            }
            set
            {
                Validate(value);

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
            Validate(item.Value);

            _innerDictionary.Add(item);
        }

        public void Add(K key, T value)
        {
            Validate(value);

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

        private void Validate(T value)
        {
            if (_valueConstraints.Any(c => !c(value)))
            {
                throw new ArgumentException("Constraint violation");
            }
        }
    }
}