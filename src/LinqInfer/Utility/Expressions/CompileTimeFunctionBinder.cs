using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class CompileTimeFunctionBinder : IFunctionBinder
    {
        readonly ISourceCodeProvider _sourceCodeProvider;
        readonly IFunctionBinder _inbuiltFunctionBinder;
        readonly IDictionary<string, LambdaExpression> _compiledExpressions;

        public CompileTimeFunctionBinder(ISourceCodeProvider sourceCodeProvider, IFunctionBinder inbuiltFunctionBinder)
        {
            _sourceCodeProvider = sourceCodeProvider;
            _inbuiltFunctionBinder = inbuiltFunctionBinder;
            _compiledExpressions = new Dictionary<string, LambdaExpression>();
        }

        public Expression BindToFunction(string name, IReadOnlyCollection<UnboundParameter> parameters, Expression instance = null)
        {
            if (_inbuiltFunctionBinder.IsDefined(name))
            {
                return _inbuiltFunctionBinder.BindToFunction(name, parameters, instance);
            }

            var resolved = parameters.Select(p => p.Resolve()).ToArray();

            var lambda = GetOrAdd(name, n =>
            {
                var fp = new FunctionProvider(this);

                var sourceCode = _sourceCodeProvider.GetSourceCode(name);

                var parser = new ExpressionParser(fp);

                return parser.Parse(sourceCode, p => resolved.ElementAt(p.Index).Type);
            });

            return Expression.Invoke(lambda, resolved);
        }

        public bool IsDefined(string name)
        {
            return _inbuiltFunctionBinder.IsDefined(name)
                || _compiledExpressions.ContainsKey(name)
                || _sourceCodeProvider.Exists(name);
        }

        LambdaExpression GetOrAdd(string name, Func<string, LambdaExpression> expressionFactory)
        {
            if (!_compiledExpressions.TryGetValue(name, out var exp))
            {
                _compiledExpressions[name] = exp = expressionFactory(name);
            }

            return exp;
        }
    }
}