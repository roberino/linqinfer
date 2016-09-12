using System;

namespace LinqInfer.Data.Remoting
{
    public interface IServer : IDisposable
    {
        ServerStatus Status { get; }
        void Start();
        void Stop();
    }
}