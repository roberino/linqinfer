using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        public TypeCompatibility GetTypeCompatibility(ParameterInfo paramType, Type argType)
        {
            if (argType.IsGenericType &&
                paramType.ParameterType.IsGenericType)
            {
                if (argType.GetGenericTypeDefinition() == paramType.ParameterType.GetGenericTypeDefinition()
                    && IsGenericArgMatch(argType.GenericTypeArguments, paramType.ParameterType.GenericTypeArguments))
                {
                    return TypeCompatibility.CompatibleGeneric;
                }

                return TypeCompatibility.Incompatible;
            }

            var rc = RequiresConversion(paramType.ParameterType, argType);

            if (rc.HasValue)
            {
                if (rc.Value)
                {
                    return TypeCompatibility.RequiresConversion;
                }

                return TypeCompatibility.Compatible;
            }

            return TypeCompatibility.Incompatible;
        }

        public bool? RequiresConversion(Type targetType, Type sourceType, bool allowGenerics = true)
        {
            if (targetType == null || sourceType == null)
            {
                return null;
            }

            if (targetType == sourceType)
            {
                return false;
            }

            var tcX = Type.GetTypeCode(targetType);
            var tcY = Type.GetTypeCode(sourceType);

            if (tcX.IsNumeric() && tcY.IsNumeric())
            {
                return true;
            }

            if (allowGenerics && tcX == TypeCode.Object && 
                targetType.IsGenericType && sourceType.IsGenericType && 
                targetType.GetGenericTypeDefinition() == sourceType.GetGenericTypeDefinition() && 
                targetType.HasGenericParameters())
            {
                return false;
            }

            return null;
        }

        static bool IsGenericArgMatch(Type[] args, Type[] paramTypes)
        {
            if (args.Length != paramTypes.Length)
            {
                return false;
            }

            foreach (var (arg, par) in args.Zip(paramTypes))
            {
                if (arg == par || par.IsAssignableFrom(arg))
                {
                    continue;
                }

                if (par.IsGenericParameter)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public enum TypeCompatibility
        {
            Incompatible,
            RequiresConversion,
            Compatible,
            CompatibleGeneric
        }
    }
}