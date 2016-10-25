using LinqInfer.Utility;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
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
            DebugOn = true;
        }

        public bool DebugOn { get; set; }

        public Func<IOwinContext, Task> BindToAsyncMethod<TArg, TResult>(Func<TArg, Task<TResult>> func)
        {
            return BindToAsyncMethod(func, default(TArg), false);
        }

        public Func<IOwinContext, Task> BindToAsyncMethod<TArg, TResult>(Func<TArg, Task<TResult>> func, TArg defaultValue)
        {
            return BindToAsyncMethod(func, defaultValue, true);
        }

        public Func<IOwinContext, Task> BindToSyncMethod<TArg, TResult>(Func<TArg, TResult> func, TArg defaultValue = default(TArg), bool fallbackToDefault = false)
        {
            var argType = typeof(TArg);

            var exec = new Func<TArg, IOwinContext, Task>(async (a, c) =>
            {
                ValidateContextAndAppendDebugInfo(c, func.Method, argType, typeof(TResult));

                var result = func(a);

                await SerialiseAndSetMimeAndStatus(c, result);
            });

            return BindParamsToMethod(exec, func.Method, defaultValue, fallbackToDefault);
        }

        private Func<IOwinContext, Task> BindToAsyncMethod<TArg, TResult>(Func<TArg, Task<TResult>> func, TArg defaultValue = default(TArg), bool fallbackToDefault = false)
        {
            var argType = typeof(TArg);

            var exec = new Func<TArg, IOwinContext, Task>(async (a, c) =>
            {
                ValidateContextAndAppendDebugInfo(c, func.Method, argType, typeof(TResult));

                var result = await func(a);

                await SerialiseAndSetMimeAndStatus(c, result);
            });

            return BindParamsToMethod(exec, func.Method, defaultValue, fallbackToDefault);
        }

        private async Task SerialiseAndSetMimeAndStatus<T>(IOwinContext context, T result)
        {
            var mimeType = context.Request.Header.PreferredMimeType(_serialiser.SupportedMimeTypes);

            await _serialiser.Serialise(result, context.Response.Header.TextEncoding, mimeType, context.Response.Content);

            context.Response.Header.MimeType = mimeType;
            context.Response.Header.StatusCode = 200;
        }

        private Func<IOwinContext, Task> BindParamsToMethod<TArg>(Func<TArg, IOwinContext, Task> exec, MethodInfo innerMethod, TArg defaultValue, bool fallbackToDefault)
        {
            var argType = typeof(TArg);

            if (Type.GetTypeCode(argType) == TypeCode.Object)
            {
                return async c =>
                {
                    var arg = await ParamsToObject(c, defaultValue, fallbackToDefault);

                    await exec(arg, c);
                };
            }
            else
            {
                var p = innerMethod.GetParameters().First().Name;

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

        private void ValidateContextAndAppendDebugInfo(IOwinContext c, MethodInfo innerMethod, Type argType, Type resultType)
        {
            object expectedResponseType;

            if (c.TryGetValue("ext.ExpectedResponseType", out expectedResponseType))
            {
                if (expectedResponseType is Type && !((Type)expectedResponseType).IsAssignableFrom(resultType))
                {
                    throw new ArgumentException("Invalid type - " + expectedResponseType);
                }
            }

            if (DebugOn)
            {
                c.Response.Header.Headers["X-FUNC-BINDING"] = new[] { string.Format("{2} {0} ({1})", innerMethod.Name, argType.Name, resultType.Name) };
            }
        }

        private async Task<T> ParamsToObject<T>(IOwinContext context, T defaultValue, bool allowDefaults)
        {
            var type = typeof(T);

            var defaultIsNull = !type.IsValueType && (defaultValue as object) == null;
            var instance = defaultValue;
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (defaultIsNull)
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);

                if (ctor == null) throw new ArgumentException("No default constructor");

                instance = (T)ctor.Invoke(new object[0]);
            }

            if (context.Request.Content.Length > 0)
            {
                try
                {
                    instance = await _serialiser.Deserialise<T>(context.Request.Content, context.Request.Header.ContentEncoding, context.Request.Header.ContentMimeType);
                }
                catch
                {
                }
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