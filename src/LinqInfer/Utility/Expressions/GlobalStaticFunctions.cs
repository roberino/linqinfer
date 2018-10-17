using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    class GlobalStaticFunctions : IFunctionBinder
    {
        readonly IFunctionBinder[] _binders;

        GlobalStaticFunctions()
        {
            _binders = new IFunctionBinder[]
            {
                new ControlFunctions(),
                new MathFunctions(),
                new ConversionFunctions(),
                new DiagnosticFunctions(m => OutputMessage?.Invoke(m))
            };
        }

        public static IFunctionBinder Default()
        {
            var fb = new GlobalStaticFunctions();

            fb.OutputMessage += Console.WriteLine;

            return fb;
        }

        public event Action<string> OutputMessage;

        public bool IsDefined(string name)
        {
            return _binders.Any(b => b.IsDefined(name));
        }

        public Expression BindToFunction(string name, IReadOnlyCollection<UnboundArgument> parameters, Expression instance = null)
        {
            return _binders
                .First(f => f.IsDefined(name)).BindToFunction(name, parameters);
        }
    }

    class MathFunctions : FunctionBinder
    {
        public MathFunctions() : base(typeof(Math), BindingFlags.Static)
        {
        }
    }
}