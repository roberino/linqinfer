using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    class InferredTypeResolver
    {
        readonly IDictionary<Type, Type> _mappings;

        public InferredTypeResolver()
        {
            _mappings = new Dictionary<Type, Type>();
        }

        public void InferAll(MethodInfo method, IReadOnlyCollection<UnboundArgument> parameters)
        {
            var actualParameters = method.GetParameters();

            foreach (var (a, p) in actualParameters.Zip(parameters))
            {
                p.Type = a.ParameterType;

                if (p.Expression != null)
                {
                    Infer(a.ParameterType, p.Expression.Type);
                }
                else
                {
                    if (p.Type.IsSubclassOf(typeof(Expression)) || p.Type.IsFunc())
                    {
                        var (inputs, output) = GetInferredArgs(p.Type);

                        p.InputTypes = inputs.Zip(p.Parameters).Select(x => x.b.IsTypeKnown ? x.b.Type : x.a).ToArray();
                        p.OutputType = output;
                    }
                }
            }

            foreach (var p in parameters)
            {
                p.Type = TryConstructType(p.Type);

                if ((p.OutputType?.IsGenericParameter).GetValueOrDefault())
                {
                    p.OutputType = TryConstructType(p.OutputType);
                }

                if (p.InputTypes != null)
                {
                    for (var x = 0; x < p.InputTypes.Length; x++)
                    {
                        if (p.InputTypes[x].IsGenericParameter)
                        {
                            p.InputTypes[x] = TryConstructType(p.InputTypes[x]);
                        }
                    }
                }
            }
        }

        public Type TryConstructType(Type generic)
        {
            if (generic.IsGenericParameter)
            {
                return TryLookup(generic);
            }

            if (generic.IsGenericType)
            {
                var typeArgs = generic.GenericTypeArguments.Select(TryConstructType).ToArray();
                
                return generic.GetGenericTypeDefinition().MakeGenericType(typeArgs);
            }

            return generic;
        }

        public void Infer(Type parameter, Type arg)
        {
            if (parameter.IsGenericParameter)
            {
                _mappings[parameter] = arg;
                return;
            }

            if (!parameter.IsGenericType)
            {
                return;
            }

            if (arg.IsCompatibleArrayAndGeneric(parameter.GetGenericTypeDefinition()))
            {
                _mappings[parameter.GenericTypeArguments.Single()] = arg.GetElementType();
            }

            foreach (var (a, b) in parameter.GenericTypeArguments.Zip(arg.GenericTypeArguments))
            {
                Infer(a, b);
            }
        }

        public Type TryLookup(Type genericArg)
        {
            return genericArg.IsGenericParameter ? _mappings.TryGetValue(genericArg, out var x) ? x : genericArg : genericArg;
        }

        public static (Type[] inputs, Type output) GetInferredArgs(Type typeToInferFrom)
        {
            // e.g. Expression<Func<int, double>>;

            if (typeToInferFrom.Name.StartsWith("Expression") &&
                typeToInferFrom.Namespace == typeof(Expression).Namespace)
            {
                typeToInferFrom = typeToInferFrom
                    .GetGenericArguments()
                    .SelectIf(x => x.Count() == 1, x => x)
                    .FirstOrDefault();

                if (typeToInferFrom == null)
                {
                    return (new Type[0], null);
                }
            }

            return GetFuncArgs(typeToInferFrom);
        }

        static (Type[] inputs, Type output) GetFuncArgs(Type funcType)
        {
            // e.g. Func<int, double>;

            if (!(funcType.IsGenericType && funcType.GetGenericTypeDefinition().Name.StartsWith("Func") &&
                  funcType.Namespace == typeof(Func<,>).Namespace))
            {
                return (new Type[0], null);
            }

            var funcArgs = funcType.GetGenericArguments();

            return (funcArgs.Take(funcArgs.Length - 1).ToArray(), funcArgs.Last());
        }
    }
}
