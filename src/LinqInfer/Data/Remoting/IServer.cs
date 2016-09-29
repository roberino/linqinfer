using System;

namespace LinqInfer.Data.Remoting
{
    public interface IServer : IDisposable
    {
        Uri BaseEndpoint { get; }
        ServerStatus Status { get; }
        void Start();
        void Stop();
    }
}