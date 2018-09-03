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
            Parameters = parameters;
        }

        public Scope(Scope globalContext, Type conversionType, params ParameterExpression[] parameters) : this(parameters.First(), globalContext, false, conversionType)
        {
            Parameters = parameters;
        }

        Scope(Expression currentContext, Scope parent = null, bool? isRoot = null, Type conversionType = null)
        {
            Parameters = new ParameterExpression[0];

            CurrentContext = currentContext;
            ParentScope = parent;
            IsRoot = isRoot.GetValueOrDefault(parent == null);
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

        public ParameterExpression[] Parameters { get; }

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

        public bool IsGlobalRoot => ParentScope == null;

        public Scope ParentScope { get; }

        public Scope RootScope => IsGlobalRoot ? this : ParentScope.RootScope;

        public Expression CurrentContext { get; }

        public Type ConversionType { get; }

        public FunctionBinder GetBinder()
        {
            return GetBinder(CurrentContext.Type);
        }

        public FunctionBinder GetStaticBinder(string typeName)
        {
            if (!IsGlobalRoot)
            {
                return ParentScope.GetStaticBinder(typeName);
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
            if (!IsGlobalRoot) return ParentScope.GetBinder(type);

            if (!_binders.TryGetValue(type, out FunctionBinder binder))
            {
                _binders[type] = binder = new FunctionBinder(type, BindingFlags.Instance);
            }

            return binder;
        }

        public Scope SelectParameterScope(string name)
        {
            var parameter = Parameters.FirstOrDefault(p => string.Equals(p.Name, name));

            if (parameter != null) return new Scope(parameter, this);

            return !IsGlobalRoot ? ParentScope.SelectParameterScope(name) : null;
        }

        public Scope SelectChildScope(string name)
        {
            return new Scope(Expression.PropertyOrField(CurrentContext, name), this);
        }

        public Scope NewConversionScope(Type conversionType)
        {
            return new Scope(CurrentContext, this, IsRoot, conversionType);
        }
    }
}