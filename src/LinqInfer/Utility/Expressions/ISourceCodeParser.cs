using System;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    public interface ISourceCodeParser
    {
        bool CanParse(SourceCode sourceCode);

        LambdaExpression Parse(SourceCode sourceCode,
            Func<Parameter, Type> parameterBinder, Type outputType = null);
    }
}