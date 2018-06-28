using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    internal class Scope
    {
        private readonly IDictionary<Type, FunctionBinder> _binders;

        public Scope(Expression currentContext, Scope globalContext = null, bool? isRoot = null, Type conversionType = null)
        {
            CurrentContext = currentContext;
            GlobalContext = globalContext ?? this;
            IsRoot = isRoot.GetValueOrDefault(globalContext == null);
            ConversionType = conversionType;

            if (IsRoot)
            {
                _binders = new Dictionary<Type, FunctionBinder>();
            }
        }

        public bool IsRoot { get; }

        public Scope GlobalContext { get; }

        public Expression CurrentContext { get; }

        public Type ConversionType { get; }

        public FunctionBinder GetBinder()
        {
            return GetBinder(CurrentContext.Type);
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