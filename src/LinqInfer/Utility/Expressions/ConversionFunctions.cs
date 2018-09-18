using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    //Xml
    //Csv
    //File
    //Folder
    //Bind
    //ToJson
    //ToFile

    static class ConversionFunctions
    {
        public static bool IsDefined(string name)
        {
            switch (name)
            {
                case nameof(ToInteger):
                case nameof(ToFloat):
                case nameof(ToString):
                case nameof(Vector):
                case nameof(BitVector):
                case nameof(OneOfNVector):
                case nameof(Matrix):
                case nameof(Convert):
                // case nameof(Xml):
                    return true;
            }

            return false;
        }

        //public static XDocument Xml(Expression[] parameters)
        //{
        //    if (parameters.Length == 0 && parameters[0].Type == typeof(string))
        //    {
        //        Expression.Call()
        //    }
        //    return XDocument.Load(path);
        //}

        public static Expression GetFunction(string name, IReadOnlyCollection<UnboundArgument> unboundParameters)
        {
            var parameters = unboundParameters.Select(u => u.Resolve()).ToArray();

            switch (name)
            {
                case nameof(ToInteger):
                    return ToInteger(parameters);
                case nameof(ToFloat):
                    return ToFloat(parameters);
                case nameof(ToString):
                    return ToString(parameters);
                case nameof(Vector):
                    return Vector(parameters);
                case nameof(BitVector):
                    return BitVector(parameters);
                case nameof(OneOfNVector):
                    return OneOfNVector(parameters);
                case nameof(Matrix):
                    return Matrix(parameters);
                case nameof(Convert):
                    return Convert(parameters);
            }

            throw new NotSupportedException(name);
        }

        static Expression Convert(IEnumerable<Expression> parameters)
        {
            return Expression.Convert(parameters.First(), (Type)((ConstantExpression)parameters.Last()).Value);
        }

        static IEnumerable<Expression> ConvertAll<T>(params Expression[] parameters)
        {
            var type = typeof(T);

            return parameters.Select(p => Expression.Convert(p, type));
        }

        static Expression ConvertOne<T>(Expression parameter)
        {
            var type = typeof(T);

            return Expression.Convert(parameter, type);
        }

        static Expression ToInteger(IEnumerable<Expression> parameters)
        {
            return Expression.Convert(parameters.Single(), typeof(int));
        }

        static Expression ToFloat(IEnumerable<Expression> parameters)
        {
            return Expression.Convert(parameters.Single(), typeof(double));
        }

        static Expression ToString(IEnumerable<Expression> parameters)
        {
            return Expression.Convert(parameters.Single(), typeof(string));
        }

        static Expression Matrix(IEnumerable<Expression> parameters)
        {
            if (parameters.Count() == 1 && parameters.Single().Type == typeof(double[][]))
            {
                return Expression.New(MatrixMethod, parameters.Single());
            }

            throw new ArgumentException();
        }

        static Expression BitVector(IEnumerable<Expression> parameters)
        {
            Expression arrParam = null;

            var paramArray = parameters.ToArray();

            if (paramArray.Length == 1 && paramArray.Single().Type == typeof(bool[]))
            {
                arrParam = paramArray.Single();
            }
            else
            {
                if (paramArray.Any(p => p.Type != typeof(bool)))
                {
                    paramArray = ConvertAll<bool>(paramArray).ToArray();
                }

                arrParam = Expression.NewArrayInit(typeof(bool), paramArray);
            }

            return Expression.New(BitVectorMethod, arrParam);
        }

        static Expression OneOfNVector(IEnumerable<Expression> parameters)
        {
            var paramArray = parameters.ToArray();

            switch (paramArray.Length)
            {
                case 1:
                    return Expression.New(OneOfNMethod, ConvertOne<int>(paramArray[0]), Expression.New(typeof(int?)));
                case 2:
                    return Expression.New(OneOfNMethod, ConvertOne<int>(paramArray[0]), ConvertOne<int?>(paramArray[1]));
            }

            throw new ArgumentException();
        }

        static Expression Vector(IEnumerable<Expression> parameters)
        {
            Expression paramArr;

            if (parameters.Count() == 1 && parameters.Single().Type == typeof(double[]))
            {
                paramArr = parameters.Single();
            }
            else
            {
                paramArr = Expression.NewArrayInit(typeof(double), parameters);
            }

            return Expression.Call(VectorMethod, paramArr);
        }

        static readonly ConstructorInfo MatrixMethod = typeof(Matrix).GetConstructors()
            .First(c => c.GetParameters().FirstOrDefault()?.ParameterType == typeof(double[][]));

        static readonly ConstructorInfo OneOfNMethod = typeof(OneOfNVector)
            .GetConstructors().First();

        static readonly ConstructorInfo BitVectorMethod = typeof(BitVector)
            .GetConstructor(new[] { typeof(bool[]) });

        static readonly MethodInfo VectorMethod = typeof(ColumnVector1D)
            .GetMethod(nameof(ColumnVector1D.Create));
    }
}