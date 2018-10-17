using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    class CompileTimeFunctionBinder : IFunctionBinder
    {
        readonly ISourceCodeProvider _sourceCodeProvider;
        readonly ISourceCodeParser[] _sourceCodeParsers;
        readonly IFunctionBinder _inbuiltFunctionBinder;
        readonly IDictionary<string, LambdaExpression> _compiledExpressions;

        public CompileTimeFunctionBinder(ISourceCodeProvider sourceCodeProvider, IFunctionBinder inbuiltFunctionBinder, params ISourceCodeParser[] customSourceCodeParsers)
        {
            _sourceCodeProvider = sourceCodeProvider;
            _inbuiltFunctionBinder = inbuiltFunctionBinder;
            _compiledExpressions = new Dictionary<string, LambdaExpression>();

            var fp = new FunctionProvider(this);

            var parser = new ExpressionParser(fp);

            _sourceCodeParsers = new ISourceCodeParser[] {parser}.Concat(customSourceCodeParsers).ToArray();
        }

        public Expression BindToFunction(string name, IReadOnlyCollection<UnboundArgument> parameters, Expression instance = null)
        {
            if (_inbuiltFunctionBinder.IsDefined(name))
            {
                return _inbuiltFunctionBinder.BindToFunction(name, parameters, instance);
            }
            
            var lambda = GetOrAdd(name, n =>
            {
                var sourceCode = _sourceCodeProvider.GetSourceCode(name);

                return Parse(sourceCode, parameters);
            });

            var resolved = new Expression[parameters.Count];

            var i = 0;

            var typeResolver = new InferredTypeResolver();

            foreach (var (arg, parameter) in parameters.Zip(lambda.Parameters))
            {
                if (arg.IsInferred)
                {
                    var inferredArgs = InferredTypeResolver.GetInferredArgs(parameter.Type);

                    arg.InputTypes = inferredArgs.inputs;
                    arg.OutputType = inferredArgs.output;
                    resolved[i++] = arg.Resolve(typeResolver);
                }
                else
                {
                    resolved[i++] = arg.Resolve(typeResolver);
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

        LambdaExpression Parse(SourceCode sourceCode, IReadOnlyCollection<UnboundArgument> args)
        {
            var parser = _sourceCodeParsers.FirstOrDefault(p => p.CanParse(sourceCode));

            if (parser == null)
            {
                throw new NotSupportedException(sourceCode.MimeType);
            }

            return parser.Parse(sourceCode, p => p.IsTypeKnown ? p.Type : args.ElementAt(p.Index).Type);
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