using System;

namespace LinqInfer.Utility.Diagnostics
{
    class Interceptor
    {
        readonly IStopwatch _sw;

        public Interceptor(IStopwatch stopwatch = null)
        {
            _sw = stopwatch ?? new StopwatchWrapper();
        }

        public static readonly  Interceptor Default = new Interceptor();

        public T Intercept<T>(Func<T> work, string actionName = null)
        {
            _sw.Reset();
            _sw.Start();

            var result = work();

            _sw.Stop();

            var name = actionName ?? work.Method.Name;

            DebugOutput.LogVerbose($"{name} {_sw.Elapsed}");

            return result;
        }
    }
}