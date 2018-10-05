using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    class Scope
    {
        readonly IFunctionProvider _functionProvider;
        readonly TokenCache _tokenCache;

        public Scope(IFunctionProvider functions, params ParameterExpression[] parameters) : this(null, null, true, functions, null, parameters)
        {
        }

        internal Scope(Scope globalContext, params ParameterExpression[] parameters) : this(null, globalContext, true, null, null, parameters)
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

            if (IsGlobalRoot)
            {
                _tokenCache = new TokenCache();
            }
        }

        public ParameterExpression[] Parameters { get; }

        public bool IsRoot { get; }

        public bool IsGlobalRoot => ParentScope == null;

        public Scope ParentScope { get; }

        public Scope RootScope => IsGlobalRoot ? this : ParentScope.RootScope;

        public Expression CurrentContext { get; }

        public Type ConversionType { get; }

        public IFunctionProvider Functions => IsGlobalRoot ? _functionProvider : RootScope.Functions;

        public TokenCache TokenCache => IsGlobalRoot ? _tokenCache : RootScope.TokenCache;

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

        public Scope SelectChildScope(string name, IEnumerable<Expression> indexExpressions = null)
        {
            if (indexExpressions == null)
            {
                try
                {
                    return SelectChildScope(Expression.PropertyOrField(CurrentContext, name));
                }
                catch (ArgumentException)
                {
                    if (typeof(IDictionary<string, object>).IsAssignableFrom(CurrentContext.Type))
                    {
                        return SelectChildScope(Expression.MakeIndex(CurrentContext,
                            GetProperty("Item"),
                            new[] { Expression.Constant(name) }));
                    }

                    throw;
                }
            }

            return SelectChildScope(Expression.MakeIndex(CurrentContext,
                GetProperty(name),
                indexExpressions));
        }

        PropertyInfo GetProperty(string name) => CurrentContext.Type.GetProperty(name,
            BindingFlags.Instance | BindingFlags.Public);

        public Scope SelectChildScope(Expression expression)
        {
            return new Scope(expression, this);
        }

        public Scope SelectIndexScope(IEnumerable<Expression> indexExpressions)
        {
            var intt = typeof(int);
            
            var convertedIndexes = indexExpressions.Select(e => e.ConvertToType(intt));

            if (CurrentContext.Type.IsArray)
            {
                return new Scope(Expression.ArrayIndex(CurrentContext, convertedIndexes), this);
            }

            return SelectChildScope(Expression.MakeIndex(CurrentContext,
                GetProperty("Item"),
                convertedIndexes));
        }

        public Scope NewConversionScope(Type conversionType)
        {
            return new Scope(CurrentContext, this, IsRoot, null, conversionType, Parameters);
        }
    }
}