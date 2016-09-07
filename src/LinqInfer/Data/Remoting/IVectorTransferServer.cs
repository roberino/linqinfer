using System;

namespace LinqInfer.Data.Remoting
{
    public interface IVectorTransferServer
    {
        ServerStatus Status { get; }
        void AddHandler(string messageType, Func<DataBatch, bool> handler);
        void Dispose();
        void Start();
        void Stop();
    }
}