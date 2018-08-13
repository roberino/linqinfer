using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LinqInfer.Utility
{
    class StopwatchWrapper : IStopwatch
    {
        readonly Stopwatch _sw;

        public StopwatchWrapper()
        {
            _sw = new Stopwatch();
        }

        public TimeSpan Elapsed => _sw.Elapsed;

        public bool IsRunning => _sw.IsRunning;

        public void Start() => _sw.Start();

        public void Stop() => _sw.Stop();

        public void Reset() => _sw.Reset();
    }
}