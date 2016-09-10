using System.Collections.Generic;
using System.Threading.Tasks;
using LinqInfer.Maths;
using System;

namespace LinqInfer.Data.Remoting
{
    public interface ITransferHandle
    {
        string Id { get; }
        string ClientId { get; }
        string OperationType { get; }
        Task Send(BinaryVectorDocument doc);
        Task Send(IEnumerable<ColumnVector1D> data);
        Task End(Uri forwardResponseTo = null);
    }
}