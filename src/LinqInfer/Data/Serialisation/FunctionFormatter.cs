using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LinqInfer.Data.Serialisation
{
    internal sealed class FunctionFormatter
    {
        public IFactory<TResult, string> CreateFactory<TResult>()
            where TResult : class
        {
            return new Factory<TResult, TResult>(this);
        }

        public IFactory<TResult, string> CreateFactory<TResult, TProvider>()
            where TProvider : class, TResult
        {
            return new Factory<TResult, TProvider>(this);
        }

        public TOutput BindToStaticMethod<TInstance, TOutput>(string formattedFunction) where TInstance : class
        {
            return Bind<TInstance, TOutput>(formattedFunction, null, GetMethodSelector(BindingFlags.Public | BindingFlags.Static));
        }

        public TOutput BindToInstance<TInstance, TOutput>(string formattedFunction, TInstance instance) where TOutput : class
        {
            return Bind<TInstance, TOutput>(formattedFunction, instance, GetMethodSelector(BindingFlags.Instance | BindingFlags.Public));
        }

        public TOutput Bind<TInstance, TOutput>(string formattedFunction, TInstance instance, Func<Type, string, MethodInfo> methodSelector)
        {
            var type = typeof(TInstance);

            if (methodSelector == null)
            {
                methodSelector = (t, n) => t.GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == n).FirstOrDefault();
            }

            var paramInfo = Parse(formattedFunction);
            var method = methodSelector(type, paramInfo.Name);
            var parameters = Bind(paramInfo.Parameters, method.GetParameters());

            return (TOutput)method.Invoke(instance, parameters);
        }

        public T Create<T>(string formattedFunction) where T : class
        {
            var type = typeof(T);
            
            var paramInfo = Parse(formattedFunction);

            if (!string.Equals(type.Name, paramInfo.Name))
            {
                return BindToStaticFactoryMethod<T>(formattedFunction);
            }

            ConstructorInfo ctr = type
                .GetTypeInfo()
                .GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length == paramInfo.Parameters.Count);

            if (ctr == null) throw new ArgumentException("No matching constructor found");

            var parameters = Bind(paramInfo.Parameters, ctr.GetParameters());

            return (T)ctr.Invoke(parameters);
        }

        public string Format<T>(T instance, params object[] parameters)
        {
            return Format(instance, x => parameters);
        }

        public string Format<T>(T instance, Func<T, IEnumerable<object>> parameterSelector, string name = null)
        {
            var sb = new StringBuilder();
            var first = true;

            sb.Append(name ?? instance.GetType().Name);
            sb.Append("(");


            foreach (var p in parameterSelector(instance))
            {
                if (!first)
                {
                    sb.Append(",");
                }
                else
                {
                    first = false;
                }

                sb.Append(p);
            }

            sb.Append(")");

            return sb.ToString();
        }

        internal TInstance BindToStaticFactoryMethod<TInstance>(string formattedFunction) where TInstance : class
        {
            return Bind<TInstance, TInstance>(formattedFunction, null, GetMethodSelector(BindingFlags.Public | BindingFlags.Static));
        }

        private Func<Type, string, MethodInfo> GetMethodSelector(BindingFlags bindingFlags)
        {
            return (t, n) => t.GetTypeInfo().GetMethods(bindingFlags).Where(m => m.Name == n).FirstOrDefault();
        }

        private object[] Bind(IList<string> parameters, ParameterInfo[] parameterInfos)
        {
            var values = new object[parameterInfos.Length];

            for (var i = 0; i < parameterInfos.Length; i++)
            {
                values[i] = Convert.ChangeType(parameters[i], parameterInfos[i].ParameterType);
            }

            return values;
        }

        private FuncParams Parse(string formattedFunction)
        {
            var funcParams = new FuncParams();
            var sb = new StringBuilder();
            var parsingName = true;

            foreach (var c in formattedFunction)
            {
                if (parsingName)
                {
                    if (c == '(')
                    {
                        funcParams.Name = sb.ToString().Trim();
                        parsingName = false;
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == ',' || c == ')')
                    {
                        funcParams.Parameters.Add(sb.ToString().Trim());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            return funcParams;
        }

        private class FuncParams
        {
            public IList<string> Parameters { get; } = new List<string>();
            public string Name { get; set; }
        }

        private class Factory<TResult, TProvider> 
            : IFactory<TResult, string> 
            where TProvider : class, TResult
        {
            private readonly FunctionFormatter _formatter;

            public Factory(FunctionFormatter formatter)
            {
                _formatter = formatter;
            }

            public TResult Create(string parameters)
            {
                return _formatter.Create<TProvider>(parameters);
            }
        }
    }
}