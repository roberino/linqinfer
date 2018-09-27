using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    class FunctionProvider : IFunctionProvider
    {
        readonly IFunctionBinder _globalFunctionBinder;
        readonly IDictionary<Type, FunctionBinder> _binders;
        readonly List<Type[]> _assemblyTypes;

        public FunctionProvider(IFunctionBinder customGlobalBinder = null)
        {
            _globalFunctionBinder = customGlobalBinder ?? new GlobalStaticFunctions();
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
                _binders[type] = binder = new FunctionBinder(type, BindingFlags.Instance);
            }

            return binder;
        }
    }
}