using System;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    public interface IVectorTransferServer : IServer
    {
        void AddHandler(string messageType, Func<DataBatch, Stream, bool> handler);
    }
}