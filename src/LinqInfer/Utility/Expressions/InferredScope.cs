using System;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class InferredScope : Scope
    {
        public InferredScope(Scope parent, 
            Type outputType,
            params ParameterExpression[] parameters) : base(parent, parameters)
        {
            OutputType = outputType;
        }
        
        public Type OutputType { get; }
    }
}