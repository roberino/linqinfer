using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    class DiagnosticFunctions : FunctionBinder
    {
        readonly Action<string> _output;

        public DiagnosticFunctions(Action<string> output) : base(typeof(DiagnosticFunctions), BindingFlags.Instance)
        {
            _output = output;
        }

        public override Expression BindToFunction(string name, IReadOnlyCollection<UnboundArgument> parameters, Expression instance = null)
        {
            return base.BindToFunction(name, parameters, instance ?? Expression.Constant(this));
        }

        public T Print<T>(T value)
        {
            _output.Invoke(value.ToString());

            return value;
        }
    }
}
