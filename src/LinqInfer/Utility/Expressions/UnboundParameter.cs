using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class UnboundParameter
    {
        public UnboundParameter(ExpressionTree source, Scope scope, Expression expression = null)
        {
            Source = source;
            Scope = scope;
            IsInferred = expression == null;
            Expression = expression;
            Type = expression?.Type;
        }

        public ExpressionTree Source { get; }

        public bool IsInferred { get; }

        public Scope Scope { get; }

        public Expression Expression { get; private set; }

        public Type Type { get; set; }

        public string[] ParameterNames {get; set; }

        public Type[] InputTypes { get; set; }

        public Type OutputType { get; set; }

        public Func<ExpressionTree, Scope, Expression> Resolver { get; set; }

        public void Resolve()
        {
            if (Expression == null)
            {
                var parameters = ParameterNames.Zip(InputTypes, (name, type) => Expression.Parameter(type, name)).ToArray();
                var inferredScope = new InferredScope(Scope, OutputType, Type, parameters);
                Expression = Resolver(Source, inferredScope);
                Type = Expression.Type;
            }
        }
    }
}