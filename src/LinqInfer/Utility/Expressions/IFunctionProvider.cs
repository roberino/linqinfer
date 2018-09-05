using System;

namespace LinqInfer.Utility.Expressions
{
    interface IFunctionProvider
    {
        IGlobalFunctionBinder GetGlobalBinder();
        IFunctionBinder GetBinder(Type type);
        IFunctionBinder GetStaticBinder(string typeName);
    }
}