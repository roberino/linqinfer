using System;
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

                return new ExpressionParser<TInput, TOutput>(functions).Parse(expression);
            }

            var asmTargs = GetTargetAssemblies(Assembly.GetCallingAssembly());

            functions.RegisterAssemblies(asmTargs);

            return new ExpressionParser<TInput, TOutput>(functions).Parse(expression);
        }

        public static Func<TOutput> AsFunc<TInput, TOutput>(
            this string expression,
            TInput input,
            TOutput defaultValue)
        {
            var asmTargs = GetTargetAssemblies(Assembly.GetCallingAssembly());
            
            var functions = new FunctionProvider()
                .RegisterAssemblies(asmTargs);

            var exp = new ExpressionParser<TInput, TOutput>(functions).Parse(expression);
            var func = exp.Compile();

            return () => func(input);
        }

        public static string ExportAsString<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression)
        {
            return expression.ExportExpression();
        }

        static Assembly[] GetTargetAssemblies(Assembly callingAssembly)
        {
            var asmTargs = new[] {callingAssembly, Assembly.GetExecutingAssembly()};
            var thisAsm = typeof(ExpressionSerialiserExtensions).Assembly;
            
            var asms = asmTargs.Distinct((x, y) => string.Equals(x.FullName, y.FullName)).Where(a => a.FullName != thisAsm.FullName).ToArray();

            return asms;
        }
    }
}