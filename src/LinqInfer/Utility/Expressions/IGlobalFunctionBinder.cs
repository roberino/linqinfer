using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    interface IGlobalFunctionBinder
    {
        Expression BindToFunction(string name, IReadOnlyCollection<UnboundParameter> parameters);

        bool IsDefined(string name);
    }
}