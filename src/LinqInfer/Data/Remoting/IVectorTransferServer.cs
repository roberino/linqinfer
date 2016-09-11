using System;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    public interface IVectorTransferServer
    {
        ServerStatus Status { get; }
        void AddHandler(string messageType, Func<DataBatch, Stream, bool> handler);
        void Dispose();
        void Start();
        void Stop();
    }
}