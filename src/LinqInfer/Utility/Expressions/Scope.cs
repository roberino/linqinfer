using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    class Scope
    {
        readonly IDictionary<Type, FunctionBinder> _binders;
        readonly List<Type[]> _assemblyTypes;

        public Scope(params ParameterExpression[] parameters) : this((Expression)parameters.First())
        {
        }

        Scope(Expression currentContext, Scope globalContext = null, bool? isRoot = null, Type conversionType = null)
        {
            CurrentContext = currentContext;
            GlobalContext = globalContext ?? this;
            IsRoot = isRoot.GetValueOrDefault(globalContext == null);
            ConversionType = conversionType;

            if (IsRoot)
            {
                _binders = new Dictionary<Type, FunctionBinder>();
                _assemblyTypes = new List<Type[]>
                {
                    typeof(FunctionBinder).Assembly.ExportedTypes.ToArray(),
                    new[] {typeof(Enumerable)}
                };
            }
        }

        public Scope RegisterAssemblies(IEnumerable<Assembly> assembly)
        {
            _assemblyTypes.AddRange(assembly.Select(a => a.ExportedTypes.ToArray()));

            return this;
        }

        public Scope RegisterStaticTypes(IEnumerable<Type> types)
        {
            _assemblyTypes.Add(types.ToArray());
            return this;
        }

        public bool IsRoot { get; }

        public Scope GlobalContext { get; }

        public Expression CurrentContext { get; }

        public Type ConversionType { get; }

        public FunctionBinder GetBinder()
        {
            return GetBinder(CurrentContext.Type);
        }

        public FunctionBinder GetStaticBinder(string typeName)
        {
            if (!IsRoot)
            {
                return GlobalContext.GetStaticBinder(typeName);
            }

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

        public FunctionBinder GetBinder(Type type)
        {
            if (_binders == null) return GlobalContext.GetBinder(type);
            
            if (!_binders.TryGetValue(type, out FunctionBinder binder))
            {
                _binders[type] = binder = new FunctionBinder(type, BindingFlags.Instance);
            }

            return binder;
        }

        public Scope SelectParameterScope(string name)
        {
            if (CurrentContext is ParameterExpression p && p.Name == name)
            {
                return new Scope(CurrentContext, GlobalContext);
            }

            return null;
        }

        public Scope SelectChildScope(string name)
        {
            return new Scope(Expression.PropertyOrField(CurrentContext, name), GlobalContext);
        }

        public Scope NewScope(Expression newContext)
        {
            return new Scope(newContext, GlobalContext);
        }

        public Scope NewConversionScope(Type conversionType)
        {
            return new Scope(CurrentContext, GlobalContext, IsRoot, conversionType);
        }
    }
}