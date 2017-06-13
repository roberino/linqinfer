using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class FunctionIterator<T>
    {
        private readonly Func<T, T> _func;

        /// <summary>
        /// Creates a new FunctionIterator for the supplied function
        /// </summary>
        public FunctionIterator(Func<T, T> func)
        {
            _func = func;
        }

        /// <summary>
        /// Iterates a function, feeding the result of each invocation back into the function
        /// </summary>
        /// <param name="initialValue">The starting value</param>
        /// <param name="maxIterations">The maximum number iterations</param>
        /// <param name="numberOfValuesToReturn">The number of output values to return</param>
        /// <param name="plotter">An action called to plot the data</param>
        /// <param name="haltingFunc">An optional function which will halt execution when a true value is returned</param>
        /// <param name="includeHaltValueInResults">When true, the value which halts the iteration is included in the output</param>
        /// <returns>Returns the most recent input values (ordered most recent to least recent)</returns>
        public FunctionIterationResult IterateFunction(
            T initialValue,
            int maxIterations = 100,
            int numberOfValuesToReturn = 3,
            Action<int, T> plotter = null,
            Func<T, bool> haltingFunc = null,
            bool includeHaltValueInResults = false)
        {
            Contract.Requires(maxIterations > 0);
            Contract.Requires(numberOfValuesToReturn > 0);

            var actualIterations = 0;
            var wasHalted = false;
            var timer = new Stopwatch();
            var valHistory = new Queue<T>(numberOfValuesToReturn);

            T x = initialValue;

            timer.Start();

            valHistory.Enqueue(x);
            plotter?.Invoke(0, x);

            for (int n = 1; n < maxIterations; n++)
            {
                if (valHistory.Count == numberOfValuesToReturn) valHistory.Dequeue();

                x = _func(x);

                if ((haltingFunc?.Invoke(x)).GetValueOrDefault())
                {
                    if (includeHaltValueInResults)
                    {
                        valHistory.Enqueue(x);
                        plotter?.Invoke(n, x);
                    }
                    wasHalted = true;
                    actualIterations = n;
                    break;
                }

                valHistory.Enqueue(x);
                plotter?.Invoke(n, x);
            }

            timer.Stop();

            return new FunctionIterationResult()
            {
                Elapsed = timer.Elapsed,
                WasHalted = wasHalted,
                MaxIterations = maxIterations,
                ActualIterations = wasHalted ? actualIterations : maxIterations,
                Outputs = valHistory.Reverse().ToArray()
            };
        }

        public sealed class FunctionIterationResult
        {
            public TimeSpan Elapsed { get; internal set; }
            public bool WasHalted { get; internal set; }
            public int ActualIterations { get; internal set; }
            public int MaxIterations { get; internal set; }
            public T[] Outputs { get; internal set; }
        }
    }
}