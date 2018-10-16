using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqInfer.Utility.Expressions
{
    class FunctionProvider : IFunctionProvider
    {
        readonly IFunctionBinder _globalFunctionBinder;
        readonly IDictionary<Type, FunctionBinder> _binders;
        readonly List<Type[]> _assemblyTypes;

        public FunctionProvider(IFunctionBinder customGlobalBinder = null)
        {
            _globalFunctionBinder = customGlobalBinder ?? GlobalStaticFunctions.Default();
            _binders = new Dictionary<Type, FunctionBinder>();
            _assemblyTypes = new List<Type[]>
            {
                typeof(FunctionBinder).Assembly.ExportedTypes.ToArray(),
                new[] {typeof(Enumerable)}
            };
        }

        public FunctionProvider RegisterAssemblies(IEnumerable<Assembly> assembly)
        {
            _assemblyTypes.AddRange(assembly.Select(a => a.ExportedTypes.ToArray()));

            return this;
        }

        public FunctionProvider RegisterStaticTypes(IEnumerable<Type> types)
        {
            _assemblyTypes.Add(types.ToArray());
            return this;
        }

        public IFunctionBinder GetGlobalBinder()
        {
            return _globalFunctionBinder;
        }

        public IFunctionBinder GetStaticBinder(string typeName)
        {
            var type = _assemblyTypes[0].SingleOrDefault(t => t.Name == typeName);

            if (type == null && _assemblyTypes.Count > 1)
            {
                type = _assemblyTypes.SelectMany(a => a).SingleOrDefault(t => t.Name == typeName);
            }

            if (type == null)
            {
                throw new MemberAccessException();
            }

            return new FunctionBinder(type, BindingFlags.Static);
        }

        public IFunctionBinder GetBinder(Type type)
        {
            if (!_binders.TryGetValue(type, out FunctionBinder binder))
            {
                var extensions = GetExtensionMethods(type).ToArray();

                _binders[type] = binder = new FunctionBinder(type, BindingFlags.Instance, extensions);
            }

            return binder;
        }

        IEnumerable<MethodInfo> GetExtensionMethods(Type extendedType)
        {
            return 
                from method in StaticMethods()
                where method.IsDefined(typeof(ExtensionAttribute), false)
                where method.GetParameters()[0].ParameterType.IsAssignableFrom(extendedType)
                select method;
        }

        IEnumerable<MethodInfo> StaticMethods()
        {
            return _assemblyTypes
                .SelectMany(t => t)
                .Where(t => !t.IsValueType)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static));
        }
    }
}