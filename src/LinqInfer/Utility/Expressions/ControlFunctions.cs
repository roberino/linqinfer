using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    static class ControlFunctions
    {
        static readonly FunctionBinder _binder = new FunctionBinder(typeof(ControlFunctions), BindingFlags.Static);

        [NonBound]
        public static bool IsDefined(string name)
        {
            return _binder.IsDefined(name);
        }
        
        [NonBound]
        public static Expression GetFunction(string name, IReadOnlyCollection<UnboundArgument> parameters)
        {
            return _binder.BindToFunction(name, parameters);
        }

        public static IPromise<T[]> Loop<T>(Func<int, T> func, int iterations)
        {
            return new Promise<T[]>(() =>
            {
                var results = new T[iterations];

                for (var i = 0; i < iterations; i++)
                {
                    results[i] = func(i);
                }

                return results;
            });
        }

        public static IPromise<T[]> LoopUntil<T>(Func<int, T> func, Func<int, T, bool> condition)
        {
            return new Promise<T[]>(() =>
            {
                var results = new List<T>();
                var i = 0;
                var lastIndex = 0;
                T lastResult;

                do
                {
                    lastIndex = i;
                    lastResult = func(lastIndex);
                    results.Add(lastResult);
                    i++;
                } while (condition(lastIndex, lastResult));

                return results.ToArray();
            });
        }
    }
}