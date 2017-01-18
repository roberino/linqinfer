using System;
using System.Collections.Generic;

namespace LinqInfer.Utility
{
    internal class DynamicEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _compareFunc;
        private readonly Func<T, int> _hashCodeFunc;
        private readonly bool _isClass;

        public DynamicEqualityComparer(Func<T, T, bool> compareFunc, Func<T, int> hashCodeFunc = null)
        {
            _isClass = typeof(T).GetTypeInf().IsClass;
            _compareFunc = compareFunc;
            _hashCodeFunc = hashCodeFunc ?? (x => (_isClass && x == null) ? 0 : x.GetHashCode());
        }

        public bool Equals(T x, T y)
        {
            if (_isClass && (x == null)) return false;
            if (_isClass && ReferenceEquals(x, y)) return true;
            return _compareFunc(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _hashCodeFunc(obj);
        }
    }
}
