using System;

namespace LinqInfer.Utility.Expressions
{
    interface IFunctionProvider
    {
        IFunctionBinder GetGlobalBinder();
        IFunctionBinder GetStaticBinder(string typeName);
        IFunctionBinder GetBinder(Type type);
    }
}