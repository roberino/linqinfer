using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    class GlobalStaticFunctions : IFunctionBinder
    {
        //Xml
        //Csv
        //File
        //Folder
        //Bind
        //ToJson
        //ToFile
        //Print
        //Execute()
        //Then(x => x + 1, e => 123)
        //Graph(x)
        //.Edge(y, 14)

        public bool IsDefined(string name)
        {
            return MathFunctions.IsDefined(name) 
                || ConversionFunctions.IsDefined(name)
                || ControlFunctions.IsDefined(name);
        }

        public Expression BindToFunction(string name, IReadOnlyCollection<UnboundArgument> parameters, Expression instance = null)
        {
            if (MathFunctions.IsDefined(name))
            {
                return MathFunctions.GetFunction(name, parameters);
            }

            if (ControlFunctions.IsDefined(name))
            {
                return ControlFunctions.GetFunction(name, parameters);
            }

            return ConversionFunctions.GetFunction(name, parameters);
        }
    }

    static class MathFunctions
    {
        static readonly FunctionBinder _mathFunctions = new FunctionBinder(typeof(Math), BindingFlags.Static);

        public static bool IsDefined(string name)
        {
            return _mathFunctions.IsDefined(name);
        }

        public static Expression GetFunction(string name, IReadOnlyCollection<UnboundArgument> parameters)
        {
            return _mathFunctions.BindToFunction(name, parameters);
        }
    }
}