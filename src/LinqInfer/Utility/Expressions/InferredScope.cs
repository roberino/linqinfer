using System;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class InferredScope : Scope
    {
        public InferredScope(Scope parent, 
            Type outputType,
            Type subType,
            params ParameterExpression[] parameters) : base(parent, null, parameters)
        {
            OutputType = outputType;
        }
        
        public Type OutputType { get; }
    }
}