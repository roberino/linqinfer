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

        public static Expression ToTuple(IReadOnlyCollection<Expression> parameters)
        {
            var createMethod = typeof(ValueTuple)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(n => n.Name == nameof(Tuple.Create))
                .Single(m => m.GetParameters().Length == parameters.Count);

            var closedMethod = createMethod.IsGenericMethodDefinition 
                ? createMethod.MakeGenericMethod(parameters.Select(p => p.Type).ToArray())
                : createMethod;

            return Expression.Call(closedMethod, parameters);
        }

        public static Expression ToAsyncPromise(Expression taskParam)
        {
            var type = taskParam.Type.GetGenericArguments().Single();

            var promiseType = typeof(AsyncPromise<>).MakeGenericType(type);

            var createMethod = promiseType.GetMethod(nameof(AsyncPromise<object>.Create),
                BindingFlags.Static | BindingFlags.Public);

            return Expression.Call(createMethod, taskParam);
        }

        public static bool IsPromise(this Expression expression)
        {
            return expression.Type.PromiseType() != null;
        }

        public static Type PromiseType(this Type type)
        {
            if (type.IsPrimitive)
            {
                return null;
            }

            bool IsPromiseIFace(Type t)
            {
                return t.IsGenericType
                       && t.GetGenericTypeDefinition() == typeof(IPromise<>);
            }

            var pType = IsPromiseIFace(type) ? type : type
                .GetInterfaces()
                .FirstOrDefault(IsPromiseIFace);

            return pType?.GenericTypeArguments.First();
        }

        public static (Expression left, Expression right) MakeCompatible(Expression left, Expression right)
        {
            var rc = TypeEqualityComparer.Instance.RequiresConversion(left.Type, right.Type, false);

            if (!rc.HasValue)
            {
                throw new ArgumentException("Incompatible types");
            }

            if (!rc.Value)
            {
                return (left, right);
            }

            var tc = Type.GetTypeCode(right.Type);

            if (tc == TypeCode.Double)
            {
                return (left.ConvertToType(right.Type), right);
            }
                
            return (left, right.ConvertToType(left.Type));

        }

        public static Expression ConvertToType(this Expression expression, Type type)
        {
            if (expression.Type == type)
            {
                return expression;
            }

            if (expression.IsPromise())
            {
                return ConvertToType(Expression.Property(expression, nameof(IPromise<object>.Result)), type);
            }

            if (expression.NodeType == ExpressionType.Constant && expression is ConstantExpression ce)
            {
                return Expression.Constant(System.Convert.ChangeType(ce.Value, type));
            }

            return Expression.Convert(expression, type);
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