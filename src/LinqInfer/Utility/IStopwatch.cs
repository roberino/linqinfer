using System;

namespace LinqInfer.Utility
{
    public interface IStopwatch
    {
        TimeSpan Elapsed { get; }
        bool IsRunning { get; }

        void Reset();
        void Start();
        void Stop();
    }
}