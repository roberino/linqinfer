using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace LinqInfer.Utility.Expressions
{
    class FunctionBinder : IFunctionBinder
    {
        readonly IDictionary<string, MethodInfo[]> _extensionMethods;
        readonly IDictionary<string, MethodInfo[]> _methods;

        public FunctionBinder(Type type, BindingFlags bindingFlags, MethodInfo[] extensionMethods = null)
        {
            _extensionMethods = ToDictionary(extensionMethods ?? new MethodInfo[0]);

            _methods =
                ToDictionary(type
                    .GetTypeInf()
                    .GetMethods(BindingFlags.Public | bindingFlags)
                    .Concat(type.GetInterfaces().SelectMany(i => i.GetMethods(BindingFlags.Public | bindingFlags))));
        }
        
        [NonBound]
        public virtual bool IsDefined(string name)
        {
            return _methods.ContainsKey(name);
        }
        
        [NonBound]
        public virtual Expression BindToFunction(string name, IReadOnlyCollection<UnboundArgument> parameters, Expression instance = null)
        {
            if (instance != null && instance.Type.IsSubclassOf(typeof(Task)))
            {
                instance = ConversionFunctions.ToAsyncPromise(instance);

                return new FunctionBinder(instance.Type, BindingFlags.Public | BindingFlags.Instance).BindToFunction(
                    name, parameters, instance);
            }

            var (method, isExtension, actualParameters) = BindToMethod(name, instance, parameters);

            var resolver = new InferredTypeResolver();

            foreach (var parameter in parameters)
            {
                if (parameter.HasUnresolvedTypes)
                {
                    resolver.InferAll(method, parameters);
                }

                parameter.Resolve(resolver);
            }

            var args = Convert(actualParameters.Select(p => p.Expression), method.GetParameters()).ToArray();

            resolver.InferAll(method, actualParameters);

            if (method.IsGenericMethodDefinition)
            {
                var genTypes = method.GetGenericArguments().Select(a => resolver.TryConstructType(a)).ToArray();

                method = method.MakeGenericMethod(genTypes);
            }

            return Expression.Call(isExtension ? null : instance, method, args);
        }

        (MethodInfo method, bool isExtension, IReadOnlyCollection<UnboundArgument> parameters) BindToMethod(string name, Expression instance, IReadOnlyCollection<UnboundArgument> parameters)
        {
            var ext = false;
            var method = BindToMethod(_methods, name, parameters);
            var newParams = parameters;

            if (method == null && instance != null)
            {
                var thisArg = new UnboundArgument(instance);
                newParams = new[] { thisArg }.Concat(parameters).ToArray();

                method = BindToMethod(_extensionMethods, name, newParams);
                ext = method != null;
            }

            if (method == null)
            {
                throw new ArgumentException(name);
            }

            return (method, ext, newParams);
        }

        static MethodInfo BindToMethod(IDictionary<string, MethodInfo[]> methods, string name, IReadOnlyCollection<UnboundArgument> parameters)
        {
            if (!methods.TryGetValue(name, out var matches))
            {
                return null;
            }

            var method = matches
                .Select(m => new
                {
                    m,
                    score = ParameterScore(m.GetParameters(), parameters)
                })
                .OrderByDescending(x => x.score)
                .FirstOrDefault();

            if (method == null || method.score == 0)
            {
                return null;
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

                    if (inferredArgs.inputs.Length == pair.a.Parameters.Length)
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

        static IDictionary<string, MethodInfo[]> ToDictionary(IEnumerable<MethodInfo> methods)
        {
            return methods.Distinct()
                .Where(m => m.CustomAttributes.All(a => a.AttributeType != typeof(NonBound)))
                .GroupBy(m => m.Name)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }
    }
}