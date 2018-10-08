using System;
using System.Collections.Generic;
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
            if (paramType.ParameterType.IsGenericType)
            {
                var genType = paramType.ParameterType.GetGenericTypeDefinition();

                if (argType.IsGenericType && argType.GetGenericTypeDefinition() == genType
                    && IsGenericArgMatch(argType.GenericTypeArguments, paramType.ParameterType.GenericTypeArguments))
                {
                    return TypeCompatibility.CompatibleGeneric;
                }

                if (argType.IsCompatibleArrayAndGeneric(genType))
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

            if (tcX == TypeCode.Object)
            {
                if (sourceType.IsSubclassOf(targetType))
                {
                    return false;
                }

                if (allowGenerics &&
                    targetType.IsGenericType && sourceType.IsGenericType &&
                    targetType.GetGenericTypeDefinition() == sourceType.GetGenericTypeDefinition() &&
                    targetType.HasGenericParameters())
                {
                    return false;
                }
            }

            if (tcY != TypeCode.Object)
            {
                return null;
            }

            var pType = sourceType.PromiseType();

            if (pType == null)
            {
                return null;
            }

            var rc = RequiresConversion(targetType, pType);

            if (rc.HasValue)
            {
                return true;
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