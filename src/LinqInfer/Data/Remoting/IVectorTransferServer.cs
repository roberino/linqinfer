using System;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    public interface IVectorTransferServer : IServer
    {
        void AddHandler(UriRoute route, Func<DataBatch, TcpResponse, bool> handler);
    }
}