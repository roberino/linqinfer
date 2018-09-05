using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqInfer.Utility.Expressions
{
    class FunctionBinder
    {
        readonly IDictionary<string, MethodInfo[]> _methods;

        public FunctionBinder(Type type, BindingFlags bindingFlags)
        {
            _methods =
                type
                    .GetTypeInf()
                    .GetMethods(BindingFlags.Public | bindingFlags)
                    .GroupBy(m => m.Name)
                    .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public bool IsDefined(string name)
        {
            return _methods.ContainsKey(name);
        }

        public Func<Expression> GetFunctionBinding(string name, IReadOnlyCollection<UnboundParameter> parameters, Expression instance = null)
        {
            var method = BindToMethod(name, parameters);

            var resolver = new InferredTypeResolver();

            resolver.InferAll(method, parameters);

            foreach (var parameter in parameters)
            {
                parameter.Resolve();
            }

            var args = Convert(parameters.Select(p => p.Expression), method.GetParameters()).ToArray();
            
            resolver.InferAll(method, parameters);

            if (method.IsGenericMethodDefinition)
            {
                var genTypes = method.GetGenericArguments().Select(a => resolver.TryConstructType(a)).ToArray();

                method =  method.MakeGenericMethod(genTypes);
            }

            return () => Expression.Call(instance, method, args);
        }

        public Expression GetFunction(string name, IEnumerable<Expression> parameters)
        {
            var method = _methods[name].FirstOrDefault(m => IsParameterMatch(m.GetParameters(), parameters.Select(p => p.Type)));

            if (method == null)
            {
                throw new ArgumentException();
            }

            return Expression.Call(null, method, Convert(parameters, method.GetParameters()));
        }

        MethodInfo BindToMethod(string name, IReadOnlyCollection<UnboundParameter> parameters)
        {
            var method = _methods[name]
                .Select(m => new
                {
                    m,
                    score = ParameterScore(m.GetParameters(), parameters)
                })
                .OrderByDescending(x => x.score)
                .FirstOrDefault();

            if (method == null || method.score == 0)
            {
                throw new ArgumentException();
            }

            return method.m;
        }

        static bool IsParameterMatch(IEnumerable<ParameterInfo> parameters, IEnumerable<Type> argTypes)
        {
            return new HashSet<Type>(parameters.Select(p => p.ParameterType), TypeEqualityComparer.Instance).SetEquals(argTypes);
        }

        static int ParameterScore(IReadOnlyCollection<ParameterInfo> parameters, IReadOnlyCollection<UnboundParameter> args)
        {
            if (parameters.Count != args.Count)
            {
                return 0;
            }
            
            var s = 1001;

            foreach (var pair in parameters.Zip(args, (p, a) => (p, a)))
            {
                if (pair.a.IsInferred)
                {
                    var inferredArgs = InferredTypeResolver.GetInferredArgs(pair.p.ParameterType);

                    if (inferredArgs.inputs.Length == pair.a.ParameterNames.Length)
                    {
                        continue;
                    }

                    return 0;
                }

                var typeCompat = TypeEqualityComparer.Instance.GetTypeCompatibility(pair.p, pair.a.Type);

                switch (typeCompat)
                {
                    case TypeEqualityComparer.TypeCompatibility.Incompatible:
                        return 0;
                    case TypeEqualityComparer.TypeCompatibility.Compatible:
                        s += 1001;
                        break;
                    case TypeEqualityComparer.TypeCompatibility.CompatibleGeneric:
                    case TypeEqualityComparer.TypeCompatibility.RequiresConversion:
                        s += 1000;
                        break;
                }
            }

            return s;
        }

        static IEnumerable<Expression> Convert(IEnumerable<Expression> expressions,
            IEnumerable<ParameterInfo> parameters)
        {
            foreach (var (e, t) in expressions.Zip(parameters.Select(p => p.ParameterType)))
            {
                if (TypeEqualityComparer.Instance.RequiresConversion(t, e.Type).GetValueOrDefault())
                {
                    if (e.NodeType == ExpressionType.Constant && e is ConstantExpression ce)
                    {
                        yield return Expression.Constant(System.Convert.ChangeType(ce.Value, t));
                        continue;
                    }

                    yield return Expression.Convert(e, t);
                    continue;
                }

                yield return e;
            }
        }

        IEnumerable<MethodInfo> GetExtensionMethods(Type extendedType)
        {
            return 
                from method in _methods.SelectMany(m => m.Value)
                where method.IsDefined(typeof(ExtensionAttribute), false)
                where method.GetParameters()[0].ParameterType.IsAssignableFrom(extendedType)
                select method;
        }
    }
}