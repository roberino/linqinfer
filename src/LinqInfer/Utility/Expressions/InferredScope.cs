using System;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class InferredScope : Scope
    {
        public InferredScope(Scope parent, 
            Type outputType,
            InferredTypeResolver typeResolver,
            params ParameterExpression[] parameters) : base(parent, parameters)
        {
            OutputType = outputType;
            TypeResolver = typeResolver;
        }
        
        public Type OutputType { get; }
        public InferredTypeResolver TypeResolver { get; }
    }
}