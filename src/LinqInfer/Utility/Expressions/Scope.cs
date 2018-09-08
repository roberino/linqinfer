using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class Scope
    {
        readonly IFunctionProvider _functionProvider;

        public Scope(IFunctionProvider functions, params ParameterExpression[] parameters) : this(null, null, true, functions, null, parameters)
        {
        }

        protected Scope(Scope globalContext, params ParameterExpression[] parameters) : this(null, globalContext, true, null, null, parameters)
        {
        }

        Scope(
            Expression currentContext, 
            Scope parent = null,
            bool? isRoot = null, 
            IFunctionProvider functions = null,
            Type conversionType = null, 
            ParameterExpression[] parameters = null)
        {
            Parameters = parameters ?? new ParameterExpression[0];

            CurrentContext = currentContext;
            ParentScope = parent;
            IsRoot = isRoot.GetValueOrDefault(parent == null);
            ConversionType = conversionType;
            
            _functionProvider = functions;
        }

        public ParameterExpression[] Parameters { get; }

        public bool IsRoot { get; }

        public bool IsGlobalRoot => ParentScope == null;

        public Scope ParentScope { get; }

        public Scope RootScope => IsGlobalRoot ? this : ParentScope.RootScope;

        public Expression CurrentContext { get; }

        public Type ConversionType { get; }

        public IFunctionProvider Functions => IsGlobalRoot ? _functionProvider : RootScope.Functions;

        public IFunctionBinder GetBinder()
        {
            return Functions.GetBinder(CurrentContext.Type);
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
            return new Scope(CurrentContext, this, IsRoot, null, conversionType, Parameters);
        }
    }
}