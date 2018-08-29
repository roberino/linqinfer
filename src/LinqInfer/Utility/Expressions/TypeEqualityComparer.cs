using System;
using System.Collections.Generic;

namespace LinqInfer.Utility.Expressions
{
    class TypeEqualityComparer : IEqualityComparer<Type>
    {
        TypeEqualityComparer()
        {
        }

        public bool Equals(Type x, Type y)
        {
            var convert = RequiresConversion(x, y);

            return convert.HasValue;
        }

        public int GetHashCode(Type obj)
        {
            return Type.GetTypeCode(obj).IsNumeric() ? typeof(double).GetHashCode() : obj.GetHashCode();
        }

        public static readonly TypeEqualityComparer Instance = new TypeEqualityComparer();

        public bool? RequiresConversion(Type x, Type y)
        {
            if (x == null || y == null)
            {
                return null;
            }

            if (x == y)
            {
                return false;
            }

            var tcX = Type.GetTypeCode(x);
            var tcY = Type.GetTypeCode(y);

            if (tcX.IsNumeric() && tcY.IsNumeric())
            {
                return true;
            }

            return null;
        }
    }
}