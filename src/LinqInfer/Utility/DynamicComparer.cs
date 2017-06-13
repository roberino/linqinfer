﻿using System;
using System.Collections.Generic;

namespace LinqInfer.Utility
{
    internal class DynamicComparer<T> : IComparer<T>, IEqualityComparer<T>
    {
        private readonly Func<T, T, int> _compareFunc;
        private readonly Func<T, int> _hashCodeFunc;
        private readonly bool _isClass;
        private readonly bool _hasCustomHashFunc;

        public DynamicComparer(Func<T, T, int> compareFunc, Func<T, int> hashCodeFunc = null)
        {
            _isClass = typeof(T).GetTypeInf().IsClass;
            _compareFunc = compareFunc;
            _hasCustomHashFunc = hashCodeFunc != null;
            _hashCodeFunc = hashCodeFunc ?? (x => (_isClass && x == null) ? 0 : x.GetHashCode());
        }

        public int Compare(T x, T y)
        {
            return _compareFunc(x, y);
        }

        public bool Equals(T x, T y)
        {
            if (_isClass && (x == null)) return false;
            if (_isClass && ReferenceEquals(x, y)) return true;
            return _compareFunc(x, y) == 0;
        }

        public int GetHashCode(T obj)
        {
            return _hashCodeFunc(obj);
        }

        public override int GetHashCode()
        {
            if (_hasCustomHashFunc)
            {
                return _hashCodeFunc.GetHashCode() * 7 + _compareFunc.GetHashCode();
            }

            return _compareFunc.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var oc = obj as DynamicComparer<T>;

            if (oc == null) return false;

            return oc._compareFunc.Equals(_compareFunc) && ((!oc._hasCustomHashFunc && !_hasCustomHashFunc) || oc._hashCodeFunc.Equals(_hashCodeFunc));
        }
    }
}
