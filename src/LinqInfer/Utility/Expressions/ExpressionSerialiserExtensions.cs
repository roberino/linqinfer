using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    public static class ExpressionSerialiserExtensions
    {
        public static Expression<Func<TInput, TOutput>> AsExpression<TInput, TOutput>(
            this string expression, params Type[] referenceFunctionTypes)
        {
            var functions = new FunctionProvider();

            if (referenceFunctionTypes.Length != 0)
            {
                functions.RegisterStaticTypes(referenceFunctionTypes);

                return new ExpressionParser(functions).Parse<TInput, TOutput>(expression);
            }

            var asmTargs = GetTargetAssemblies(Assembly.GetCallingAssembly());

            functions.RegisterAssemblies(asmTargs);

            return new ExpressionParser(functions).Parse<TInput, TOutput>(expression);
        }

        public static Expression<Func<TInput, TOutput>> AsExpression<TInput, TOutput>(
            this string expression, ISourceCodeProvider sourceCodeProvider)
        {
            var functions = new CompileTimeFunctionBinder(sourceCodeProvider,
                new GlobalStaticFunctions());

            var funcProvider = new FunctionProvider(functions);

            return new ExpressionParser(funcProvider).Parse<TInput, TOutput>(expression);
        }

        public static LambdaExpression AsExpression(
            this string expression, 
            ISourceCodeProvider sourceCodeProvider, 
            Func<Parameter, Type> parameterBinder,
            params ISourceCodeParser[] customParsers)
        {
            var functions = new CompileTimeFunctionBinder(sourceCodeProvider,
                new GlobalStaticFunctions(), customParsers);

            var funcProvider = new FunctionProvider(functions);

            return new ExpressionParser(funcProvider).Parse(SourceCode.Default(expression), parameterBinder);
        }

        public static Func<InvocationResult<TOutput>> AsFunc<TOutput>(
            this string expression,
            ISourceCodeProvider sourceCodeProvider,
            Func<Parameter, object> inputArgBinder,
            params ISourceCodeParser[] customParsers)
        {
            var args = new List<object>();

            var lambda = AsExpression(expression, sourceCodeProvider, p =>
            {
                var arg = inputArgBinder(p);

                args.Add(arg);

                return !p.IsTypeKnown ? arg.GetType() : p.Type;
            }, customParsers);

            var argExpressions = args
                .Select(a => a is Expression lambdaExpression ? lambdaExpression : Expression.Constant(a))
                .ToArray();

            var boundLambda = Expression.Lambda<Func<TOutput>>(Expression.Invoke(lambda, argExpressions).Convert<TOutput>());

            var compiled = boundLambda.Compile();

            return () =>
            {
                try
                {
                    return new InvocationResult<TOutput>(compiled());
                }
                catch (Exception ex)
                {
                    return new InvocationResult<TOutput>(default, ex);
                }
            };
        }

        public static Func<TOutput> AsFunc<TInput, TOutput>(
            this string expression,
            TInput input,
            TOutput defaultValue)
        {
            var asmTargs = GetTargetAssemblies(Assembly.GetCallingAssembly());

            var functions = new FunctionProvider()
                .RegisterAssemblies(asmTargs);

            var exp = new ExpressionParser(functions).Parse<TInput, TOutput>(expression);
            var func = exp.Compile();

            return () => func(input);
        }

        public static string ExportAsString<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression)
        {
            return expression.ExportExpression();
        }

        static Assembly[] GetTargetAssemblies(Assembly callingAssembly)
        {
            var asmTargs = new[] { callingAssembly, Assembly.GetExecutingAssembly() };
            var thisAsm = typeof(ExpressionSerialiserExtensions).Assembly;

            var asms = asmTargs.Distinct((x, y) => string.Equals(x.FullName, y.FullName)).Where(a => a.FullName != thisAsm.FullName).ToArray();

            return asms;
        }
    }
}