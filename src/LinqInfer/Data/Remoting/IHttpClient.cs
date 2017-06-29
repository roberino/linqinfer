using System;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IHttpClient : IDisposable
    {
        Task<TcpResponse> GetAsync(Uri url);
    }
}