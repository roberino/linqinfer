using LinqInfer.Utility;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class FunctionBinder
    {
        private readonly IObjectSerialiser _serialiser;

        public FunctionBinder(IObjectSerialiser serialiser)
        {
            Contract.Assert(serialiser != null);
            _serialiser = serialiser;
        }

        public Func<IOwinContext, Task> BindToAsyncMethod<TArg, TResult>(Func<TArg, Task<TResult>> func)
        {
            return BindToAsyncMethod(func, default(TArg), false);
        }

        public Func<IOwinContext, Task> BindToAsyncMethod<TArg, TResult>(Func<TArg, Task<TResult>> func, TArg defaultValue)
        {
            return BindToAsyncMethod(func, defaultValue, true);
        }

        private Func<IOwinContext, Task> BindToAsyncMethod<TArg, TResult>(Func<TArg, Task<TResult>> func, TArg defaultValue = default(TArg), bool fallbackToDefault = false)
        {
            var argType = typeof(TArg);

            var exec = new Func<TArg, IOwinContext, Task>(async (a, c) =>
            {
                var result = await func(a);

                var writer = c.Response.CreateTextResponse();

                await _serialiser.Serialise(result, c.Response.Content, c.Response.Header.TextEncoding);

                c.Response.Header.MimeType = _serialiser.MimeType;
            });

            if (Type.GetTypeCode(argType) == TypeCode.Object)
            {
                return async c =>
                {
                    var arg = ParamsToObject(c, defaultValue, fallbackToDefault);

                    await exec(arg, c);
                };
            }
            else
            {
                var p = func.Method.GetParameters().First().Name;

                return async c =>
                {
                    TArg arg = defaultValue;

                    try
                    {
                        arg = ParamsToPrimative<TArg>(c, p);
                    }
                    catch (ArgumentException)
                    {
                        if (!fallbackToDefault) throw;
                    }

                    await exec(arg, c);
                };
            }
        }

        private T ParamsToObject<T>(IOwinContext context, T defaultValue, bool allowDefaults)
        {
            var type = typeof(T);

            var defaultIsNull = !type.IsValueType && (defaultValue as object) == null;
            var instance = defaultValue;
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (defaultIsNull)
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);

                if (ctor == null) throw new ArgumentException("No default constructor");

                instance = (T)ctor.Invoke(new object[0]);
            }

            int i = 0;

            var values = properties.Select(p => new
            {
                prop = p,
                index = i++,
                val = ParamsToPrimative(p.PropertyType, context, p.Name)
            }).ToList();

            var missing = values.Where(v => !v.val.Item2).ToList();

            if ((!allowDefaults || defaultIsNull) && missing.Any())
            {
                throw new ArgumentException("Parameter(s) not found: " + string.Join(",", missing.Select(m => m.prop.Name)));
            }

            if (!properties.All(p => p.CanWrite))
            {
                if (missing.Count == values.Count) return instance;

                var conarg = values.Select(v => v.val.Item1).ToArray();

                foreach (var prop in missing)
                {
                    conarg[prop.index] = prop.prop.GetValue(instance);
                }

                instance = (T)Activator.CreateInstance(type, conarg);
            }
            else
            {
                foreach (var value in values.Where(v => v.val.Item2))
                {
                    value.prop.SetValue(instance, value.val.Item1);
                }
            }

            return instance;
        }

        private T ParamsToPrimative<T>(IOwinContext context, string name)
        {
            var val = ParamsToPrimative(typeof(T), context, name);

            if (!val.Item2) throw new ArgumentException("Parameter not found: " + name);

            if (val.Item1 == null) return default(T);

            return (T)val.Item1;
        }

        private Tuple<object, bool> ParamsToPrimative(Type type, IOwinContext context, string name)
        {
            object val;

            if (context.TryGetValue("route." + name, out val))
            {
                return new Tuple<object, bool>(Convert.ChangeType(val, type), true);
            }
            else
            {
                var nblType = type.GetNullableTypeType();

                return new Tuple<object, bool>(null, nblType != null);
            }
        }
    }
}