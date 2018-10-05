using System;
using System.Linq;
using System.Linq.Expressions;
using static System.Reflection.BindingFlags;

namespace LinqInfer.Utility.Expressions
{
    class UnboundArgument
    {
        public UnboundArgument(ExpressionTree source, Scope scope, Expression expression = null)
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

        public Parameter[] Parameters {get; set; }

        public Type[] InputTypes { get; set; }

        public Type OutputType { get; set; }

        public Func<ExpressionTree, Scope, Expression> Resolver { get; set; }

        public bool HasUnresolvedTypes =>
            (OutputType?.IsGenericParameter).GetValueOrDefault()
            || (InputTypes?.Any(t => t.IsGenericParameter)).GetValueOrDefault()
            || (Parameters?.Any(p => !p.IsTypeKnown)).GetValueOrDefault();

        public Expression Resolve(InferredTypeResolver typeResolver = null)
        {
            if (Expression == null)
            {
                var parameters = Parameters.Zip(InputTypes ?? new Type[0], (para, type) => Expression.Parameter(type, para.Name)).ToArray();
                var inferredScope = new InferredScope(Scope, OutputType, typeResolver ?? new InferredTypeResolver(), parameters);
                Expression = Resolver(Source, inferredScope);
                Type = Expression.Type;
            }

            return Expression;
        }

        public Expression Compile()
        {
            var lambda = (LambdaExpression) Expression;
            var type = lambda.GetType();

            var compileMethod = type.GetMethod(nameof(Expression<object>.Compile), Instance | Static);

            var func = compileMethod.Invoke(lambda, new object[0]);

            var c = Expression.Constant(func);

            return c;
        }
    }
}