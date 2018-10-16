using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Utility.Expressions
{
    class ControlFunctions : FunctionBinder
    {
        public ControlFunctions() : base(typeof(ControlFunctions), BindingFlags.Static)
        {
        }

        public static bool HasProperty<T>(T instance, Token property)
        {
            if (instance == null)
            {
                return false;
            }

            return instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Any(p => string.Equals(p.Name, property.ToString()));
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

        public static IPromise<TOut[]> ForEach<TIn, TOut>(IEnumerable<TIn> items, Func<TIn, int, TOut> func)
        {
            return new Promise<TOut[]>(() =>
            {
                var results = new List<TOut>();
                var i = 0;

                foreach (var x in items)
                {
                    results[i++] = func(x, i);
                }

                return results.ToArray();
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