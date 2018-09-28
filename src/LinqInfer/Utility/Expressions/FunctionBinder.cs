using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqInfer.Utility.Expressions
{
    class FunctionBinder : IFunctionBinder
    {
        readonly IDictionary<string, MethodInfo[]> _methods;

        public FunctionBinder(Type type, BindingFlags bindingFlags)
        {
            _methods =
                type
                    .GetTypeInf()
                    .GetMethods(BindingFlags.Public | bindingFlags)
                    .Where(m => m.CustomAttributes.All(a => a.AttributeType != typeof(NonBound)))
                    .GroupBy(m => m.Name)
                    .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public bool IsDefined(string name)
        {
            return _methods.ContainsKey(name);
        }

        public Expression BindToFunction(string name, IReadOnlyCollection<UnboundArgument> parameters)
        {
            return BindToFunction(name, parameters, null);
        }

        public Expression BindToFunction(string name, IReadOnlyCollection<UnboundArgument> parameters, Expression instance = null)
        {
            var method = BindToMethod(name, parameters);

            var resolver = new InferredTypeResolver();

            resolver.InferAll(method, parameters);

            var isFirstArg = true;

            foreach (var parameter in parameters)
            {
                if (!isFirstArg && parameter.HasUnresolvedTypes)
                {
                    resolver.InferAll(method, parameters);
                }

                parameter.Resolve();

                isFirstArg = false;
            }

            var args = Convert(parameters.Select(p => p.Expression), method.GetParameters()).ToArray();
            
            resolver.InferAll(method, parameters);

            if (method.IsGenericMethodDefinition)
            {
                var genTypes = method.GetGenericArguments().Select(a => resolver.TryConstructType(a)).ToArray();

                method =  method.MakeGenericMethod(genTypes);
            }

            return Expression.Call(instance, method, args);
        }

        MethodInfo BindToMethod(string name, IReadOnlyCollection<UnboundArgument> parameters)
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

        static int ParameterScore(IReadOnlyCollection<ParameterInfo> parameters, IReadOnlyCollection<UnboundArgument> args)
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
                    yield return e.ConvertToType(t);
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