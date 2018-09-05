using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    interface IFunctionBinder
    {
        Expression BindToFunction(string name, IReadOnlyCollection<UnboundParameter> parameters, Expression instance = null);
        bool IsDefined(string name);
    }
}