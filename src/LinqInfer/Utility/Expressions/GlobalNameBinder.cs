using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class GlobalNameBinder
    {
        public Expression BindToName(string name)
        {
            switch (name)
            {
                case "true":
                    return Expression.Constant(true);
                case "false":
                    return Expression.Constant(false);
                case "empty":
                    return Expression.Constant(string.Empty);
                default:
                    return null;
            }
        }
    }
}
