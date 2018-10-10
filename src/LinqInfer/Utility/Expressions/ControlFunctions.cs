using System;
using System.Collections.Generic;
using System.Linq;
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

        public static IPromise<T> Do<T>(Func<T> func)
        {
            return new Promise<T>(func);
        }

        public static IPromise<TOut> If<TIn, TOut>(TIn input, Func<TIn, bool> condition, Func<TIn, TOut> trueFunc, Func<TIn, TOut> falseFunc)
        {
            return new Promise<TOut>(() =>
            {
                if (condition(input))
                {
                    return trueFunc(input);
                }

                return falseFunc(input);
            });
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

        public static IPromise<T[]> PLoop<T>(Func<int, T> func, int iterations)
        {
            return new Promise<T[]>(() =>
            {
                var results = new T[iterations];

                Enumerable.Range(0, iterations)
                    .AsParallel().ForAll(n => results[n] = func(n));

                return results;
            });
        }

        public static IPromise<T[]> Recurse<T>(Func<int, T, (T nextResult, bool halt)> func)
        {
            return new Promise<T[]>(() =>
            {
                var results = new List<T>();
                var i = 0;
                (T result, bool halt) lastResult = (default, false);

                do
                {
                    lastResult = func(i, lastResult.result);
                    results.Add(lastResult.result);
                    i++;
                } while (!lastResult.halt);

                return results.ToArray();
            });
        }

        public static IPromise<T[]> LoopUntil<T>(Func<int, T> func, Func<int, T, bool> condition)
        {
            return new Promise<T[]>(() =>
            {
                var results = new List<T>();
                var i = 0;
                int lastIndex;
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