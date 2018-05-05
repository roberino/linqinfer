using System.Collections.Generic;
using System.Threading.Tasks;
using LinqInfer.Maths;
using System;
using System.IO;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Data.Remoting
{
    public interface ITransferHandle : IDisposable
    {
        string Id { get; }
        string ClientId { get; }
        string Path { get; }
        Verb Verb { get; }
        Task Send(PortableDataDocument doc);
        Task Send(IEnumerable<ColumnVector1D> data);
        Task<Stream> Receive(object parameters = null);
        Task<Stream> End(object parameters, Uri forwardResponseTo = null);
        Task<Stream> End(IDictionary<string, string> parameters = null, Uri forwardResponseTo = null);
    }
}