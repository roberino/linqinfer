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

        public Expression BindToFunction(string name, IReadOnlyCollection<UnboundArgument> parameters, Expression instance = null)
        {
            if (_inbuiltFunctionBinder.IsDefined(name))
            {
                return _inbuiltFunctionBinder.BindToFunction(name, parameters, instance);
            }
            
            var lambda = GetOrAdd(name, n =>
            {
                var fp = new FunctionProvider(this);

                var sourceCode = _sourceCodeProvider.GetSourceCode(name);

                var parser = new ExpressionParser(fp);

                return parser.Parse(sourceCode, p => p.IsTypeKnown ? p.Type : parameters.ElementAt(p.Index).Type);
            });

            var resolved = new Expression[parameters.Count];

            var i = 0;

            foreach (var (arg, parameter) in parameters.Zip(lambda.Parameters))
            {
                if (arg.IsInferred)
                {
                    var inferredArgs = InferredTypeResolver.GetInferredArgs(parameter.Type);

                    arg.InputTypes = inferredArgs.inputs;
                    arg.OutputType = inferredArgs.output;
                    resolved[i++] = arg.Resolve(); // Expression.Quote(
                }
                else
                {
                    resolved[i++] = arg.Resolve();
                }
            }

            try
            {
                return Expression.Invoke(lambda, resolved);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.Write(ex);
                throw;
            }
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