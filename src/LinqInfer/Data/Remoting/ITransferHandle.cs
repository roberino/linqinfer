using System.Collections.Generic;
using System.Threading.Tasks;
using LinqInfer.Maths;
using System;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    public interface ITransferHandle
    {
        string Id { get; }
        string ClientId { get; }
        string OperationType { get; }
        Task Send(BinaryVectorDocument doc);
        Task Send(IEnumerable<ColumnVector1D> data);
        Task<Stream> Receive(object parameters = null);
        Task<Stream> End(object parameters, Uri forwardResponseTo = null);
        Task<Stream> End(IDictionary<string, string> parameters = null, Uri forwardResponseTo = null);
    }
}