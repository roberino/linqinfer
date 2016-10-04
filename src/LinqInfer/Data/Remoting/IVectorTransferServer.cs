using System;
using System.IO;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IVectorTransferServer : IServer
    {
        void AddAsyncHandler(UriRoute route, Func<DataBatch, TcpResponse, Task<bool>> handler);
        void AddHandler(UriRoute route, Func<DataBatch, TcpResponse, bool> handler);
    }
}